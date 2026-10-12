namespace LunchFate.Maps;

/// <summary>
/// 附近搜索的默认中心点，来源为服务端配置（Integration:Gaode 的 Default* 项）。
/// 浏览器端可用本地存储覆盖，因此该值只作为"未设置本地默认位置时"的兜底。
/// </summary>
public class MapDefaultLocationDto
{
    /// <summary>经度（GCJ-02）。</summary>
    public double Longitude { get; set; }

    /// <summary>纬度（GCJ-02）。</summary>
    public double Latitude { get; set; }

    /// <summary>显示名称，可为空。</summary>
    public string? Name { get; set; }
}
