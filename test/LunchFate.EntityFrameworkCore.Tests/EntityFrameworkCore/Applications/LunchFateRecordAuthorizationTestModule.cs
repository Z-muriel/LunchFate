using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;

namespace LunchFate.EntityFrameworkCore.Applications;

/// <summary>只用于新增授权用例，在基础模块注册完成后恢复真实授权链，不改变其他测试模块。</summary>
[DependsOn(typeof(LunchFateEntityFrameworkCoreTestModule))]
public class LunchFateRecordAuthorizationTestModule : AbpModule
{
    /// <summary>替换全部四种始终允许服务，并将权限值来源限定为测试用户的显式声明。</summary>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.RemoveAll<IAuthorizationService>();
        context.Services.RemoveAll<IAbpAuthorizationService>();
        context.Services.RemoveAll<IMethodInvocationAuthorizationService>();
        context.Services.RemoveAll<IPermissionChecker>();
        context.Services.AddTransient<IAuthorizationService, AbpAuthorizationService>();
        context.Services.AddTransient<IAbpAuthorizationService, AbpAuthorizationService>();
        context.Services.AddTransient<IMethodInvocationAuthorizationService, MethodInvocationAuthorizationService>();
        context.Services.AddTransient<IPermissionChecker, PermissionChecker>();
        context.Services.AddTransient<TestUserPermissionValueProvider>();
        Configure<AbpPermissionOptions>(options =>
        {
            options.ValueProviders.Clear();
            options.ValueProviders.Add<TestUserPermissionValueProvider>();
        });
    }
}

/// <summary>仅为已认证且带用户标识的测试身份授予明确声明的权限，其余权限明确禁止。</summary>
public class TestUserPermissionValueProvider : PermissionValueProvider
{
    /// <summary>测试用户权限声明键，不参与生产授权。</summary>
    public const string ClaimType = "LunchFate.Test.Permission";
    /// <summary>真实授权链中的测试权限提供者标识。</summary>
    public override string Name => "TestUserClaims";

    /// <summary>保留真实权限提供者的依赖契约，测试不读取数据库权限授予。</summary>
    public TestUserPermissionValueProvider(IPermissionStore permissionStore) : base(permissionStore)
    {
    }

    /// <summary>检查单个权限，不使用始终允许授权器。</summary>
    public override Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context)
    {
        var granted = context.Principal?.Identity?.IsAuthenticated == true &&
            context.Principal.FindFirst(AbpClaimTypes.UserId) != null &&
            context.Principal.HasClaim(ClaimType, context.Permission.Name);
        return Task.FromResult(granted ? PermissionGrantResult.Granted : PermissionGrantResult.Prohibited);
    }

    /// <summary>逐项检查多个权限，防止批量授权路径意外全部放行。</summary>
    public override Task<MultiplePermissionGrantResult> CheckAsync(PermissionValuesCheckContext context)
    {
        var result = new MultiplePermissionGrantResult(context.Permissions.Select(x => x.Name).Distinct().ToArray());
        foreach (var permission in context.Permissions)
        {
            result.Result[permission.Name] = context.Principal?.Identity?.IsAuthenticated == true &&
                context.Principal.FindFirst(AbpClaimTypes.UserId) != null &&
                context.Principal.HasClaim(ClaimType, permission.Name)
                ? PermissionGrantResult.Granted : PermissionGrantResult.Prohibited;
        }
        return Task.FromResult(result);
    }
}
