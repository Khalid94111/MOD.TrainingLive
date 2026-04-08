using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MOD.Training.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class TrainingDbContextFactory : IDesignTimeDbContextFactory<TrainingDbContext>
{
    public TrainingDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        
        TrainingEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<TrainingDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));
        
        return new TrainingDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../MOD.Training.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
