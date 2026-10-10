using System;
using Volo.Abp.Application.Dtos;

namespace LunchFate.Shops;

/// <summary>
/// 大转盘专用的轻量 DTO（需求文档 §6）：仅包含扇区展示与结果弹窗所需的字段。
/// </summary>
public class EnabledShopDto : EntityDto<Guid>
{
    /// <summary>店铺名称。</summary>
    public string Name { get; set; } = default!;

    /// <summary>店铺分类。</summary>
    public ShopCategory Category { get; set; }

    /// <summary>人均价格（元）。</summary>
    public decimal AveragePrice { get; set; }

    /// <summary>地址，可空。</summary>
    public string? Address { get; set; }
}
