using Travel.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Travel.Permissions;

public class TravelManagementPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var group = context.AddGroup(TravelManagementPermissions.GroupName, L("Permission:TravelManagement"));

        var requests = group.AddPermission(
            TravelManagementPermissions.TravelRequests.Default,
            L("Permission:TravelRequests"));

        requests.AddChild(TravelManagementPermissions.TravelRequests.Create, L("Permission:TravelRequests.Create"));
        requests.AddChild(TravelManagementPermissions.TravelRequests.Edit, L("Permission:TravelRequests.Edit"));
        requests.AddChild(TravelManagementPermissions.TravelRequests.Delete, L("Permission:TravelRequests.Delete"));
        requests.AddChild(TravelManagementPermissions.TravelRequests.Confirm, L("Permission:TravelRequests.Confirm"));
        requests.AddChild(TravelManagementPermissions.TravelRequests.ViewByType, L("Permission:TravelRequests.ViewByType"));
        requests.AddChild(TravelManagementPermissions.TravelRequests.ManageAllowances, L("Permission:TravelRequests.ManageAllowances"));

        var rules = group.AddPermission(
            TravelManagementPermissions.AllowanceRules.Default,
            L("Permission:AllowanceRules"));
        rules.AddChild(TravelManagementPermissions.AllowanceRules.Create, L("Permission:AllowanceRules.Create"));
        rules.AddChild(TravelManagementPermissions.AllowanceRules.Edit, L("Permission:AllowanceRules.Edit"));
        rules.AddChild(TravelManagementPermissions.AllowanceRules.Delete, L("Permission:AllowanceRules.Delete"));

        var rates = group.AddPermission(
            TravelManagementPermissions.AllowanceRates.Default,
            L("Permission:AllowanceRates"));
        rates.AddChild(TravelManagementPermissions.AllowanceRates.Create, L("Permission:AllowanceRates.Create"));
        rates.AddChild(TravelManagementPermissions.AllowanceRates.Edit, L("Permission:AllowanceRates.Edit"));
        rates.AddChild(TravelManagementPermissions.AllowanceRates.Delete, L("Permission:AllowanceRates.Delete"));

        var clothingRules = group.AddPermission(
            TravelManagementPermissions.ClothingAllowanceRules.Default,
            L("Permission:ClothingAllowanceRules"));
        clothingRules.AddChild(TravelManagementPermissions.ClothingAllowanceRules.Create, L("Permission:ClothingAllowanceRules.Create"));
        clothingRules.AddChild(TravelManagementPermissions.ClothingAllowanceRules.Edit, L("Permission:ClothingAllowanceRules.Edit"));
        clothingRules.AddChild(TravelManagementPermissions.ClothingAllowanceRules.Delete, L("Permission:ClothingAllowanceRules.Delete"));

        var travelTypes = group.AddPermission(
            TravelManagementPermissions.TravelTypes.Default,
            L("Permission:TravelTypes"));
        travelTypes.AddChild(TravelManagementPermissions.TravelTypes.Create, L("Permission:TravelTypes.Create"));
        travelTypes.AddChild(TravelManagementPermissions.TravelTypes.Edit, L("Permission:TravelTypes.Edit"));
        travelTypes.AddChild(TravelManagementPermissions.TravelTypes.Delete, L("Permission:TravelTypes.Delete"));

        var accommodationRules = group.AddPermission(
            TravelManagementPermissions.AccommodationRules.Default,
            L("Permission:AccommodationRules"));
        accommodationRules.AddChild(TravelManagementPermissions.AccommodationRules.Create, L("Permission:AccommodationRules.Create"));
        accommodationRules.AddChild(TravelManagementPermissions.AccommodationRules.Edit, L("Permission:AccommodationRules.Edit"));
        accommodationRules.AddChild(TravelManagementPermissions.AccommodationRules.Delete, L("Permission:AccommodationRules.Delete"));

        var fundingSourceVoteRules = group.AddPermission(
            TravelManagementPermissions.FundingSourceVoteRules.Default,
            L("Permission:FundingSourceVoteRules"));
        fundingSourceVoteRules.AddChild(TravelManagementPermissions.FundingSourceVoteRules.Edit, L("Permission:FundingSourceVoteRules.Edit"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<TravelResource>(name);
    }
}
