using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Volo.Abp.Mapperly;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.TenantManagement;
using LunchFate.Integration;

namespace LunchFate;

[DependsOn(
    typeof(LunchFateDomainModule),
    typeof(LunchFateApplicationContractsModule),
    // ShopAppService 注入 IGaodeMapService，依赖必须在消费方声明，不能只靠宿主顺带加载。
    typeof(LunchFateIntegrationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class LunchFateApplicationModule : AbpModule
{

}
