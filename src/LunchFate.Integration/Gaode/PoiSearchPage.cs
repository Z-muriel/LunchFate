using System.Collections.Generic;

namespace LunchFate.Integration.Gaode;

/// <summary>
/// 周边搜索的单页结果：除本页数据外还带回高德返回的匹配总数，供上层计算总页数。
/// 失败安全：查询失败或服务未启用时返回空页（Items 为空、TotalCount 为 0），不抛异常。
/// </summary>
public class PoiSearchPage
{
    /// <summary>本页 POI 列表，已按距搜索中心的距离升序。</summary>
    public List<PoiSearchResult> Items { get; set; } = [];

    /// <summary>
    /// 符合关键字的真实匹配总数（取自高德 /v3/place/around 的 count 字段），用于分页控件展示总页数。
    /// 注意：该值可能大于高德实际可翻页返回的条数，上层需容忍"后续页为空"。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 本次查询是否失败（服务未启用、网络异常或响应无法解析）。
    /// 用于把"查询失败"和"确实没有结果"区分开——否则失败会被上层误报成"附近没有匹配的店铺"。
    /// </summary>
    public bool Failed { get; set; }
}
