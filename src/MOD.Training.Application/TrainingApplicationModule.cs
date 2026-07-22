using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Volo.Abp.AutoMapper;
using Volo.Abp.Mapperly;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Gdpr;
using Volo.Abp.LanguageManagement;
using Volo.FileManagement;
using Volo.Abp.OpenIddict;
using Volo.Abp.TextTemplateManagement;
using Volo.Saas.Host;
using Travel;
using Travel.Allowances;
using Travel.Tenants;
using Travel.TravelRequests;
using MOD.Training.Tenants;
using MOD.Training.Training.Travel.Integration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MOD.Training;

[DependsOn(
    typeof(TrainingDomainModule),
    typeof(TrainingApplicationContractsModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpAccountPublicApplicationModule),
    typeof(AbpAccountAdminApplicationModule),
    typeof(SaasHostApplicationModule),
    typeof(TextTemplateManagementApplicationModule),
    typeof(AbpOpenIddictProApplicationModule),
    typeof(LanguageManagementApplicationModule),
    typeof(FileManagementApplicationModule),
    typeof(AbpGdprApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(TravelApplicationModule)
    )]
public class TrainingApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<TrainingApplicationModule>();
        context.Services.AddAutoMapper(typeof(TrainingApplicationModule).Assembly);
        context.Services.Replace(ServiceDescriptor.Transient<ITenantLookup, TrainingTenantLookup>());
        context.Services.Replace(ServiceDescriptor.Transient<ITrainingTravelGateway, TravelTrainingGateway>());
        context.Services.Replace(ServiceDescriptor.Transient<ITravelEmployeeLookup, TrainingTravelHrLookup>());
        context.Services.Replace(ServiceDescriptor.Transient<IRankLookup, TrainingTravelHrLookup>());
    }
}
