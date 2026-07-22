using Volo.Abp.Settings;

namespace Travel.Settings;

public class TravelManagementSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(TravelManagementSettings.OverseasFirst7DaysRate, "100"),
            new SettingDefinition(TravelManagementSettings.OverseasBeyond21DaysRate, "75"),
            new SettingDefinition(TravelManagementSettings.OverseasMaxDays, "60"),
            new SettingDefinition(TravelManagementSettings.ClothingFull5YearsAmount, "500"),
            new SettingDefinition(TravelManagementSettings.ClothingPartial3YearsAmount, "300"),
            new SettingDefinition(TravelManagementSettings.HotelMealsDeductionRate, "50")
        );
    }
}
