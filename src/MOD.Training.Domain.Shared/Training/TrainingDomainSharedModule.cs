using MOD.Training.Training.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.Validation.Localization;
using Volo.Abp.VirtualFileSystem;

namespace MOD.Training.Training;

/// <summary>
/// Add [DependsOn(typeof(TrainingDomainSharedModule))] to your existing DomainSharedModule.
/// This registers the Training localization resource.
/// </summary>
public class TrainingDomainSharedModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<TrainingDomainSharedModule>(
                baseNamespace: "YourApp.Training"
            );
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Add<TrainingResource>("en")
                .AddBaseTypes(typeof(AbpValidationResource))
                .AddVirtualJson("/Localization/Training");
        });
    }
}
