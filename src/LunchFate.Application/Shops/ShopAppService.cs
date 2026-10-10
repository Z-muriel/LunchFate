using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LunchFate.Permissions;
using Microsoft.AspNetCore.Authorization;
using System.Linq.Dynamic.Core;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LunchFate.Shops;

/// <summary>
/// 店铺应用服务：提供增删改查、筛选查询与大转盘数据源（需求文档 FR-S-01 ~ FR-S-05、FR-W-01）。
/// </summary>
[Authorize(LunchFatePermissions.Shops.Default)]
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

    private readonly IRepository<Shop, Guid> _repository;
    private readonly ShopManager _shopManager;

    public ShopAppService(
        IRepository<Shop, Guid> repository,
        ShopManager shopManager)
    {
        _repository = repository;
        _shopManager = shopManager;
    }

    /// <summary>
    /// 按 Id 获取店铺详情。
    /// </summary>
    /// <param name="id">店铺 Id</param>
    /// <returns>店铺详情</returns>
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
    /// 大转盘数据源：所有用户可读，仅返回已启用的店铺（需求文档 §2、FR-W-01）。
    /// </summary>
    /// <returns>启用店铺列表（不超过 <see cref="ShopConsts.MaxWheelSize"/> 条）</returns>
    [AllowAnonymous]
    public virtual async Task<ListResultDto<EnabledShopDto>> GetEnabledListAsync()
    {
        var query = await _repository.GetQueryableAsync();

        var items = await AsyncExecuter.ToListAsync(
            query.Where(s => s.IsEnabled)
                 .OrderByDescending(s => s.LastModificationTime ?? s.CreationTime)
                 .Take(ShopConsts.MaxWheelSize)
                 .Select(s => new EnabledShopDto
                 {
                     Id = s.Id,
                     Name = s.Name,
                     Category = s.Category,
                     AveragePrice = s.AveragePrice,
                     Address = s.Address
                 }));

        return new ListResultDto<EnabledShopDto>(items);
    }

    /// <summary>
    /// 新增店铺；名称在未删除数据中必须唯一（领域服务校验，需求文档 §3.2）。
    /// </summary>
    /// <param name="input">新增入参</param>
    /// <returns>创建后的店铺</returns>
    [Authorize(LunchFatePermissions.Shops.Create)]
    public virtual async Task<ShopDto> CreateAsync(CreateUpdateShopDto input)
    {
        // 1. 领域服务校验名称唯一并返回规范化名称
        var name = await _shopManager.EnsureNameIsUniqueAsync(input.Name);

        // 2. 组装聚合根并入库
        var shop = new Shop();
        Apply(input, shop);
        shop.Name = name;
        await _repository.InsertAsync(shop, autoSave: true);

        // 3. 返回
        return ObjectMapper.Map<Shop, ShopDto>(shop);
    }

    /// <summary>
    /// 更新店铺；名称在未删除数据中必须唯一（排除自身，需求文档 §3.2）。
    /// </summary>
    /// <param name="id">待更新店铺 Id</param>
    /// <param name="input">更新入参</param>
    /// <returns>更新后的店铺</returns>
    [Authorize(LunchFatePermissions.Shops.Edit)]
    public virtual async Task<ShopDto> UpdateAsync(Guid id, CreateUpdateShopDto input)
    {
        // 1. 加载聚合根并校验名称唯一（排除自身）
        var shop = await _repository.GetAsync(id);
        var name = await _shopManager.EnsureNameIsUniqueAsync(input.Name, id);

        // 2. 应用入参并保存
        Apply(input, shop);
        shop.Name = name;
        await _repository.UpdateAsync(shop, autoSave: true);

        // 3. 返回
        return ObjectMapper.Map<Shop, ShopDto>(shop);
    }

    /// <summary>
    /// 删除店铺（软删除）。
    /// </summary>
    /// <param name="id">待删除店铺 Id</param>
    [Authorize(LunchFatePermissions.Shops.Delete)]
    public virtual async Task DeleteAsync(Guid id)
    {
        // Shop 继承 FullAuditedAggregateRoot，仓储删除即置 IsDeleted，不物理删除（需求文档 FR-S-04）。
        await _repository.DeleteAsync(id);
    }

    /// <summary>
    /// 把入参写入聚合根（手工映射，避免 Mapperly 为审计字段生成无意义的映射代码）。
    /// 名称不在本方法赋值：由调用方使用领域服务校验并规范化后的名称写入。
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
