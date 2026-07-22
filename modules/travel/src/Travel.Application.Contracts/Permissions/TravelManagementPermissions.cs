using Volo.Abp.Reflection;

namespace Travel.Permissions;

public static class TravelManagementPermissions
{
    public const string GroupName = "TravelManagement";

    public static class TravelRequests
    {
        public const string Default = GroupName + ".TravelRequests";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Confirm = Default + ".Confirm";
        public const string ViewByType = Default + ".ViewByType";
        public const string ManageAllowances = Default + ".ManageAllowances";
    }

    public static class AllowanceRules
    {
        public const string Default = GroupName + ".AllowanceRules";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class AllowanceRates
    {
        public const string Default = GroupName + ".AllowanceRates";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class ClothingAllowanceRules
    {
        public const string Default = GroupName + ".ClothingAllowanceRules";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class TravelTypes
    {
        public const string Default = GroupName + ".TravelTypes";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class AccommodationRules
    {
        public const string Default = GroupName + ".AccommodationRules";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class FundingSourceVoteRules
    {
        public const string Default = GroupName + ".FundingSourceVoteRules";
        public const string Edit = Default + ".Edit";
    }

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(TravelManagementPermissions));
    }
}
