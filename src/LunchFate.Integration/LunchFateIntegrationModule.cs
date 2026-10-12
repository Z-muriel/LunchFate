using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LunchFate.Integration.Gaode;
using Volo.Abp.Modularity;

namespace LunchFate.Integration;

/// <summary>
/// 高德地图集成模块，注册外部地图服务调用能力。
/// </summary>
public class LunchFateIntegrationModule : AbpModule
{
    /// <summary>
    /// 配置服务，注册高德地图服务与内存缓存。
    /// </summary>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        context.Services.Configure<GaodeMapOptions>(
            configuration.GetSection("Integration:Gaode"));

        context.Services.AddMemoryCache();

        context.Services.AddHttpClient<IGaodeMapService, GaodeMapService>(client =>
        {
            var options = configuration.GetSection("Integration:Gaode").Get<GaodeMapOptions>();
            if (options is null)
            {
                return;
            }

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        }).ConfigurePrimaryHttpMessageHandler(() =>
        {
            var options = configuration.GetSection("Integration:Gaode").Get<GaodeMapOptions>();
            var handler = new HttpClientHandler();

            // 默认严格校验证书；只有显式开启 Integration:Gaode:AllowInvalidServerCertificate 才放行自签名证书，
            // 避免生产环境被中间人截获高德 Key 与响应内容。
            if (options?.AllowInvalidServerCertificate == true)
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            return handler;
        });
    }
}
