using MOD.Training.Localization;
using Volo.Abp.Application.Services;

namespace MOD.Training;

/* Inherit your application services from this class.
 */
public abstract class TrainingAppService : ApplicationService
{
    protected TrainingAppService()
    {
        LocalizationResource = typeof(TrainingResource);
    }
}
