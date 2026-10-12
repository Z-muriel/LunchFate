using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Security.Cryptography;
using System.Threading.Tasks;
using LunchFate.Permissions;
using LunchFate.Shops;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp.Security.Claims;

namespace LunchFate.Records;

/// <summary>
/// 提供必须登录的服务端抽取及租户隔离的只读记录查询。
/// </summary>
[Authorize]
public class RecordAppService : LunchFateAppService, IRecordAppService
{
    /// <summary>抽取记录写入及查询仓储。</summary>
    private readonly IRepository<DrawRecord, Guid> _drawRepository;
    /// <summary>门店添加记录查询仓储。</summary>
    private readonly IReadOnlyRepository<ShopCreationRecord, Guid> _shopCreationRepository;
    /// <summary>永久门店只读仓储，抽取不修改其他聚合。</summary>
    private readonly IReadOnlyRepository<Shop, Guid> _shopRepository;

    /// <summary>
    /// 初始化抽取记录、门店新增记录及永久门店查询仓储。
    /// </summary>
    public RecordAppService(
        IRepository<DrawRecord, Guid> drawRepository,
        IReadOnlyRepository<ShopCreationRecord, Guid> shopCreationRepository,
        IReadOnlyRepository<Shop, Guid> shopRepository)
    {
        _drawRepository = drawRepository;
        _shopCreationRepository = shopCreationRepository;
        _shopRepository = shopRepository;
    }

    /// <summary>
    /// 校验候选（门店库与附近候选可同时提供），按原始顺序等概率抽取一次，并在记录保存成功后返回结果。
    /// </summary>
    [Authorize]
    [UnitOfWork(isTransactional: true)]
    public virtual async Task<DrawResult> DrawAsync(CreateDrawDto input)
    {
        var userId = RequireUserId();
        ValidateCandidates(input);

        // 1. 候选顺序 = 门店库候选（按传入顺序）+ 附近临时候选（按传入顺序）。
        //    客户端转盘扇区必须按同一顺序排列，WinnerIndex 才能直接对应到扇区。
        var candidates = new List<DrawCandidate>();

        if (input.ShopIds.Count > 0)
        {
            var ids = input.ShopIds;
            var query = await _shopRepository.GetQueryableAsync();
            var shops = await AsyncExecuter.ToListAsync(
                query.Where(s => s.IsEnabled && !s.IsDeleted && ids.Contains(s.Id))
                     .Select(s => new EnabledShopDto
                     {
                         Id = s.Id,
                         Name = s.Name,
                         Address = s.Address,
                         Longitude = s.Longitude,
                         Latitude = s.Latitude,
                         Category = s.Category,
                         AveragePrice = s.AveragePrice
                     }));

            if (shops.Count != ids.Count)
            {
                throw new UserFriendlyException(L["Records:InvalidCandidates"].Value);
            }

            var shopsById = shops.ToDictionary(s => s.Id);
            candidates.AddRange(ids.Select(id => new DrawCandidate(shopsById[id], false, null)));
        }

        foreach (var temporary in input.TemporaryShops)
        {
            candidates.Add(new DrawCandidate(
                new EnabledShopDto
                {
                    Id = Guid.Empty,
                    Name = temporary.Name,
                    Address = temporary.Address,
                    Longitude = temporary.Longitude,
                    Latitude = temporary.Latitude,
                    Category = ShopCategory.Other,
                    AveragePrice = 0m
                },
                true,
                temporary.PoiId));
        }

        // 2. 等概率抽取（FR-W-04）：候选权重相同，使用密码学安全随机数避免可预测
        var winnerIndex = RandomNumberGenerator.GetInt32(candidates.Count);
        var winner = candidates[winnerIndex];

        // 3. 保存不可变快照：临时门店不伪造门店库标识，只保留 POI 原始标识
        var record = new DrawRecord(
            GuidGenerator.Create(),
            CurrentTenant.Id,
            userId,
            CurrentUser.UserName ?? string.Empty,
            winner.IsTemporary ? null : winner.Shop.Id,
            winner.Shop.Name,
            winner.Shop.Address,
            winner.Shop.Category,
            winner.Shop.AveragePrice,
            Clock.Now,
            winner.IsTemporary,
            winner.PoiId);

        await _drawRepository.InsertAsync(record, autoSave: true);

        return new DrawResult
        {
            RecordId = record.Id,
            WinnerIndex = winnerIndex,
            Shop = winner.Shop
        };
    }

