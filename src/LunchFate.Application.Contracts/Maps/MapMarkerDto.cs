namespace LunchFate.Maps;

/// <summary>
/// 地图标点的通用数据。
/// </summary>
public class MapMarkerDto
{
    /// <summary>经度（GCJ-02）。</summary>
    public double Longitude { get; set; }

    /// <summary>纬度（GCJ-02）。</summary>
    public double Latitude { get; set; }

    /// <summary>标点名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>地址。</summary>
    public string? Address { get; set; }
}
