using System.Threading.Tasks;

namespace MOD.Training.Data;

public interface ITrainingDbSchemaMigrator
{
    Task MigrateAsync();
}
