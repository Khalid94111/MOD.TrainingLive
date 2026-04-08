using MOD.Training.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace MOD.Training.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class TrainingController : AbpControllerBase
{
    protected TrainingController()
    {
        LocalizationResource = typeof(TrainingResource);
    }
}
