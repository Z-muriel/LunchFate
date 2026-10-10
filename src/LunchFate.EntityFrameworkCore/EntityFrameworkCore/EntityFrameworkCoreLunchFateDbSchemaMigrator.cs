using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LunchFate.Data;
using Volo.Abp.DependencyInjection;

namespace LunchFate.EntityFrameworkCore;

public class EntityFrameworkCoreLunchFateDbSchemaMigrator
    : ILunchFateDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreLunchFateDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the LunchFateDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<LunchFateDbContext>()
            .Database
            .MigrateAsync();
    }
}
