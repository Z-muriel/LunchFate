using Volo.Abp.Modularity;

namespace LunchFate;

[DependsOn(
    typeof(LunchFateDomainModule),
    typeof(LunchFateTestBaseModule)
)]
public class LunchFateDomainTestModule : AbpModule
{

}
