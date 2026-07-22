using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Travel;

[DependsOn(
    typeof(AbpVirtualFileSystemModule)
    )]
public class TravelInstallerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<TravelInstallerModule>();
        });
    }
}
