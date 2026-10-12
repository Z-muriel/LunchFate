namespace LunchFate.Maps;

/// <summary>
/// 附近 POI 搜索结果。
/// </summary>
public class NearbyPoiDto : MapMarkerDto
{
    /// <summary>高德 POI Id。</summary>
    public string? Id { get; set; }

    /// <summary>分类文本。</summary>
    public string? Type { get; set; }

    /// <summary>联系电话。</summary>
    public string? Tel { get; set; }

    /// <summary>评分。</summary>
    public string? Rating { get; set; }

    /// <summary>距离当前位置的直线距离（米）。</summary>
    public double DistanceMeters { get; set; }
}
