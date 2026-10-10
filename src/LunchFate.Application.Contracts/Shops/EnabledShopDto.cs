using System;
using Volo.Abp.Application.Dtos;

namespace LunchFate.Shops;

/// <summary>
/// 大转盘专用的轻量 DTO（需求文档 §6）：仅包含扇区展示与结果弹窗所需的字段。
/// </summary>
public class EnabledShopDto : EntityDto<Guid>
{
    public string Name { get; set; } = default!;

    public ShopCategory Category { get; set; }

    public decimal AveragePrice { get; set; }

    public string? Address { get; set; }
}
