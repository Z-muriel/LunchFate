namespace LunchFate.Integration.Gaode;

/// <summary>
/// 天气查询结果。
/// </summary>
public class WeatherResult
{
    /// <summary>
    /// 天气现象，如"晴"、"小雨"。
    /// </summary>
    public string Weather { get; set; } = string.Empty;

    /// <summary>
    /// 实时温度（摄氏度）。
    /// </summary>
    public string Temperature { get; set; } = string.Empty;

    /// <summary>
    /// 风向。
    /// </summary>
    public string WindDirection { get; set; } = string.Empty;

    /// <summary>
    /// 风力等级。
    /// </summary>
    public string WindPower { get; set; } = string.Empty;

    /// <summary>
    /// 空气湿度（百分比）。
    /// </summary>
    public string Humidity { get; set; } = string.Empty;
}
