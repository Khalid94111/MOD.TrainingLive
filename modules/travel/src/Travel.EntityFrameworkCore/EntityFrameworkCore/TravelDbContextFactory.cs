using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Travel.EntityFrameworkCore;

public class TravelDbContextFactory : IDesignTimeDbContextFactory<TravelDbContext>
{
    public TravelDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetConnectionString(TravelDbProperties.ConnectionStringName)
            ?? configuration.GetConnectionString("Default");

        var builder = new DbContextOptionsBuilder<TravelDbContext>()
            .UseSqlServer(connectionString);

        return new TravelDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var basePath = Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "../HRMST.DbMigrator"))
            ? Path.Combine(Directory.GetCurrentDirectory(), "../HRMST.DbMigrator")
            : Directory.GetCurrentDirectory();

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }
}
