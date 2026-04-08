using MOD.Training.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace MOD.Training.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(TrainingEntityFrameworkCoreModule),
    typeof(TrainingApplicationContractsModule)
)]
public class TrainingDbMigratorModule : AbpModule
{
}
