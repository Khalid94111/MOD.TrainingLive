using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Travel.Allowances;
using Travel.Ranks;
using Travel.Tenants;
using Travel.TravelRequests;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.FileSystem;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;

namespace Travel;

[DependsOn(
    typeof(TravelDomainModule),
    typeof(TravelApplicationContractsModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpBlobStoringModule),
    typeof(AbpBlobStoringFileSystemModule)
    )]
public class TravelApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<TravelApplicationModule>();
        });
        context.Services.AddAutoMapperObjectMapper<TravelApplicationModule>();
        context.Services.TryAddTransient<IRankLookup, StaticRankLookup>();
        context.Services.TryAddTransient<ITravelEmployeeLookup, NullTravelEmployeeLookup>();
        context.Services.TryAddTransient<ICountryLookup, NullCountryLookup>();
        context.Services.TryAddTransient<ITenantLookup, NullTenantLookup>();
        context.Services.TryAddTransient<IPayrollElementLookup, NullPayrollElementLookup>();
        context.Services.TryAddTransient<IPayrollOverseasTaskGenerator, NullPayrollOverseasTaskGenerator>();

        Configure<AbpBlobStoringOptions>(options =>
        {
            options.Containers.Configure<TravelDocumentContainer>(container =>
            {
                container.UseFileSystem(fileSystem =>
                    fileSystem.BasePath = "travel-files");
            });
        });
    }
}
