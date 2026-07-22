using Localization.Resources.AbpUi;
using Travel.Localization;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Travel;

[DependsOn(
    typeof(TravelApplicationContractsModule),
    typeof(AbpAspNetCoreMvcModule))]
public class TravelHttpApiModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(TravelHttpApiModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<TravelResource>()
                .AddBaseTypes(typeof(AbpUiResource));
        });
    }
}
