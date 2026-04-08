using Localization.Resources.AbpUi;
using MOD.Training.Localization;
using Volo.Abp.Account;
using Volo.Abp.SettingManagement;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.HttpApi;
using Volo.Abp.Localization;
using Volo.Abp.LanguageManagement;
using Volo.FileManagement;
using Volo.Saas.Host;
using Volo.Abp.TextTemplateManagement;
using Volo.Abp.Gdpr;
using Volo.Abp.OpenIddict;

namespace MOD.Training;

 [DependsOn(
    typeof(TrainingApplicationContractsModule),
    typeof(AbpPermissionManagementHttpApiModule),
    typeof(AbpSettingManagementHttpApiModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpAccountAdminHttpApiModule),
    typeof(TextTemplateManagementHttpApiModule),
    typeof(AbpOpenIddictProHttpApiModule),
    typeof(LanguageManagementHttpApiModule),
    typeof(FileManagementHttpApiModule),
    typeof(SaasHostHttpApiModule),
    typeof(AbpGdprHttpApiModule),
    typeof(AbpAccountPublicHttpApiModule),
    typeof(AbpFeatureManagementHttpApiModule)
    )]
public class TrainingHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureLocalization();
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<TrainingResource>()
                .AddBaseTypes(
                    typeof(AbpUiResource)
                );
        });
    }
}
