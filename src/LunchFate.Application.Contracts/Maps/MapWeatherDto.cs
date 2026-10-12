namespace LunchFate.Maps;

/// <summary>
/// 城市实时天气结果。
/// </summary>
public class MapWeatherDto
{
    /// <summary>天气现象。</summary>
    public string Weather { get; set; } = string.Empty;

    /// <summary>温度。</summary>
    public string Temperature { get; set; } = string.Empty;

    /// <summary>风向。</summary>
    public string WindDirection { get; set; } = string.Empty;

    /// <summary>风力。</summary>
    public string WindPower { get; set; } = string.Empty;

    /// <summary>湿度。</summary>
    public string Humidity { get; set; } = string.Empty;
}
