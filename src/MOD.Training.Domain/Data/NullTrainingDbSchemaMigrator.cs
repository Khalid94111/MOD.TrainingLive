using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace MOD.Training.Data;

/* This is used if database provider does't define
 * ITrainingDbSchemaMigrator implementation.
 */
public class NullTrainingDbSchemaMigrator : ITrainingDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
