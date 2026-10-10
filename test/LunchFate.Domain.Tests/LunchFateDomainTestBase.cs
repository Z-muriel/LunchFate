using Volo.Abp.Modularity;

namespace LunchFate;

/* Inherit from this class for your domain layer tests. */
public abstract class LunchFateDomainTestBase<TStartupModule> : LunchFateTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
