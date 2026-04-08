using Volo.Abp.Settings;

namespace MOD.Training.Settings;

public class TrainingSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(TrainingSettings.MySetting1));
    }
}
