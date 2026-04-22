namespace MOD.Training.Training.Permissions;

public static class TrainingPermissions
{
    public const string GroupName = "Training";

    public static class CourseCatalog
    {
        public const string Default = GroupName + ".CourseCatalog";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class CourseFields
    {
        public const string Default = GroupName + ".CourseFields";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class CourseProposals
    {
        public const string Default = GroupName + ".CourseProposals";
        public const string Create = Default + ".Create";
        public const string Review = Default + ".Review";
    }

    public static class TenantCourses
    {
        public const string Default = GroupName + ".TenantCourses";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string EditConditions = Default + ".EditConditions";

    }

    public static class TrainingPlans
    {
        public const string Default = GroupName + ".TrainingPlans";
        public const string Create = Default + ".Create";
        public const string SubmitItems = Default + ".SubmitItems";
        public const string Review = Default + ".Review";
        public const string ApproveTD = Default + ".ApproveTD";
        public const string ApproveTH = Default + ".ApproveTH";
    }

    public static class Nominations
    {
        public const string Default = GroupName + ".Nominations";
        public const string Create = Default + ".Create";
        public const string ApproveUGM = Default + ".ApproveUGM";
        public const string ApproveTD = Default + ".ApproveTD";
    }

    public static class CasualCourses
    {
        public const string Default = GroupName + ".CasualCourses";
        public const string Create = Default + ".Create";
        public const string ApproveUGM = Default + ".ApproveUGM";
        public const string Review = Default + ".Review";
        public const string ApproveTD = Default + ".ApproveTD";
        public const string ApproveTH = Default + ".ApproveTH";
    }

    public static class Finance
    {
        public const string Default = GroupName + ".Finance";
        public const string ManageItems = Default + ".ManageItems";
        public const string ManageBudgets = Default + ".ManageBudgets";
        public const string ManageExchangeRates = Default + ".ManageExchangeRates";
        public const string ManageDefaults = Default + ".ManageDefaults";
        public const string ManagePayments = Default + ".ManagePayments";
    }

    public static class TrainingCenters
    {
        public const string Default = GroupName + ".TrainingCenters";
        public const string ManagePlans = Default + ".ManagePlans";
        public const string ManageSessions = Default + ".ManageSessions";
        public const string ApprovePlans = Default + ".ApprovePlans";
    }

    public static class Reports
    {
        public const string Default = GroupName + ".Reports";
        public const string FinancialReport = Default + ".FinancialReport";
        public const string PlanProgress = Default + ".PlanProgress";
        public const string Reallocation = Default + ".Reallocation";
    }

    public static class FinancialItems
    {
        public const string Default = GroupName + ".FinancialItems";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class CourseTypeFinancialDefaults
    {
        public const string Default = GroupName + ".CourseTypeFinancialDefaults";
        public const string Create = Default + ".Create";
        public const string Delete = Default + ".Delete";
    }

    public static class ExchangeRates
    {
        public const string Default = GroupName + ".ExchangeRates";
        public const string Create = Default + ".Create";
        public const string Delete = Default + ".Delete";
    }

    public static class TrainingBudgets
    {
        public const string Default = GroupName + ".TrainingBudgets";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class Centers
    {
        public const string Default = GroupName + ".Centers";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string ManageRoles = Default + ".ManageRoles";
        public const string ManageWindows = Default + ".ManageWindows";
    }

    public static class CenterPlans
    {
        public const string Default = GroupName + ".CenterPlans";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Submit = Default + ".Submit";
        public const string Approve = Default + ".Approve";
    }

    public static class CenterPlanItems
    {
        public const string Default = GroupName + ".CenterPlanItems";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string SetUnits = Default + ".SetUnits";
    }
    // --- Phase 3: Annual Plans ---
    public static class TrainingPlan
    {
        public const string Default = GroupName + ".TrainingPlan";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Submit = Default + ".Submit";       // UTM submits items
        public const string Review = Default + ".Review";       // Staff reviews
        public const string Approve = Default + ".Approve";     // TD approves
        public const string FinalApprove = Default + ".FinalApprove"; // TH final
        public const string ReturnToCreator = Default + ".ReturnToCreator"; // CHG-05
        public const string Resubmit = Default + ".Resubmit";               // CHG-05
    }

    public static class TrainingPlanItem
    {
        public const string Default = GroupName + ".TrainingPlanItem";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string AssignFinancials = Default + ".AssignFinancials"; // Staff only
        public const string Return = Default + ".Return"; // CHG-05 — Staff/TD/TH return individual item
    }

    // --- Phase 3: Nominations ---
    public static class Nomination
    {
        public const string Default = GroupName + ".Nomination";
        public const string Create = Default + ".Create";       // UTM nominates
        public const string ApproveUGM = Default + ".ApproveUGM";
        public const string ApproveTD = Default + ".ApproveTD";
        public const string Return = Default + ".Return";     // CHG-05
        public const string Replace = Default + ".Replace";   // CHG-05 — UTM replaces returned nominee
    }

    // --- Phase 3 v4.4: New groups ---
    public static class FinancialItemRankAmount
    {
        public const string Default = GroupName + ".FinancialItemRankAmount";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class PlanItemFinancialItemRank
    {
        public const string Default = GroupName + ".PlanItemFinancialItemRank";
        public const string UpdateRate = Default + ".UpdateRate";
    }

    public static class PlanNote
    {
        public const string Default = GroupName + ".PlanNote";
        public const string Create = Default + ".Create";
    }

    // --- Phase 3: Price Quotes ---
    public static class PriceQuote
    {
        public const string Default = GroupName + ".PriceQuote";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    // --- Phase 3: Training Providers ---
    public static class TrainingProvider
    {
        public const string Default = GroupName + ".TrainingProvider";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    // --- Phase 3: Course Sessions ---
    public static class CourseSession
    {
        public const string Default = GroupName + ".CourseSession";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }
}
