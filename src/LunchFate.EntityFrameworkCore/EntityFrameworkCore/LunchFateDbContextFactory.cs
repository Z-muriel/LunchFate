using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LunchFate.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class LunchFateDbContextFactory : IDesignTimeDbContextFactory<LunchFateDbContext>
{
    public LunchFateDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        
        LunchFateEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<LunchFateDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));
        
        return new LunchFateDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../LunchFate.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
