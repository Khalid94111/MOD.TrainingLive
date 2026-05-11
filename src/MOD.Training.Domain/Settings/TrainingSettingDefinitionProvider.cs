using MOD.Training.Localization;
using System.IO;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace MOD.Training.Settings;

public class TrainingSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        // Phase 4B-β — File system base path for Course invoice blobs.
        //
        // The BlobStoring.FileSystem provider needs the path at module startup, so the
        // authoritative source for the value is appsettings.json (key: Gtms:Files:Path).
        // This setting registration mirrors the same key into ABP Setting Management so
        // admins can SEE the active value in the Settings UI; runtime changes require
        // an app restart since file-system roots are pinned at module bootstrap.
        context.Add(new SettingDefinition(
            name: "Gtms.Files.Path",
            defaultValue: Path.Combine(Path.GetTempPath(), "gtms-files"),
            displayName: L("Setting:Gtms.Files.Path"),
            description: L("Setting:Gtms.Files.Path.Description"),
            isVisibleToClients: false));
    }

    private static LocalizableString L(string name)
        => LocalizableString.Create<TrainingResource>(name);
}
