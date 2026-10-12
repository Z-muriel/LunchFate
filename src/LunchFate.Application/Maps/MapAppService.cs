using System.Linq;
using System.Threading.Tasks;
using LunchFate.Integration.Gaode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LunchFate.Maps;

/// <summary>
/// 地图应用服务实现。
/// </summary>
[Authorize]
public class MapAppService : LunchFateAppService, IMapAppService
{
    private readonly IGaodeMapService _gaodeMapService;
    private readonly GaodeMapOptions _gaodeOptions;

    /// <summary>初始化地图应用服务。</summary>
    public MapAppService(IGaodeMapService gaodeMapService, IOptions<GaodeMapOptions> gaodeOptions)
    {
        _gaodeMapService = gaodeMapService;
        _gaodeOptions = gaodeOptions.Value;
    }

    /// <inheritdoc />
    [Authorize]
    public async Task<MapWeatherDto?> GetWeatherAsync(string adcode)
    {
        var result = await _gaodeMapService.GetWeatherAsync(adcode);
        return result == null
            ? null
            : new MapWeatherDto
            {
                Weather = result.Weather,
                Temperature = result.Temperature,
                WindDirection = result.WindDirection,
                WindPower = result.WindPower,
                Humidity = result.Humidity
            };
    }

    /// <inheritdoc />
    [Authorize]
    public Task<MapDefaultLocationDto> GetDefaultLocationAsync()
    {
        return Task.FromResult(new MapDefaultLocationDto
        {
            Longitude = _gaodeOptions.DefaultLongitude,
            Latitude = _gaodeOptions.DefaultLatitude,
            Name = _gaodeOptions.DefaultLocationName
        });
    }

    /// <inheritdoc />
    [Authorize]
    public async Task<PagedResultDto<NearbyPoiDto>> SearchNearbyAsync(NearbySearchInput input)
    {
        // 1. 服务端校验分页：不信任客户端传入的 SkipCount / MaxResultCount。
        //    高德周边搜索单页上限为 MaxPageSize，超出上限直接拒绝而不是静默截断，避免页码与结果错位。
        if (input == null ||
            input.SkipCount < 0 ||
            input.MaxResultCount < 1 ||
            input.MaxResultCount > IGaodeMapService.MaxPageSize)
        {
            throw new UserFriendlyException(L["Map:InvalidPaging"].Value);
        }

        // 2. SkipCount（0 基）换算为高德页码（1 基）
        var pageNum = (input.SkipCount / input.MaxResultCount) + 1;

        // 3. 调用高德周边搜索：由集成层负责页码/每页条数的边界夹取与失败降级。
        //    高德按距离升序返回且跨页连续，distance 直接来自响应，无需前端重算。
        var page = await _gaodeMapService.SearchAroundAsync(
            input.Keywords,
            input.Longitude,
            input.Latitude,
            input.Radius,
            pageNum: pageNum,
            pageSize: input.MaxResultCount);

        // 集成层不会抛异常，失败以 Failed 标记返回；这里显式转成用户可读错误，
        // 否则"服务不可用"会被前端误报成"附近没有找到匹配的店铺"。
        if (page.Failed)
        {
            throw new UserFriendlyException(L["Map:SearchFailed"].Value);
        }

        // 4. 投影；距离缺失时用直线距离兜底，保证展示与排序都有值。
        var items = page.Items.Select(item => new NearbyPoiDto
        {
            Id = item.Id,
            Name = item.Name,
            Longitude = item.Longitude,
            Latitude = item.Latitude,
            Address = item.Address,
            Type = item.Type,
            Tel = item.Tel,
            Rating = item.Rating,
            DistanceMeters = item.DistanceMeters ?? _gaodeMapService.CalculateDistance(
                input.Latitude,
                input.Longitude,
                item.Latitude,
                item.Longitude)
        }).OrderBy(item => item.DistanceMeters).ToList();

        // 5. 返回分页结果：TotalCount 是高德返回的真实匹配总数，前端据此计算总页数。
        return new PagedResultDto<NearbyPoiDto>(page.TotalCount, items);
    }

    /// <inheritdoc />
    [Authorize]
    public Task<double> CalculateDistanceAsync(
        double latitude,
        double longitude,
        double targetLatitude,
        double targetLongitude)
    {
        return Task.FromResult(_gaodeMapService.CalculateDistance(
            latitude, longitude, targetLatitude, targetLongitude));
    }
}
