using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace Travel.EntityFrameworkCore;

[DependsOn(
    typeof(TravelDomainModule),
    typeof(AbpEntityFrameworkCoreModule)
)]
public class TravelEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<TravelDbContext>(options =>
        {
            options.AddDefaultRepositories<ITravelDbContext>(includeAllEntities: true);
        });
    }
}
