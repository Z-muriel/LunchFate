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
    /// <summary>搜索关键字。</summary>
    [Required]
    [StringLength(80)]
    public string Keywords { get; set; } = string.Empty;

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
