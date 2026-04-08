using Microsoft.Extensions.Localization;
using MOD.Training.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace MOD.Training;

[Dependency(ReplaceServices = true)]
public class TrainingBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<TrainingResource> _localizer;

    public TrainingBrandingProvider(IStringLocalizer<TrainingResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
