using Volo.Abp.Modularity;

namespace LunchFate;

public abstract class LunchFateApplicationTestBase<TStartupModule> : LunchFateTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
