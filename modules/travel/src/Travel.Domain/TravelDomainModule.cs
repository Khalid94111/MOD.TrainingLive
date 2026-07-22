using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using Volo.Abp.Commercial.SuiteTemplates;

namespace Travel;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(VoloAbpCommercialSuiteTemplatesModule),
    typeof(TravelDomainSharedModule)
)]
public class TravelDomainModule : AbpModule
{

}
