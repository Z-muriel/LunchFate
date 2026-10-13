using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.Server.AspNetCore;
using LunchFate.Blazor.Client.Navigation;
using LunchFate.EntityFrameworkCore;
using LunchFate.Integration;
using LunchFate.Localization;
using LunchFate.MultiTenancy;
using Microsoft.OpenApi;
using Microsoft.Extensions.Options;
using LunchFate.Blazor.Client;
using LunchFate.Blazor.Components;
using Volo.Abp;
using Volo.Abp.AspNetCore.Components.Web;
using Volo.Abp.AspNetCore.Components.Web.Theming.Routing;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.AntiForgery;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Mapperly;
using Volo.Abp.Security.Claims;
using Volo.Abp.AspNetCore.Components.WebAssembly.Theming.Bundling;
using Volo.Abp.AspNetCore.Components.Server.BasicTheme.Bundling;
using Volo.Abp.AspNetCore.Components.Server.BasicTheme;
using Volo.Abp.AspNetCore.Components.WebAssembly.BasicTheme.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic.Bundling;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;
using Volo.Abp.UI.Navigation;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.Account.Web;
using Volo.Abp.Identity.Blazor.Server;
using Volo.Abp.TenantManagement.Blazor.Server;

namespace LunchFate.Blazor;

[DependsOn(
    typeof(LunchFateApplicationModule),
    typeof(LunchFateEntityFrameworkCoreModule),
    typeof(LunchFateIntegrationModule),
    typeof(LunchFateHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpIdentityBlazorServerModule),
    typeof(AbpTenantManagementBlazorServerModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAspNetCoreComponentsServerBasicThemeModule),
    typeof(AbpAspNetCoreComponentsWebAssemblyBasicThemeBundlingModule),
    typeof(AbpAspNetCoreMvcUiBasicThemeModule),
    typeof(AbpAspNetCoreSerilogModule)
   )]
