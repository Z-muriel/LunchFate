using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace LunchFate.Data;

/* This is used if database provider does't define
 * ILunchFateDbSchemaMigrator implementation.
 */
public class NullLunchFateDbSchemaMigrator : ILunchFateDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