    /// <summary>
    /// 显式按当前租户筛选抽取记录，默认仅本人；查看全部必须验证指定权限。
    /// </summary>
    [Authorize]
    public virtual async Task<PagedResultDto<DrawRecordResult>> GetDrawListAsync(RecordSearchInput input)
    {
        var userId = RequireUserId();
        ValidateSearchInput(input);
        if (input.IncludeAll)
        {
            await AuthorizationService.CheckAsync(LunchFatePermissions.Records.DrawsViewAll);
        }

        var query = (await _drawRepository.GetQueryableAsync())
            .Where(r => r.TenantId == CurrentTenant.Id);
        if (!input.IncludeAll)
        {
            query = query.Where(r => r.UserId == userId);
        }

        var keyword = input.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(r => r.ShopName.Contains(keyword) || r.UserName.Contains(keyword) ||
                (r.Address != null && r.Address.Contains(keyword)));
        }

        if (input.FromTime.HasValue)
        {
            query = query.Where(r => r.RecordTime >= input.FromTime.Value);
        }

        if (input.ToTime.HasValue)
        {
            query = query.Where(r => r.RecordTime <= input.ToTime.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(query);
        var items = await AsyncExecuter.ToListAsync(
            query.OrderBy(SanitizeSorting(input.Sorting))
                 .Skip(input.SkipCount)
                 .Take(input.MaxResultCount)
                 .Select(r => new DrawRecordResult
                 {
                     Id = r.Id,
                     UserId = r.UserId,
                     UserName = r.UserName,
                     ShopId = r.ShopId,
                     ShopName = r.ShopName,
                     Address = r.Address,
                     Category = r.Category,
                     AveragePrice = r.AveragePrice,
                     RecordTime = r.RecordTime,
                     IsTemporary = r.IsTemporary,
                     PoiId = r.PoiId
                 }));

        return new PagedResultDto<DrawRecordResult>(totalCount, items);
    }

    /// <summary>
    /// 显式按当前租户筛选门店新增记录，默认仅本人；查看全部必须验证指定权限。
    /// </summary>
    [Authorize]
    public virtual async Task<PagedResultDto<ShopCreationRecordResult>> GetShopCreationListAsync(RecordSearchInput input)
    {
        var userId = RequireUserId();
        ValidateSearchInput(input);
        if (input.IncludeAll)
        {
            await AuthorizationService.CheckAsync(LunchFatePermissions.Records.ShopCreationsViewAll);
        }

        var query = (await _shopCreationRepository.GetQueryableAsync())
            .Where(r => r.TenantId == CurrentTenant.Id);
        if (!input.IncludeAll)
        {
            query = query.Where(r => r.UserId == userId);
        }

        var keyword = input.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(r => r.ShopName.Contains(keyword) || r.UserName.Contains(keyword) ||
                (r.Address != null && r.Address.Contains(keyword)));
        }

        if (input.FromTime.HasValue)
        {
            query = query.Where(r => r.RecordTime >= input.FromTime.Value);
        }

        if (input.ToTime.HasValue)
        {
            query = query.Where(r => r.RecordTime <= input.ToTime.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(query);
        var items = await AsyncExecuter.ToListAsync(
            query.OrderBy(SanitizeSorting(input.Sorting))
                 .Skip(input.SkipCount)
                 .Take(input.MaxResultCount)
                 .Select(r => new ShopCreationRecordResult
                 {
                     Id = r.Id,
                     UserId = r.UserId,
                     UserName = r.UserName,
                     ShopId = r.ShopId,
                     ShopName = r.ShopName,
                     Address = r.Address,
                     Category = r.Category,
                     AveragePrice = r.AveragePrice,
                     RecordTime = r.RecordTime
                 }));

        return new PagedResultDto<ShopCreationRecordResult>(totalCount, items);
    }

    /// <summary>
    /// 显式验证当前用户已经认证且拥有标识，避免宽松授权器允许匿名调用。
    /// </summary>
    private Guid RequireUserId()
    {
        if (!CurrentUser.IsAuthenticated || !CurrentUser.Id.HasValue ||
            LazyServiceProvider.LazyGetRequiredService<ICurrentPrincipalAccessor>().Principal.Identity?.IsAuthenticated != true)
        {
            throw new AbpAuthorizationException(L["Records:LoginRequired"].Value);
        }

        return CurrentUser.Id.Value;
    }

    /// <summary>
    /// 验证候选集合：门店库与附近候选可同时提供（转盘取并集），但至少要有一种；
    /// 并检查标识非空不重复、临时快照合法且 POI 标识唯一。
    /// </summary>
    private void ValidateCandidates(CreateDrawDto input)
    {
        if (input == null || input.ShopIds == null || input.TemporaryShops == null ||
            (input.ShopIds.Count == 0 && input.TemporaryShops.Count == 0) ||
            input.ShopIds.Any(id => id == Guid.Empty) ||
            input.ShopIds.Distinct().Count() != input.ShopIds.Count)
        {
            throw new UserFriendlyException(L["Records:InvalidCandidates"].Value);
        }

        // POI 标识重复会让同一家店在转盘上出现两次并扭曲概率，直接拒绝。
        var poiIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var shop in input.TemporaryShops)
        {
            if (shop == null || string.IsNullOrWhiteSpace(shop.PoiId) || shop.PoiId.Length > 128 ||
                !poiIds.Add(shop.PoiId) ||
                string.IsNullOrWhiteSpace(shop.Name) || shop.Name.Length > 256 ||
                shop.Address?.Length > 256 ||
                !IsValidCoordinate(shop.Longitude, -180d, 180d) ||
                !IsValidCoordinate(shop.Latitude, -90d, 90d))
            {
                throw new UserFriendlyException(L["Records:InvalidCandidates"].Value);
            }
        }
    }

    /// <summary>
    /// 验证可空坐标；非空时必须为有限数值且位于指定的闭区间内。
    /// </summary>
    private static bool IsValidCoordinate(double? value, double minimum, double maximum)
    {
        return !value.HasValue ||
            (double.IsFinite(value.Value) && value.Value >= minimum && value.Value <= maximum);
    }

    /// <summary>
    /// 验证分页偏移、每页数量和时间区间，保证查询不会接受非法分页或逆序时间。
    /// </summary>
    private void ValidateSearchInput(RecordSearchInput input)
    {
        if (input == null || input.SkipCount < 0 || input.MaxResultCount < 1 || input.MaxResultCount > 1000)
        {
            throw new UserFriendlyException(L["Records:InvalidPaging"].Value);
        }

        if (input.FromTime.HasValue && input.ToTime.HasValue && input.FromTime.Value > input.ToTime.Value)
        {
            throw new UserFriendlyException(L["Records:InvalidTimeRange"].Value);
        }
    }

    /// <summary>
    /// 仅允许按记录时间、店铺名称或用户名排序，非法输入回退为时间降序，并追加标识稳定排序。
    /// </summary>
    private static string SanitizeSorting(string? sorting)
    {
        var tokens = (sorting ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length is < 1 or > 2 ||
            tokens[0] is not (nameof(DrawRecord.RecordTime) or nameof(DrawRecord.ShopName) or nameof(DrawRecord.UserName)))
        {
            return "RecordTime descending, Id";
        }

        var descending = tokens.Length == 2 &&
            (tokens[1].Equals("desc", StringComparison.OrdinalIgnoreCase) ||
             tokens[1].Equals("descending", StringComparison.OrdinalIgnoreCase));
        if (tokens.Length == 2 && !descending &&
            !tokens[1].Equals("asc", StringComparison.OrdinalIgnoreCase) &&
            !tokens[1].Equals("ascending", StringComparison.OrdinalIgnoreCase))
        {
            return "RecordTime descending, Id";
        }

        return descending ? $"{tokens[0]} descending, Id" : $"{tokens[0]}, Id";
    }

    /// <summary>
    /// 抽取候选：门店库门店或附近临时门店，并携带来源信息以便写入记录快照。
    /// </summary>
    /// <param name="Shop">候选门店快照，临时候选的标识为 <see cref="Guid.Empty"/></param>
    /// <param name="IsTemporary">是否为附近临时门店</param>
    /// <param name="PoiId">临时门店的高德 POI 标识；门店库候选为 null</param>
    private sealed record DrawCandidate(EnabledShopDto Shop, bool IsTemporary, string? PoiId);
}
