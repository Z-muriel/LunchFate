using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LunchFate.Integration.Gaode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace LunchFate.EntityFrameworkCore.Applications;

/// <summary>
/// 仅用于地理编码用例：把高德服务替换为可控的测试替身，验证保存流程是否按预期调用地理编码。
/// 不改变其他测试模块，避免影响真实降级行为（Key 为空即返回 null）的既有用例。
/// </summary>
[DependsOn(typeof(LunchFateEntityFrameworkCoreTestModule))]
public class LunchFateShopGeocodeTestModule : AbpModule
{
    /// <summary>
    /// 移除高德 HttpClient 注册，改为把 <see cref="IGaodeMapService"/> 指向同一个记录型单例，
    /// 保证被注入到应用服务的实例与用例读取的实例一致。
    /// </summary>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.RemoveAll<IGaodeMapService>();
        context.Services.AddSingleton<IGaodeMapService>(sp => sp.GetRequiredService<RecordingGaodeMapService>());
    }
}

/// <summary>
/// 记录地理编码入参并返回确定性坐标的测试替身，用于断言"何时调用了地理编码"。
/// </summary>
public class RecordingGaodeMapService : IGaodeMapService, ISingletonDependency
{
    private readonly ConcurrentQueue<string> _geocodeRequests = new();

    /// <summary>按调用顺序记录的地理编码地址。</summary>
    public IReadOnlyCollection<string> GeocodeRequests => _geocodeRequests.ToArray();

    /// <inheritdoc />
    public Task<GeocodeResult?> GeocodeAsync(
        string address,
        string? city = null,
        CancellationToken cancellationToken = default)
    {
        _geocodeRequests.Enqueue(address);

        // 用地址长度构造确定性坐标，便于断言坐标确实随地址变化而更新。
        return Task.FromResult<GeocodeResult?>(new GeocodeResult
        {
            Longitude = 100d + address.Length,
            Latitude = 20d + address.Length
        });
    }

    /// <inheritdoc />
    public Task<PoiSearchPage> SearchAroundAsync(
        string keywords,
        double longitude,
        double latitude,
        int radius,
        int pageNum = 1,
        int pageSize = IGaodeMapService.MaxPageSize,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PoiSearchPage());
    }

    /// <inheritdoc />
    public Task<WeatherResult?> GetWeatherAsync(string adcode, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<WeatherResult?>(null);
    }

    /// <inheritdoc />
    public double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
    {
        return 0d;
    }
}
