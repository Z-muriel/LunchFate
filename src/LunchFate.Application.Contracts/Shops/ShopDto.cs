using System;
using Volo.Abp.Application.Dtos;

namespace LunchFate.Shops;

/// <summary>
/// 店铺查询结果 DTO（列表与详情共用），含审计字段。
/// </summary>
public class ShopDto : FullAuditedEntityDto<Guid>
{
    /// <summary>店铺名称，必填、唯一（不含已删除数据）。</summary>
    public string Name { get; set; } = default!;

    /// <summary>店铺分类。</summary>
    public ShopCategory Category { get; set; }

    /// <summary>地址，可空。</summary>
    public string? Address { get; set; }

    /// <summary>经度（GCJ-02），可空。</summary>
    public double? Longitude { get; set; }

    /// <summary>纬度（GCJ-02），可空。</summary>
    public double? Latitude { get; set; }

    /// <summary>联系电话，可空。</summary>
    public string? Phone { get; set; }

    /// <summary>人均价格（元）。</summary>
    public decimal AveragePrice { get; set; }

    /// <summary>评分 1.0 ~ 5.0，未评分为 null。</summary>
    public decimal? Rating { get; set; }

    /// <summary>标签，逗号分隔。</summary>
    public string? Tags { get; set; }

    /// <summary>抽取权重（需求文档 FR-W-08，P1 生效），P0 恒为 1 表示等概率。</summary>
    public int Weight { get; set; }

    /// <summary>是否启用；停用的店铺不进入转盘（需求文档 FR-S-05）。</summary>
    public bool IsEnabled { get; set; }

    /// <summary>备注，可空。</summary>
    public string? Remark { get; set; }
}
