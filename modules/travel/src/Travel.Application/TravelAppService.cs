using Travel.Localization;
using Volo.Abp.Application.Services;

namespace Travel;

public abstract class TravelAppService : ApplicationService
{
    protected TravelAppService()
    {
        LocalizationResource = typeof(TravelResource);
        ObjectMapperContext = typeof(TravelApplicationModule);
    }
}
