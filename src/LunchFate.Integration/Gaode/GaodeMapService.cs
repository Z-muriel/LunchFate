using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LunchFate.Integration.Gaode;

/// <summary>
/// 高德地图服务实现，封装地理编码、周边 POI 搜索、天气查询与距离计算。
/// 所有外部调用均为失败安全：异常时返回 null 或空集合，不阻断业务流程。
/// </summary>
public class GaodeMapService : IGaodeMapService
{
    /// <summary>高德周边搜索允许的最大半径（米）。</summary>
    private const int MaxRadius = 50000;

    private readonly HttpClient _httpClient;
    private readonly GaodeMapOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GaodeMapService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 初始化高德地图服务。
    /// </summary>
    /// <param name="httpClient">HTTP 客户端</param>
    /// <param name="options">配置项</param>
    /// <param name="cache">内存缓存</param>
    /// <param name="logger">日志记录器</param>
    public GaodeMapService(
        HttpClient httpClient,
        IOptions<GaodeMapOptions> options,
        IMemoryCache cache,
        ILogger<GaodeMapService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GeocodeResult?> GeocodeAsync(string address, string? city = null, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled() || string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var cacheKey = $"geocode:{address.GetHashCode()}";
        if (_cache.TryGetValue(cacheKey, out GeocodeResult? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            // 1. 拼接地理编码请求 URL（/v3/geocode/geo）
            var url = $"/v3/geocode/geo?key={_options.Key}&address={Uri.EscapeDataString(address)}";
            if (!string.IsNullOrWhiteSpace(city))
            {
                url += $"&city={Uri.EscapeDataString(city)}";
            }

            // 2. 发送请求并解析响应
            var response = await _httpClient.GetFromJsonAsync<GaodeGeocodeResponse>(url, JsonOptions, cancellationToken);
            if (response?.Geocodes is null || response.Geocodes.Count == 0)
            {
                // status=0 时（如 QPS 限流 CUQPS_HAS_EXCEEDED_THE_LIMIT）也走这里，
                // 不记日志会让"坐标丢失"在 logs.txt 里查无痕迹。
                _logger.LogWarning(
                    "地理编码无结果：{Address} / status={Status} info={Info} infocode={InfoCode}",
                    address,
                    response?.Status,
                    response?.Info,
                    response?.InfoCode);
                return null;
            }

            // 3. 解析 location 字段（格式："经度,纬度"）
            var location = response.Geocodes[0].Location.Split(',');
            var result = new GeocodeResult
            {
                Longitude = double.Parse(location[0], CultureInfo.InvariantCulture),
                Latitude = double.Parse(location[1], CultureInfo.InvariantCulture)
            };

            // 4. 写入缓存（24 小时）
            _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "地理编码失败：{Address}", address);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<PoiSearchPage> SearchAroundAsync(
        string keywords,
        string types,
        double longitude,
        double latitude,
        int radius,
        int pageNum = 1,
        int pageSize = IGaodeMapService.MaxPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled())
        {
            // 服务未启用（未配置 Key）按失败返回，让上层提示"服务不可用"而不是"没有结果"。
            return new PoiSearchPage { Failed = true };
        }

        // 高德限制：页码从 1 开始、单页最多 25 条。越界时夹取而不是抛异常（降级优先，不阻断业务）。
        var safePageNum = Math.Max(1, pageNum);
        var safePageSize = Math.Clamp(pageSize, 1, IGaodeMapService.MaxPageSize);
        var safeRadius = Math.Clamp(radius, 1, MaxRadius);

        // 坐标必须用不变文化格式化，否则逗号小数分隔符的地区会把经纬度写坏。
        var location = string.Create(
            CultureInfo.InvariantCulture,
            $"{longitude},{latitude}");

        try
        {
            // 1. 拼接周边搜索请求 URL（/v3/place/around）
            //    用 v3 而非 v5：v5 的 count 是本页条数，算不出总页数；v3 的 count 才是真实总数。
            //    sortrule=distance 保证结果跨页全局按距离升序。
            var url = $"/v3/place/around?key={_options.Key}&location={Uri.EscapeDataString(location)}" +
                      $"&radius={safeRadius}&offset={safePageSize}&page={safePageNum}" +
                      "&extensions=all&sortrule=distance";

            if (!string.IsNullOrWhiteSpace(keywords))
            {
                url += $"&keywords={Uri.EscapeDataString(keywords)}";
            }

            // 类型召回：优先按类型筛选（如 050000 餐饮服务）；
            // 两者都为空时高德自己回退为餐饮/生活服务/商务住宅三类，这里不重复拼参数。
            if (!string.IsNullOrWhiteSpace(types))
            {
                url += $"&types={Uri.EscapeDataString(types)}";
            }

            // 2. 发送请求并解析响应
            var response = await _httpClient.GetFromJsonAsync<GaodeAroundResponse>(url, JsonOptions, cancellationToken);

            // status!=1（限流 CUQPS_HAS_EXCEEDED_THE_LIMIT、Key 无效等）时高德连 pois 字段都不返回；
            // 若按"pois 为 null"当成空结果页，故障会被前端伪装成"附近没有匹配的店铺"，且日志无痕迹。
            if (response is null || response.Status != "1")
            {
                _logger.LogWarning(
                    "周边搜索返回非成功状态：status={Status} info={Info} infocode={InfoCode} keywords={Keywords} types={Types}",
                    response?.Status,
                    response?.Info,
                    response?.InfoCode,
                    keywords,
                    types);
                return new PoiSearchPage { Failed = true };
            }

            // status=1 但 pois 缺失按成功空页处理（高德"查无结果"时仍返回 status=1）。
            if (response.Pois is null)
            {
                return new PoiSearchPage();
            }

            // 3. 映射 POI 列表；坐标缺失的脏数据直接跳过，不污染结果
            var items = response.Pois
                .Select(MapPoi)
                .OfType<PoiSearchResult>()
                .ToList();

            // 4. 总数缺失或非法时回退为"至少这么多、可能还有下一页"，
            //    避免前端把"下一页"按钮永久禁用（这正是换用 v3 前的问题）。
            var fallbackTotal = ((safePageNum - 1) * safePageSize) + items.Count +
                                (items.Count == safePageSize ? 1 : 0);

            return new PoiSearchPage
            {
                Items = items,
                TotalCount = ParseTotalCount(response.Count, fallbackTotal)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "周边搜索失败：{Keywords} / {Types}", keywords, types);
            return new PoiSearchPage { Failed = true };
        }
    }

    /// <inheritdoc />
    public async Task<WeatherResult?> GetWeatherAsync(string adcode, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled() || string.IsNullOrWhiteSpace(adcode))
        {
            return null;
        }

        try
        {
            // 1. 拼接天气查询请求 URL（/v3/weather/weatherInfo）
            //    adcode 来自客户端入参，必须转义，否则可被注入额外查询参数。
            var url = $"/v3/weather/weatherInfo?key={_options.Key}&city={Uri.EscapeDataString(adcode)}&extensions=base";

            // 2. 发送请求并解析响应
            var response = await _httpClient.GetFromJsonAsync<GaodeWeatherResponse>(url, JsonOptions, cancellationToken);

            // status!=1 时高德不返回 lives；不记日志会让"天气组件静默隐藏"无从排查。
            if (response is null || response.Status != "1")
            {
                _logger.LogWarning(
                    "天气查询返回非成功状态：status={Status} info={Info} infocode={InfoCode} adcode={Adcode}",
                    response?.Status,
                    response?.Info,
                    response?.InfoCode,
                    adcode);
                return null;
            }

            // status=1 但 lives 缺失或为空时按无天气处理，不视为故障。
            if (response.Lives is null || response.Lives.Count == 0)
            {
                return null;
            }

            // 3. 映射天气结果
            var live = response.Lives[0];
            return new WeatherResult
            {
                Weather = live.Weather,
                Temperature = live.Temperature,
                WindDirection = live.WindDirection,
                WindPower = live.WindPower,
                Humidity = live.Humidity
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "天气查询失败：{Adcode}", adcode);
            return null;
        }
    }

    /// <inheritdoc />
    public double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadius = 6371000.0;

        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadius * c;
    }

