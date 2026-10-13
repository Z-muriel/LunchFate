using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace LunchFate.Maps;

/// <summary>
/// 附近 POI 搜索参数（分页查询）。
/// 继承 <see cref="PagedResultRequestDto"/> 以复用 ABP 的 SkipCount / MaxResultCount 约定，
/// 前端翻页时只改这两个字段，无需自定义分页字段。
/// </summary>
public class NearbySearchInput : PagedResultRequestDto
{
    /// <summary>搜索关键字。可为空：为空时只按 <see cref="Types"/> 召回附近同类店铺。</summary>
    [StringLength(80)]
    public string Keywords { get; set; } = string.Empty;

    /// <summary>
    /// POI 类型编码，多个用"|"分隔（如 050000 餐饮服务）。可为空；
    /// 只做长度约束，具体取值交给高德（非法值只会查不到结果，不会有注入风险，参数经 URL 转义）。
    /// </summary>
    [StringLength(32)]
    public string Types { get; set; } = string.Empty;

    /// <summary>当前位置经度（GCJ-02）。</summary>
    [Range(-180, 180)]
    public double Longitude { get; set; }

    /// <summary>当前位置纬度（GCJ-02）。</summary>
    [Range(-90, 90)]
    public double Latitude { get; set; }

    /// <summary>搜索半径（米）。</summary>
    [Range(100, 50000)]
    public int Radius { get; set; } = 3000;
}
