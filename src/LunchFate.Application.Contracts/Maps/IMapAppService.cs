using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LunchFate.Maps;

/// <summary>
/// 地图应用服务：统一向前端提供天气、附近 POI 与距离能力。
/// </summary>
public interface IMapAppService : IApplicationService
{
    /// <summary>查询城市实时天气。</summary>
    Task<MapWeatherDto?> GetWeatherAsync(string adcode);

    /// <summary>
    /// 获取服务端配置的默认搜索中心；浏览器未设置本地默认位置时使用。
    /// 单一数据源放在服务端，避免 Blazor 两种渲染模式各读一份客户端配置导致首屏不一致。
    /// </summary>
    Task<MapDefaultLocationDto> GetDefaultLocationAsync();

    /// <summary>搜索当前位置附近的 POI，支持分页浏览超过单页上限的更多结果。</summary>
    Task<PagedResultDto<NearbyPoiDto>> SearchNearbyAsync(NearbySearchInput input);

    /// <summary>计算两点间直线距离。</summary>
    Task<double> CalculateDistanceAsync(double latitude, double longitude, double targetLatitude, double targetLongitude);
}
