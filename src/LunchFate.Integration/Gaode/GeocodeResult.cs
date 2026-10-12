namespace LunchFate.Integration.Gaode;

/// <summary>
/// 地理编码结果。
/// </summary>
public class GeocodeResult
{
    /// <summary>
    /// 经度（GCJ-02 坐标系）。
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// 纬度（GCJ-02 坐标系）。
    /// </summary>
    public double Latitude { get; set; }
}
