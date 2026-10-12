using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LunchFate.Integration.Gaode;
using LunchFate.Localization;
using LunchFate.Permissions;
using LunchFate.Records;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Linq.Dynamic.Core;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Authorization;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Uow;
using Volo.Abp.Security.Claims;

namespace LunchFate.Shops;

/// <summary>
/// 店铺应用服务：提供增删改查、筛选查询与大转盘数据源（需求文档 FR-S-01 ~ FR-S-05、FR-W-01）。
/// </summary>
[Authorize]
public class ShopAppService : ApplicationService, IShopAppService
{
    /// <summary>
    /// 允许客户端动态排序的字段白名单：Sorting 直接来自请求，必须先校验再交给动态 LINQ。
    /// </summary>
    private static readonly string[] SortableFields =
    [
        nameof(Shop.Name),
        nameof(Shop.AveragePrice),
        nameof(Shop.Rating),
        nameof(Shop.CreationTime)
    ];

    /// <summary>
    /// 单次批量新增允许的最大条数；超出直接拒绝，不做静默截断。
    /// </summary>
    private const int MaxBatchSize = 100;

    private readonly IRepository<Shop, Guid> _repository;
    private readonly ShopManager _shopManager;
    private readonly IGaodeMapService _gaodeMapService;
    /// <summary>通过本地事件保存独立门店添加记录。</summary>
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 初始化门店仓储、领域校验、地图地理编码及门店新增本地事件总线。
    /// </summary>
    public ShopAppService(
        IRepository<Shop, Guid> repository,
        ShopManager shopManager,
        IGaodeMapService gaodeMapService,
        ILocalEventBus localEventBus)
    {
        _repository = repository;
        _shopManager = shopManager;
        _gaodeMapService = gaodeMapService;
        _localEventBus = localEventBus;
        LocalizationResource = typeof(LunchFateResource);
    }

    /// <summary>
    /// 按 Id 获取店铺详情。
    /// </summary>
    /// <param name="id">店铺 Id</param>
    /// <returns>店铺详情</returns>
    [Authorize(LunchFatePermissions.Shops.Default)]
    public virtual async Task<ShopDto> GetAsync(Guid id)
    {
        var shop = await _repository.GetAsync(id);
        return ObjectMapper.Map<Shop, ShopDto>(shop);
    }

    /// <summary>
    /// 分页查询店铺列表，支持关键字（名称/标签）、分类与启用状态筛选。
    /// </summary>
    /// <param name="input">分页、排序与筛选入参</param>
    /// <returns>分页结果</returns>
    [Authorize(LunchFatePermissions.Shops.Default)]
    public virtual async Task<PagedResultDto<ShopDto>> GetListAsync(ShopSearchInput input)
    {
        // 1. 解析筛选条件
        var keyword = input.Keyword?.Trim();

        var query = await _repository.GetQueryableAsync();

        // 2. 应用筛选
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(s =>
                s.Name.Contains(keyword) ||
                (s.Tags != null && s.Tags.Contains(keyword)));
        }

        if (input.Category.HasValue)
        {
            var category = input.Category.Value;
            query = query.Where(s => s.Category == category);
        }

        if (input.IsEnabled.HasValue)
        {
            var isEnabled = input.IsEnabled.Value;
            query = query.Where(s => s.IsEnabled == isEnabled);
        }

        // 3. 计数（排序与投影不影响总数，先计数可让 COUNT 语句保持简单）
        var totalCount = await AsyncExecuter.CountAsync(query);

        // 4. 排序 → 分页 → 投影：列表查询在数据库端投影，不加载完整实体
        var items = await AsyncExecuter.ToListAsync(
            query.OrderBy(SanitizeSorting(input.Sorting))
                 .Skip(input.SkipCount)
                 .Take(input.MaxResultCount)
                 .Select(s => new ShopDto
                 {
                     Id = s.Id,
                     Name = s.Name,
                      Category = s.Category,
                      Address = s.Address,
                      Longitude = s.Longitude,
                      Latitude = s.Latitude,
                     Phone = s.Phone,
                     AveragePrice = s.AveragePrice,
                     Rating = s.Rating,
                     Tags = s.Tags,
                     Weight = s.Weight,
                     IsEnabled = s.IsEnabled,
                     Remark = s.Remark,
                     IsDeleted = s.IsDeleted,
                     DeleterId = s.DeleterId,
                     DeletionTime = s.DeletionTime,
                     LastModificationTime = s.LastModificationTime,
                     LastModifierId = s.LastModifierId,
                     CreationTime = s.CreationTime,
                     CreatorId = s.CreatorId
                 }));

