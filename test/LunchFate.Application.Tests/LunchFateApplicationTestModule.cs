using Volo.Abp.Modularity;

namespace LunchFate;

[DependsOn(
    typeof(LunchFateApplicationModule),
    typeof(LunchFateDomainTestModule)
)]
public class LunchFateApplicationTestModule : AbpModule
{

}
