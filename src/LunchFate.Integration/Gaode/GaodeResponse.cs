using System.Text.Json.Serialization;

namespace LunchFate.Integration.Gaode;

/// <summary>
/// 高德 API 统一响应封装。
/// </summary>
public class GaodeResponse
{
    /// <summary>
    /// 状态码：1 成功，0 失败。
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 返回状态说明，成功为 "OK"，失败为错误原因。
    /// </summary>
    [JsonPropertyName("info")]
    public string Info { get; set; } = string.Empty;

    /// <summary>
    /// 状态码详情，10000 代表正确。
    /// </summary>
    [JsonPropertyName("infocode")]
    public string InfoCode { get; set; } = string.Empty;

    /// <summary>
    /// 单次请求返回的结果个数（v3 周边搜索的 count 为匹配总数；v5 为本页条数）。
    /// </summary>
    [JsonPropertyName("count")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string Count { get; set; } = string.Empty;

    /// <summary>
    /// 请求是否成功。
    /// </summary>
    public bool IsSuccess => Status == "1";
}
