using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace LunchFate.Shops;

/// <summary>
/// 店铺新增/编辑入参；数据注解同时用于前端 <c>DataAnnotationsValidator</c> 与服务端自动校验。
/// </summary>
public class CreateUpdateShopDto
{
    /// <summary>店铺名称，必填且有长度上限。</summary>
    [Required]
    [StringLength(ShopConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    /// <summary>店铺分类，必填。</summary>
    [Required]
    public ShopCategory Category { get; set; } = ShopCategory.Other;

    /// <summary>地址，可空。</summary>
    [StringLength(ShopConsts.MaxAddressLength)]
    public string? Address { get; set; }

    /// <summary>
    /// 经度（GCJ-02），可空。仅用于把地图 POI 的已知坐标原样带入门店库；
    /// 未提供时由服务端按地址地理编码补齐（需求文档 FR-M-01）。
    /// </summary>
    [Range(-180d, 180d)]
    public double? Longitude { get; set; }

    /// <summary>
    /// 纬度（GCJ-02），可空。与 <see cref="Longitude"/> 必须同时提供，只提供其一时忽略并由服务端地理编码补齐。
    /// </summary>
    [Range(-90d, 90d)]
    public double? Latitude { get; set; }

    /// <summary>联系电话，可空。</summary>
    [StringLength(ShopConsts.MaxPhoneLength)]
    public string? Phone { get; set; }

    /// <summary>人均价格（元）。</summary>
    [Range(0, 100000)]
    public decimal AveragePrice { get; set; }

    /// <summary>评分 1.0 ~ 5.0、步进 0.5，未评分为 null。</summary>
    [Range(1, 5)]
    [MultipleOf(0.5)]
    public decimal? Rating { get; set; }

    /// <summary>标签，逗号分隔。</summary>
    [StringLength(ShopConsts.MaxTagsLength)]
    public string? Tags { get; set; }

    /// <summary>抽取权重（需求文档 FR-W-08，P1 生效），P0 恒为 1 表示等概率。</summary>
    [Range(1, 100)]
    public int Weight { get; set; } = 1;

    /// <summary>是否启用；停用的店铺不进入转盘（需求文档 FR-S-05）。</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>备注，可空。</summary>
    [StringLength(ShopConsts.MaxRemarkLength)]
    public string? Remark { get; set; }
}
