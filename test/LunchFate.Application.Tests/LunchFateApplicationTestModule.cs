using LunchFate.Integration;
using Volo.Abp.Modularity;

namespace LunchFate;

[DependsOn(
    typeof(LunchFateApplicationModule),
    typeof(LunchFateDomainTestModule),
    typeof(LunchFateIntegrationModule)
)]
public class LunchFateApplicationTestModule : AbpModule
{

}
