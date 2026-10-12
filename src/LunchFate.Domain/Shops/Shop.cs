using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LunchFate.Shops;

/// <summary>
/// 午餐店铺聚合根。
/// 继承 <see cref="FullAuditedAggregateRoot{TKey}"/> 以获得审计字段与软删除（需求文档 FR-S-04）。
/// </summary>
public class Shop : FullAuditedAggregateRoot<Guid>
{
    /// <summary>店铺名称，必填、唯一（不含已删除数据）。</summary>
    public string Name { get; set; } = default!;

    /// <summary>店铺分类，需求文档 §3.2 的 9 个枚举值之一。</summary>
    public ShopCategory Category { get; set; } = ShopCategory.Other;

    /// <summary>地址，可空。</summary>
    public string? Address { get; set; }

    /// <summary>经度（GCJ-02 坐标系），可空。由高德地理编码服务自动填充。</summary>
    public double? Longitude { get; set; }

    /// <summary>纬度（GCJ-02 坐标系），可空。由高德地理编码服务自动填充。</summary>
    public double? Latitude { get; set; }

    /// <summary>联系电话，可空。</summary>
    public string? Phone { get; set; }

    /// <summary>人均价格。</summary>
    public decimal AveragePrice { get; set; }

    /// <summary>评分 1.0 ~ 5.0，未评分为 null。</summary>
    public decimal? Rating { get; set; }

    /// <summary>标签，逗号分隔。</summary>
    public string? Tags { get; set; }

    /// <summary>抽取权重（需求文档 FR-W-08，P1 生效），P0 恒为 1 表示等概率。</summary>
    public int Weight { get; set; } = 1;

    /// <summary>是否启用；停用的店铺不进入转盘（需求文档 FR-S-05）。</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>备注，可空。</summary>
    public string? Remark { get; set; }
}
