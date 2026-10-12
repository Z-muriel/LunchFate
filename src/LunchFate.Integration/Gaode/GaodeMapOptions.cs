namespace LunchFate.Integration.Gaode;

/// <summary>
/// 高德地图服务配置项。
/// </summary>
public class GaodeMapOptions
{
    /// <summary>
    /// 高德开放平台 Web 服务 Key。为空时服务整体禁用。
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// API 基础地址，默认高德 REST API。
    /// </summary>
    public string BaseUrl { get; set; } = "https://restapi.amap.com";

    /// <summary>
    /// HTTP 超时时间（秒），默认 3 秒。
    /// </summary>
    public int TimeoutSeconds { get; set; } = 3;

    /// <summary>
    /// 是否启用地图服务。为 false 时所有方法直接返回 null/空。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否接受无效的服务器证书。默认 false，仅在本机存在 TLS 拦截代理等调试场景下按需开启；
    /// 生产环境必须保持 false，否则高德请求可被中间人篡改。
    /// </summary>
    public bool AllowInvalidServerCertificate { get; set; }

    /// <summary>
    /// 附近搜索的默认中心点经度（GCJ-02）。浏览器未设置本地默认位置时使用。
    /// </summary>
    public double DefaultLongitude { get; set; } = 113.293333;

    /// <summary>
    /// 附近搜索的默认中心点纬度（GCJ-02）。浏览器未设置本地默认位置时使用。
    /// </summary>
    public double DefaultLatitude { get; set; } = 23.092112;

    /// <summary>
    /// 默认位置的显示名称，用于界面提示，如"中山大学广州校区南校园"。
    /// </summary>
    public string DefaultLocationName { get; set; } = "中山大学广州校区南校园";
}