    /// <summary>
    /// 把高德周边搜索的 POI 响应映射为结果项；坐标缺失或非法的脏数据返回 null，由调用方跳过。
    /// </summary>
    /// <param name="poi">高德响应中的 pois 元素</param>
    /// <returns>结果项；坐标不可用时为 null</returns>
    private static PoiSearchResult? MapPoi(GaodePoi poi)
    {
        var parts = (poi.Location ?? string.Empty).Split(',');
        if (parts.Length != 2 ||
            !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude))
        {
            return null;
        }

        double? distance = null;
        if (double.TryParse(poi.Distance, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDistance))
        {
            distance = parsedDistance;
        }

        return new PoiSearchResult
        {
            Id = poi.Id ?? string.Empty,
            Name = poi.Name ?? string.Empty,
            Longitude = longitude,
            Latitude = latitude,
            Type = poi.Type ?? string.Empty,
            Address = poi.Address ?? string.Empty,
            Tel = poi.Tel,
            Rating = poi.BizExt?.Rating,
            DistanceMeters = distance
        };
    }

    /// <summary>
    /// 解析高德响应中的匹配总数（count 为字符串）。
    /// 缺失或非法时回退为调用方计算的"至少这么多、可能还有下一页"，保证分页控件不会把下一页永久禁用。
    /// </summary>
    /// <param name="count">高德响应的 count 字段</param>
    /// <param name="fallback">解析失败时使用的下界估计</param>
    /// <returns>非负的匹配总数</returns>
    private static int ParseTotalCount(string? count, int fallback)
    {
        return int.TryParse(count, NumberStyles.Integer, CultureInfo.InvariantCulture, out var total) && total >= 0
            ? total
            : fallback;
    }

    /// <summary>
    /// 检查服务是否可用（已启用且配置了 Key）。
    /// </summary>
    private bool IsEnabled()
    {
        return _options.Enabled && !string.IsNullOrWhiteSpace(_options.Key);
    }

    /// <summary>
    /// 角度转弧度。
    /// </summary>
    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    #region 高德 API 响应模型

    private class GaodeGeocodeResponse : GaodeResponse
    {
        public List<GaodeGeocode>? Geocodes { get; set; }
    }

    private class GaodeGeocode
    {
        public string Location { get; set; } = string.Empty;
    }

    private class GaodeAroundResponse : GaodeResponse
    {
        public List<GaodePoi>? Pois { get; set; }
    }

    /// <summary>
    /// 高德周边搜索的 POI 响应模型。
    /// 所有文本字段都走 <see cref="FlexibleStringJsonConverter"/>：v3 的 tel/address 等
    /// 在"没有该信息"时会返回空数组，直接映射成 string 会抛 JsonException。
    /// </summary>
    private class GaodePoi
    {
        [JsonConverter(typeof(FlexibleStringJsonConverter))]
        public string? Id { get; set; }

        [JsonConverter(typeof(FlexibleStringJsonConverter))]
        public string? Name { get; set; }

        [JsonConverter(typeof(FlexibleStringJsonConverter))]
        public string? Location { get; set; }

        [JsonConverter(typeof(FlexibleStringJsonConverter))]
        public string? Type { get; set; }

        [JsonConverter(typeof(FlexibleStringJsonConverter))]
        public string? Address { get; set; }

        [JsonConverter(typeof(FlexibleStringJsonConverter))]
        public string? Tel { get; set; }

        /// <summary>距搜索中心的距离（米），高德以字符串返回。</summary>
        [JsonConverter(typeof(FlexibleStringJsonConverter))]
        public string? Distance { get; set; }

        /// <summary>扩展信息，包含评分与人均消费。</summary>
        [JsonPropertyName("biz_ext")]
        public GaodeBizExt? BizExt { get; set; }
    }

    private class GaodeBizExt
    {
        [JsonConverter(typeof(FlexibleStringJsonConverter))]
        public string? Rating { get; set; }

        [JsonConverter(typeof(FlexibleStringJsonConverter))]
        public string? Cost { get; set; }
    }

    private class GaodeWeatherResponse : GaodeResponse
    {
        public List<GaodeWeatherLive>? Lives { get; set; }
    }

    private class GaodeWeatherLive
    {
        public string Weather { get; set; } = string.Empty;
        public string Temperature { get; set; } = string.Empty;
        public string WindDirection { get; set; } = string.Empty;
        public string WindPower { get; set; } = string.Empty;
        public string Humidity { get; set; } = string.Empty;
    }

    #endregion
}
