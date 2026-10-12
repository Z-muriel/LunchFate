namespace LunchFate.Integration.Gaode;

/// <summary>
/// 高德地图服务接口，提供地理编码、周边 POI 搜索、天气查询与距离计算能力。
/// 所有方法均为失败安全设计：异常时返回 null 或空集合，不抛出阻断业务。
/// </summary>
public interface IGaodeMapService
{
    /// <summary>
    /// 周边搜索单页可返回的最大条数（高德 /v3/place/around 的 offset 上限）。
    /// 上层校验分页参数时复用该常量，避免把超出上限的请求发往高德。
    /// </summary>
    public const int MaxPageSize = 25;

    /// <summary>
    /// 地理编码：将地址文本转换为 GCJ-02 坐标经纬度。
    /// </summary>
    /// <param name="address">结构化地址文本，如"北京市朝阳区阜通东大街6号"</param>
    /// <param name="city">指定查询城市（中文/全拼/citycode/adcode），可为 null 则全国检索</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>经纬度结果；失败返回 null</returns>
    Task<GeocodeResult?> GeocodeAsync(string address, string? city = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 周边搜索（高德 /v3/place/around）：以给定坐标为中心、在半径内按关键字检索 POI。
    /// 结果按距离升序，且 <see cref="PoiSearchPage.TotalCount"/> 是真实匹配总数，可直接用于分页。
    /// </summary>
    /// <remarks>
    /// 这里用 v3 而不是 v5 的 /v5/place/around：v5 的 count 字段返回的是"本页条数"，
    /// 算不出总页数（实测周边搜索每页都返回 page_size，导致前端"下一页"永远不可点）；
    /// v3 的 count 是真实匹配总数，且响应直接带 distance。
    /// </remarks>
    /// <param name="keywords">搜索关键字，可为空（为空时返回半径内全部 POI）</param>
    /// <param name="longitude">搜索中心经度（GCJ-02）</param>
    /// <param name="latitude">搜索中心纬度（GCJ-02）</param>
    /// <param name="radius">搜索半径（米）</param>
    /// <param name="pageNum">页码，从 1 开始；小于 1 时按 1 处理</param>
    /// <param name="pageSize">每页条数，超出 <see cref="MaxPageSize"/> 时按上限夹取</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>单页结果与匹配总数；失败返回空页</returns>
    Task<PoiSearchPage> SearchAroundAsync(
        string keywords,
        double longitude,
        double latitude,
        int radius,
        int pageNum = 1,
        int pageSize = MaxPageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询城市实时天气（高德 /v3/weather/weatherInfo）。
    /// </summary>
    /// <param name="adcode">城市 adcode（如北京 110000）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>天气结果；失败返回 null</returns>
    Task<WeatherResult?> GetWeatherAsync(string adcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 计算两坐标间直线距离（米），使用 Haversine 公式。
    /// </summary>
    /// <param name="lat1">起点纬度</param>
    /// <param name="lng1">起点经度</param>
    /// <param name="lat2">终点纬度</param>
    /// <param name="lng2">终点经度</param>
    /// <returns>直线距离（米）</returns>
    double CalculateDistance(double lat1, double lng1, double lat2, double lng2);
}
