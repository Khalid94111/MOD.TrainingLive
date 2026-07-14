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
        public const string ViewSubscribedTenants = Default + ".ViewSubscribedTenants";
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
        public const string Default     = GroupName + ".CasualCourses";
        public const string Create      = Default + ".Create";
        public const string Edit        = Default + ".Edit";
        public const string Delete      = Default + ".Delete";
        public const string Submit      = Default + ".Submit";
        public const string Approve     = Default + ".Approve";     // UGM approve
        public const string Review      = Default + ".Review";      // Staff (start-review + assign-scenario + financials)
        public const string TDApprove   = Default + ".TDApprove";
        public const string HeadApprove = Default + ".HeadApprove";
        public const string Return      = Default + ".Return";
        public const string Reject      = Default + ".Reject";

        // Aliases kept for older pre-v4.5 scaffolding bindings
        public const string ApproveUGM  = Approve;
        public const string ApproveTD   = TDApprove;
        public const string ApproveTH   = HeadApprove;
    }

    public static class Finance
    {
        public const string Default = GroupName + ".Finance";
        public const string ManageItems = Default + ".ManageItems";
        public const string ManageBudgets = Default + ".ManageBudgets";
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

    // --- Phase 3: Course Sessions (extended in Phase 4C-α v4.10.0) ---
    public static class CourseSession
    {
        public const string Default        = GroupName + ".CourseSession";
        public const string Create         = Default + ".Create";
        public const string Update         = Default + ".Update";
        public const string Delete         = Default + ".Delete";

        // Phase 4C-α — execution lifecycle transitions (applied via CourseSessionAppService).
        public const string SelectQuote    = Default + ".SelectQuote";
        public const string MarkInProgress = Default + ".MarkInProgress";
        public const string MarkCompleted  = Default + ".MarkCompleted";
        public const string Cancel         = Default + ".Cancel";
    }
}

// Phase 4C-α (v4.10.0) — annual plan session creation + dashboard permissions.
// Separate top-level group so role grants stay narrowly scoped, mirroring the pattern of
// TrainingExecutionPermissions / TrainingPaymentsPermissions.
public static class AnnualPlanSessionPermissions
{
    public const string GroupName = "AnnualPlanSessions";

    public const string Default        = GroupName + ".Default";
    public const string Create         = GroupName + ".Create";
    public const string Substitute     = GroupName + ".Substitute";
    public const string Dashboard      = GroupName + ".Dashboard";
}

/// <summary>
/// Phase 4B-α — pre-execution preparation permissions (price quote selection
/// and travel instruction lifecycle). Separate group from <c>TrainingPermissions</c>
/// so role grants stay narrowly scoped.
/// </summary>
public static class TrainingExecutionPermissions
{
    public const string GroupName = "TrainingExecution";

    public static class PriceQuotes
    {
        public const string Default = GroupName + ".PriceQuotes";
        public const string Create  = Default + ".Create";
        public const string Edit    = Default + ".Edit";
        public const string Delete  = Default + ".Delete";
        public const string Select  = Default + ".Select";
    }

    public static class TravelInstructions
    {
        public const string Default = GroupName + ".TravelInstructions";
        public const string Edit    = Default + ".Edit";
        public const string Issue   = Default + ".Issue";
        public const string Cancel  = Default + ".Cancel";
    }
}

/// <summary>
/// Phase 4B-β — payments + auto-reallocation permissions. Separate top-level group from
/// <c>TrainingPermissions</c> and <c>TrainingExecutionPermissions</c> so role grants (Finance,
/// Staff, TD) stay narrowly scoped and the admin permissions UI keeps the modules visually distinct.
/// </summary>
public static class TrainingPaymentsPermissions
{
    public const string GroupName = "TrainingPayments";

    public static class TravelAllowance
    {
        public const string Default  = GroupName + ".TravelAllowance";
        public const string Create   = Default + ".Create";
        public const string Update   = Default + ".Update";
        public const string Delete   = Default + ".Delete";
        public const string Confirm  = Default + ".Confirm";
    }

    public static class CoursePayments
    {
        public const string Default          = GroupName + ".CoursePayments";
        public const string Create           = Default + ".Create";
        public const string Update           = Default + ".Update";
        public const string Delete           = Default + ".Delete";
        public const string Confirm          = Default + ".Confirm";
        public const string UploadInvoice    = Default + ".UploadInvoice";
        public const string DownloadInvoice  = Default + ".DownloadInvoice";
    }

    public static class Reallocations
    {
        public const string Default       = GroupName + ".Reallocations";
        public const string View          = Default + ".View";
        public const string MarkApproved  = Default + ".MarkApproved";
    }
}
