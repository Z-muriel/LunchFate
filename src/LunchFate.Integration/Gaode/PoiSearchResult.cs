namespace LunchFate.Integration.Gaode;

/// <summary>
/// 周边搜索结果项（对应高德 /v3/place/around 返回的 pois 元素）。
/// 只保留应用实际使用的字段，避免把高德的完整响应结构泄漏到上层。
/// </summary>
public class PoiSearchResult
{
    /// <summary>
    /// POI 唯一标识。
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 地点名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 经度（GCJ-02）。
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// 纬度（GCJ-02）。
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// POI 类型描述，如"餐饮服务;中餐厅;中餐厅"。
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 详细地址。
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// 联系电话。
    /// </summary>
    public string? Tel { get; set; }

    /// <summary>
    /// 评分（取自 biz_ext.rating）。
    /// </summary>
    public string? Rating { get; set; }

    /// <summary>
    /// 距搜索中心的直线距离（米），由高德直接返回。
    /// </summary>
    public double? DistanceMeters { get; set; }
}
