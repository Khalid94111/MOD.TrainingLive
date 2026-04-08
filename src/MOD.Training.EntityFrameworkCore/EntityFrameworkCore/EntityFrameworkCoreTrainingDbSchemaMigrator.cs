using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MOD.Training.Data;
using Volo.Abp.DependencyInjection;

namespace MOD.Training.EntityFrameworkCore;

public class EntityFrameworkCoreTrainingDbSchemaMigrator
    : ITrainingDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreTrainingDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the TrainingDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<TrainingDbContext>()
            .Database
            .MigrateAsync();
    }
}
