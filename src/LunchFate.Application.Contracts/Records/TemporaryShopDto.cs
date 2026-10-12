using System.ComponentModel.DataAnnotations;

namespace LunchFate.Records;

/// <summary>
/// 客户端声明的临时地图候选快照，不代表已验证的库内店铺。
/// </summary>
public class TemporaryShopDto
{
    /// <summary>地图兴趣点标识，必填，最大长度为 128。</summary>
    [Required]
    [StringLength(128)]
    public string PoiId { get; set; } = string.Empty;

    /// <summary>地图兴趣点名称，必填，最大长度为 256。</summary>
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>地址快照，最大长度为 256，可空。</summary>
    [StringLength(256)]
    public string? Address { get; set; }

    /// <summary>经度，可空，非空时必须为有限值且位于 -180 至 180。</summary>
    [Range(-180d, 180d)]
    public double? Longitude { get; set; }

    /// <summary>纬度，可空，非空时必须为有限值且位于 -90 至 90。</summary>
    [Range(-90d, 90d)]
    public double? Latitude { get; set; }
}
