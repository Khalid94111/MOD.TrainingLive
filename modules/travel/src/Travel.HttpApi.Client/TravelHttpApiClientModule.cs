using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Http.Client;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Travel;

[DependsOn(
    typeof(TravelApplicationContractsModule),
    typeof(AbpHttpClientModule))]
public class TravelHttpApiClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClientProxies(
            typeof(TravelApplicationContractsModule).Assembly,
            TravelRemoteServiceConsts.RemoteServiceName
        );

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<TravelHttpApiClientModule>();
        });

    }
}
