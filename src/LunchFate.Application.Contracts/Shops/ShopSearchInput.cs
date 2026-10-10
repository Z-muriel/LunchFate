using Volo.Abp.Application.Dtos;

namespace LunchFate.Shops;

/// <summary>
/// 店铺列表查询入参（需求文档 FR-S-01）。
/// 继承 <see cref="PagedAndSortedResultRequestDto"/> 以便直接用于 ABP 分页/排序，
/// 页面传入本类型，应用服务内按本类型解析筛选条件。
/// </summary>
public class ShopSearchInput : PagedAndSortedResultRequestDto
{
    /// <summary>关键字：匹配店铺名称或标签。</summary>
    public string? Keyword { get; set; }

    /// <summary>分类筛选；null 表示不按分类筛选。</summary>
    public ShopCategory? Category { get; set; }

    /// <summary>null 表示不按启用状态筛选。</summary>
    public bool? IsEnabled { get; set; }
}