public class LunchFateBlazorModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        context.Services.PreConfigure<AbpMvcDataAnnotationsLocalizationOptions>(options =>
        {
            options.AddAssemblyResource(
                typeof(LunchFateResource),
                typeof(LunchFateDomainModule).Assembly,
                typeof(LunchFateDomainSharedModule).Assembly,
                typeof(LunchFateApplicationModule).Assembly,
                typeof(LunchFateApplicationContractsModule).Assembly,
                typeof(LunchFateBlazorModule).Assembly
            );
        });

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("LunchFate");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });

        if (!hostingEnvironment.IsDevelopment())
        {
            PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
            {
                options.AddDevelopmentEncryptionAndSigningCertificate = false;
            });

            PreConfigure<OpenIddictServerBuilder>(serverBuilder =>
            {
                serverBuilder.AddProductionEncryptionAndSigningCertificate("openiddict.pfx", configuration["AuthServer:CertificatePassPhrase"]!);
                serverBuilder.SetIssuer(new Uri(configuration["AuthServer:Authority"]!));
            });
        }

        PreConfigure<AbpAspNetCoreComponentsWebOptions>(options =>
        {
            options.IsBlazorWebApp = true;
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        // Add services to the container.
        context.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        if (!configuration.GetValue<bool>("App:DisablePII"))
        {
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.LogCompleteSecurityArtifact = true;
        }

        if (!configuration.GetValue<bool>("AuthServer:RequireHttpsMetadata"))
        {
            Configure<OpenIddictServerAspNetCoreOptions>(options =>
            {
                options.DisableTransportSecurityRequirement = true;
            });
        }

        ConfigureAuthentication(context);
        ConfigureUrls(configuration);
        ConfigureBundles(hostingEnvironment);
        ConfigureVirtualFileSystem(hostingEnvironment);
        ConfigureSwaggerServices(context.Services);
        ConfigureAutoApiControllers();
        ConfigureBlazorise(context);
        ConfigureRouter(context);
        ConfigureMenu(context);

        // http 部署下 ABP 签发的 XSRF-TOKEN cookie（AbpAntiForgeryOptions.TokenCookie 硬编码 SameSite=None）
        // 因缺 Secure 属性会被 Chrome 丢弃：WASM 读不到 token → 发不出 RequestVerificationToken 头 → 所有 POST 400。
        // 已把 WASM 基址改为同源，无需跨站 cookie，改用 Lax（http/https 下均合法）。
        Configure<AbpAntiForgeryOptions>(options =>
        {
            options.TokenCookie.SameSite = SameSiteMode.Lax;
        });
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            options.RedirectAllowedUrls.AddRange(configuration["App:RedirectAllowedUrls"]?.Split(',') ?? Array.Empty<string>());
        });
    }

    private void ConfigureBundles(IHostEnvironment hostingEnvironment)
    {
        Configure<AbpBundlingOptions>(options =>
        {
            // Blazor Web App
            options.Parameters.InteractiveAuto = true;

            // MVC UI（登录页 / 错误页）：只需要令牌、基础与覆盖层
            options.StyleBundles.Configure(
                BasicThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles(LunchFateStyleBundleContributor.MvcStyleFiles);
                }
            );

            // MVC 布局只加载开发期登录提示；global-scripts.js 是 ES 模块，
            // 由 Blazor 组件按需 import，放进经典脚本包会因 export 语法报错。
            options.ScriptBundles.Configure(
                BasicThemeBundles.Scripts.Global,
                bundle =>
                {
                    if (hostingEnvironment.IsDevelopment())
                    {
                        bundle.AddFiles("/dev-login-helper.js");
                    }
                }
            );

            // Blazor UI（服务端渲染首屏）：完整样式层，与 WASM 侧保持同一顺序
            options.StyleBundles.Configure(
                BlazorBasicThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles(LunchFateStyleBundleContributor.BlazorStyleFiles);
                }
            );
        });

        Configure<AbpBundlingOptions>(options =>
        {
            var globalStyles = options.StyleBundles.Get(BlazorWebAssemblyStandardBundles.Styles.Global);
            globalStyles.AddContributors(typeof(LunchFateStyleBundleContributor));

            var globalScripts = options.ScriptBundles.Get(BlazorWebAssemblyStandardBundles.Scripts.Global);
            globalScripts.AddContributors(typeof(LunchFateScriptBundleContributor));
            
        });
    }

    private void ConfigureVirtualFileSystem(IWebHostEnvironment hostingEnvironment)
    {
        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<LunchFateDomainSharedModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}LunchFate.Domain.Shared"));
                options.FileSets.ReplaceEmbeddedByPhysical<LunchFateDomainModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}LunchFate.Domain"));
                options.FileSets.ReplaceEmbeddedByPhysical<LunchFateApplicationContractsModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}LunchFate.Application.Contracts"));
                options.FileSets.ReplaceEmbeddedByPhysical<LunchFateApplicationModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}LunchFate.Application"));
                options.FileSets.ReplaceEmbeddedByPhysical<LunchFateBlazorModule>(hostingEnvironment.ContentRootPath);
            });
        }
    }

    private void ConfigureSwaggerServices(IServiceCollection services)
    {
        services.AddAbpSwaggerGen(
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "LunchFate API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            }
        );
    }


    private void ConfigureBlazorise(ServiceConfigurationContext context)
    {
        context.Services
            .AddBlazorise(options =>
            {
                // TODO (IMPORTANT): To use Blazorise, you need a license key. Get your license key directly from Blazorise, follow  the instructions at https://abp.io/faq#how-to-get-blazorise-license-key
                //options.ProductToken = "Your Product Token";
            })
            .AddBootstrap5Providers()
            .AddFontAwesomeIcons();
    }

    private void ConfigureMenu(ServiceConfigurationContext context)
    {
        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new LunchFateMenuContributor(context.Services.GetConfiguration()));
        });
    }

    private void ConfigureRouter(ServiceConfigurationContext context)
    {
        Configure<AbpRouterOptions>(options =>
        {
            options.AppAssembly = typeof(LunchFateBlazorModule).Assembly;
            options.AdditionalAssemblies.Add(typeof(LunchFateBlazorClientModule).Assembly);
        });
    }

    private void ConfigureAutoApiControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(LunchFateApplicationModule).Assembly);
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var env = context.GetEnvironment();
        var app = context.GetApplicationBuilder();

        app.Use(async (ctx, next) =>
        {
            /* Converting to https to be able to include https URLs in `/.well-known/openid-configuration` endpoint.
             * This should only be done if the request is coming outside of the cluster.  */
            if (ctx.Request.Headers.ContainsKey("from-ingress"))
            {
                ctx.Request.Scheme = "https";
            }

            await next();
        });       

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
            app.UseHsts();
        }

        app.UseCorrelationId();
        app.UseRouting();
        var configuration = context.GetConfiguration();
        if (Convert.ToBoolean(configuration["AuthServer:IsOnK8s"]))
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.Value != null &&
                    context.Request.Path.Value.StartsWith("/appsettings", StringComparison.OrdinalIgnoreCase) &&
                    context.Request.Path.Value.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    // Set endpoint to null so the static files middleware will handle the request.
                    context.SetEndpoint(null);
                }
                await next(context);
            });

            app.UseStaticFilesForPatterns("appsettings*.json");
        }
        app.MapAbpStaticAssets();
        app.UseAbpSecurityHeaders();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }

        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseAntiforgery();
        app.UseAuthorization();
        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "LunchFate API");
        });
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();

        app.UseConfiguredEndpoints(builder =>
        {
            builder.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(builder.ServiceProvider.GetRequiredService<IOptions<AbpRouterOptions>>().Value.AdditionalAssemblies.ToArray());
        });
    }
}