        // 5. 返回
        return new PagedResultDto<ShopDto>(totalCount, items);
    }

    /// <summary>
    /// 大转盘数据源：必须登录，仅返回已启用的店铺，不限制候选数量。
    /// </summary>
    /// <returns>全部启用店铺列表</returns>
    [Authorize]
    public virtual async Task<ListResultDto<EnabledShopDto>> GetEnabledListAsync()
    {
        RequireLogin();
        var query = await _repository.GetQueryableAsync();

        var items = await AsyncExecuter.ToListAsync(
            query.Where(s => s.IsEnabled)
                 .OrderByDescending(s => s.LastModificationTime ?? s.CreationTime)
                 .Select(s => new EnabledShopDto
                 {
                     Id = s.Id,
                     Name = s.Name,
                     Category = s.Category,
                      AveragePrice = s.AveragePrice,
                      Address = s.Address,
                      Longitude = s.Longitude,
                      Latitude = s.Latitude
                 }));

        return new ListResultDto<EnabledShopDto>(items);
    }

    /// <summary>
    /// 批量新增店铺：每条使用独立事务保存门店及新增记录，单条失败回滚且不影响其余项目。
    /// </summary>
    /// <param name="inputs">店铺新增入参</param>
    /// <returns>批量处理统计</returns>
    /// <exception cref="UserFriendlyException">入参为空或超出单批上限时抛出</exception>
    [Authorize(LunchFatePermissions.Shops.Default)]
    [Authorize(LunchFatePermissions.Shops.Create)]
    public virtual async Task<BatchCreateShopsResult> BatchCreateAsync(List<CreateUpdateShopDto> inputs)
    {
        RequireLogin();

        // 1. 显式拒绝空入参和超量入参，绝不静默截断——截断会让用户看到的统计与实际选择不一致。
        if (inputs is null || inputs.Count == 0)
        {
            throw new UserFriendlyException(L["Shops:BatchEmpty"].Value);
        }

        if (inputs.Count > MaxBatchSize)
        {
            throw new UserFriendlyException(L["Shops:BatchTooLarge", MaxBatchSize].Value);
        }

        // 2. 逐条独立事务处理：同名跳过，其它异常记入错误明细但不中断整批。
        var result = new BatchCreateShopsResult();
        for (var index = 0; index < inputs.Count; index++)
        {
            var input = inputs[index];
            if (input is null)
            {
                result.Errors.Add(L["Shops:BatchItemInvalid", index + 1].Value);
                continue;
            }

            var nameValidated = false;
            try
            {
                using var unitOfWork = UnitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
                var name = await _shopManager.EnsureNameIsUniqueAsync(input.Name);
                nameValidated = true;
                var shop = new Shop();
                Apply(input, shop);
                shop.Name = name;
                await GeocodeShopAsync(shop, input.Address, CancellationToken.None);
                await _repository.InsertAsync(shop, autoSave: true);
                await PublishShopCreatedAsync(shop);
                await unitOfWork.CompleteAsync();
                result.CreatedCount++;
            }
            catch (UserFriendlyException) when (!nameValidated)
            {
                // 名称校验阶段被拒绝（同名或非法名称）视为跳过，不计为失败。
                result.SkippedCount++;
            }
            catch (Exception ex)
            {
                // 只回传本地化文案，内部异常文本仅写日志，避免泄露实现细节。
                Logger.LogWarning(ex, "批量新增门店失败：{ShopName}", input.Name);
                result.Errors.Add(L["Shops:BatchItemFailed", index + 1, input.Name].Value);
            }
        }

        return result;
    }

    /// <summary>
    /// 批量启用/停用店铺（需求文档 FR-S-06）。
    /// 与批量新增不同，这里不涉及名称唯一性等可预期的逐条失败，因此整体单事务提交：
    /// 要么全部生效，要么全部回滚，避免用户看到"一半成功"的中间态。
    /// </summary>
    /// <param name="input">门店标识集合与目标启用状态</param>
    /// <returns>批量处理统计</returns>
    /// <exception cref="UserFriendlyException">入参为空、含空标识或超出单批上限时抛出</exception>
    [Authorize(LunchFatePermissions.Shops.Default)]
    [Authorize(LunchFatePermissions.Shops.Edit)]
    [UnitOfWork(isTransactional: true)]
    public virtual async Task<BatchSetShopsEnabledResult> BatchSetEnabledAsync(BatchSetShopsEnabledInput input)
    {
        RequireLogin();

        // 1. 显式拒绝非法入参，绝不静默截断或静默忽略——那会让回显的统计与实际操作不一致。
        if (input?.Ids is null || input.Ids.Count == 0)
        {
            throw new UserFriendlyException(L["Shops:BatchSelectionEmpty"].Value);
        }

        // 2. 先去重再判上限：重复标识不应占用批次额度，也不该被重复统计。
        var ids = input.Ids.Distinct().ToList();
        if (ids.Count > MaxBatchSize)
        {
            throw new UserFriendlyException(L["Shops:BatchSelectionTooLarge", MaxBatchSize].Value);
        }

        if (ids.Contains(Guid.Empty))
        {
            throw new UserFriendlyException(L["Shops:BatchSelectionInvalid"].Value);
        }

        // 3. 只加载确实存在的门店：仓储已应用软删除与多租户全局过滤器，
        //    已删除或属于其它租户的门店会自然落入 NotFound，而不是被误改。
        var query = await _repository.GetQueryableAsync();
        var shops = await AsyncExecuter.ToListAsync(query.Where(s => ids.Contains(s.Id)));

        var result = new BatchSetShopsEnabledResult
        {
            NotFoundCount = ids.Count - shops.Count
        };

        // 4. 只更新状态确实需要变更的门店，避免无意义地刷新审计字段（LastModificationTime）。
        var changed = shops.Where(s => s.IsEnabled != input.IsEnabled).ToList();
        result.UpdatedCount = changed.Count;
        result.UnchangedCount = shops.Count - changed.Count;

        if (changed.Count > 0)
        {
            // 5. 只改启用状态：不经过 Apply，因此不会触碰地址、坐标与权重，也不会触发重新地理编码。
            foreach (var shop in changed)
            {
                shop.IsEnabled = input.IsEnabled;
            }

            await _repository.UpdateManyAsync(changed, autoSave: true);
        }

        return result;
    }

    /// <summary>
    /// 新增店铺；名称在未删除数据中必须唯一（领域服务校验，需求文档 §3.2）。
    /// </summary>
    /// <param name="input">新增入参</param>
    /// <returns>创建后的店铺</returns>
    [Authorize(LunchFatePermissions.Shops.Default)]
    [Authorize(LunchFatePermissions.Shops.Create)]
    [UnitOfWork(isTransactional: true)]
    public virtual async Task<ShopDto> CreateAsync(CreateUpdateShopDto input)
    {
        RequireLogin();
        // 1. 领域服务校验名称唯一并返回规范化名称
        var name = await _shopManager.EnsureNameIsUniqueAsync(input.Name);

        // 2. 组装聚合根并入库
        var shop = new Shop();
        Apply(input, shop);
        shop.Name = name;

        // 3. 地理编码：客户端未提供坐标时按地址解析（FR-M-01），失败不阻断保存
        if (!HasExplicitCoordinates(input))
        {
            await GeocodeShopAsync(shop, input.Address, CancellationToken.None);
        }

        await _repository.InsertAsync(shop, autoSave: true);
        await PublishShopCreatedAsync(shop);

        // 4. 返回
        return ObjectMapper.Map<Shop, ShopDto>(shop);
    }

    /// <summary>
    /// 更新店铺；名称在未删除数据中必须唯一（排除自身，需求文档 §3.2）。
    /// </summary>
    /// <param name="id">待更新店铺 Id</param>
    /// <param name="input">更新入参</param>
    /// <returns>更新后的店铺</returns>
    [Authorize(LunchFatePermissions.Shops.Default)]
    [Authorize(LunchFatePermissions.Shops.Edit)]
    public virtual async Task<ShopDto> UpdateAsync(Guid id, CreateUpdateShopDto input)
    {
        // 1. 加载聚合根并校验名称唯一（排除自身）
        var shop = await _repository.GetAsync(id);
        var name = await _shopManager.EnsureNameIsUniqueAsync(input.Name, id);

        // 2. 先记录变更前的地址：Apply 之后 shop.Address 已被覆盖，再比较将永远相等（FR-M-01 编辑场景失效）。
        var previousAddress = shop.Address;

        // 3. 应用入参并保存
        Apply(input, shop);
        shop.Name = name;

        // 4. 地理编码：客户端未提供坐标且地址确实变化时重新解析（FR-M-01），失败不阻断保存
        if (!HasExplicitCoordinates(input) && !string.Equals(previousAddress, input.Address, StringComparison.Ordinal))
        {
            await GeocodeShopAsync(shop, input.Address, CancellationToken.None);
        }

        await _repository.UpdateAsync(shop, autoSave: true);

        // 5. 返回
        return ObjectMapper.Map<Shop, ShopDto>(shop);
    }

    /// <summary>
    /// 删除店铺（软删除）。
    /// </summary>
    /// <param name="id">待删除店铺 Id</param>
    [Authorize(LunchFatePermissions.Shops.Default)]
    [Authorize(LunchFatePermissions.Shops.Delete)]
    public virtual async Task DeleteAsync(Guid id)
    {
        // Shop 继承 FullAuditedAggregateRoot，仓储删除即置 IsDeleted，不物理删除（需求文档 FR-S-04）。
        await _repository.DeleteAsync(id);
    }

    /// <summary>
    /// 显式校验当前用户已经登录且拥有标识，避免宽松授权器允许匿名调用。
    /// </summary>
    private void RequireLogin()
    {
        if (!CurrentUser.IsAuthenticated || !CurrentUser.Id.HasValue ||
            LazyServiceProvider.LazyGetRequiredService<ICurrentPrincipalAccessor>().Principal.Identity?.IsAuthenticated != true)
        {
            throw new AbpAuthorizationException(L["Records:LoginRequired"].Value);
        }
    }

    /// <summary>
    /// 在当前工作单元立即发布已插入门店的服务端身份、时间与快照，记录失败直接传播。
    /// </summary>
    private async Task PublishShopCreatedAsync(Shop shop)
    {
        await _localEventBus.PublishAsync(new ShopCreatedEvent(
            CurrentTenant.Id,
            CurrentUser.Id,
            CurrentUser.UserName ?? string.Empty,
            shop.Id,
            shop.Name,
            shop.Address,
            shop.Category,
            shop.AveragePrice,
            Clock.Now), onUnitOfWorkComplete: false);
    }

    /// <summary>
    /// 把入参写入聚合根（手工映射，避免 Mapperly 为审计字段生成无意义的映射代码）。
    /// 名称不在本方法赋值：由调用方使用领域服务校验并规范化后的名称写入。
    /// 经纬度只在客户端显式提供时写入：门店管理表单不带坐标，若无条件赋值会把已有坐标清空。
    /// </summary>
    /// <param name="input">新增/更新入参</param>
    /// <param name="shop">店铺聚合根</param>
    private static void Apply(CreateUpdateShopDto input, Shop shop)
    {
        shop.Category = input.Category;
        shop.Address = input.Address;
        shop.Phone = input.Phone;
        shop.AveragePrice = input.AveragePrice;
        shop.Rating = input.Rating;
        shop.Tags = input.Tags;
        shop.Weight = input.Weight;
        shop.IsEnabled = input.IsEnabled;
        shop.Remark = input.Remark;

        if (HasExplicitCoordinates(input))
        {
            shop.Longitude = input.Longitude;
            shop.Latitude = input.Latitude;
        }
    }

    /// <summary>
    /// 判断客户端是否同时提供了经纬度。仅提供其一时视为无效，交由地址地理编码补齐。
    /// </summary>
    /// <param name="input">新增/更新入参</param>
    /// <returns>经纬度齐全时为 true</returns>
    private static bool HasExplicitCoordinates(CreateUpdateShopDto input)
    {
        return input.Longitude.HasValue && input.Latitude.HasValue;
    }

    /// <summary>
    /// 调用高德地理编码服务，将地址写入聚合根的经纬度字段。
    /// 地址为空或服务不可用时经纬度留空，不阻断保存流程（FR-M-01 降级策略）：
    /// 保留旧坐标会让导航指向旧地址，比留空更危险，因此先清空再按结果回填。
    /// </summary>
    /// <param name="shop">店铺聚合根</param>
    /// <param name="address">地址文本</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task GeocodeShopAsync(Shop shop, string? address, CancellationToken cancellationToken)
    {
        shop.Longitude = null;
        shop.Latitude = null;

        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        var result = await _gaodeMapService.GeocodeAsync(address, cancellationToken: cancellationToken);
        if (result is not null)
        {
            shop.Longitude = result.Longitude;
            shop.Latitude = result.Latitude;
        }
    }

    /// <summary>
    /// 校验并规范化来自客户端的动态排序表达式：只接受白名单字段，非法值回退为按名称升序。
    /// </summary>
    /// <param name="sorting">排序表达式，例如 "Name desc"</param>
    /// <returns>可安全交给动态 LINQ 的排序表达式</returns>
    private static string SanitizeSorting(string? sorting)
    {
        var tokens = (sorting ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 0 || !SortableFields.Contains(tokens[0], StringComparer.Ordinal))
        {
            return nameof(Shop.Name);
        }

        var descending = tokens.Length > 1 &&
            (tokens[1].Equals("desc", StringComparison.OrdinalIgnoreCase) ||
             tokens[1].Equals("descending", StringComparison.OrdinalIgnoreCase));

        return descending ? $"{tokens[0]} descending" : tokens[0];
    }
}
