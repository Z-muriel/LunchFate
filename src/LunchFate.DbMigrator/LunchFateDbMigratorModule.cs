using LunchFate.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace LunchFate.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(LunchFateEntityFrameworkCoreModule),
    typeof(LunchFateApplicationContractsModule)
)]
public class LunchFateDbMigratorModule : AbpModule
{
}
