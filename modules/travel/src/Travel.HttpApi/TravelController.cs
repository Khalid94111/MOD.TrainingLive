using Travel.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Travel;

public abstract class TravelController : AbpControllerBase
{
    protected TravelController()
    {
        LocalizationResource = typeof(TravelResource);
    }
}
