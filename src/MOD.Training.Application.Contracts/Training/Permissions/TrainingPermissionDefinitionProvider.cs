using MOD.Training.Training.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace MOD.Training.Training.Permissions;

public class TrainingPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var group = context.AddGroup(TrainingPermissions.GroupName, L("Permission:Training"));

        var catalog = group.AddPermission(TrainingPermissions.CourseCatalog.Default, L("Permission:CourseCatalog"));
        catalog.AddChild(TrainingPermissions.CourseCatalog.Create, L("Permission:CourseCatalog.Create"));
        catalog.AddChild(TrainingPermissions.CourseCatalog.Update, L("Permission:CourseCatalog.Update"));
        catalog.AddChild(TrainingPermissions.CourseCatalog.Delete, L("Permission:CourseCatalog.Delete"));
        catalog.AddChild(TrainingPermissions.CourseCatalog.ViewSubscribedTenants, L("Permission:CourseCatalog.ViewSubscribedTenants"));

        var fields = group.AddPermission(TrainingPermissions.CourseFields.Default, L("Permission:CourseFields"));
        fields.AddChild(TrainingPermissions.CourseFields.Create, L("Permission:CourseFields.Create"));
        fields.AddChild(TrainingPermissions.CourseFields.Update, L("Permission:CourseFields.Update"));
        fields.AddChild(TrainingPermissions.CourseFields.Delete, L("Permission:CourseFields.Delete"));

        var proposals = group.AddPermission(TrainingPermissions.CourseProposals.Default, L("Permission:CourseProposals"));
        proposals.AddChild(TrainingPermissions.CourseProposals.Create, L("Permission:CourseProposals.Create"));
        proposals.AddChild(TrainingPermissions.CourseProposals.Review, L("Permission:CourseProposals.Review"));

        var tenantCourses = group.AddPermission(TrainingPermissions.TenantCourses.Default, L("Permission:TenantCourses"));
        tenantCourses.AddChild(TrainingPermissions.TenantCourses.Create, L("Permission:TenantCourses.Create"));
        tenantCourses.AddChild(TrainingPermissions.TenantCourses.Update, L("Permission:TenantCourses.Update"));
        tenantCourses.AddChild(TrainingPermissions.TenantCourses.Delete, L("Permission:TenantCourses.Delete"));

        var plans = group.AddPermission(TrainingPermissions.TrainingPlans.Default, L("Permission:TrainingPlans"));
        plans.AddChild(TrainingPermissions.TrainingPlans.Create, L("Permission:TrainingPlans.Create"));
        plans.AddChild(TrainingPermissions.TrainingPlans.SubmitItems, L("Permission:TrainingPlans.SubmitItems"));
        plans.AddChild(TrainingPermissions.TrainingPlans.Review, L("Permission:TrainingPlans.Review"));
        plans.AddChild(TrainingPermissions.TrainingPlans.ApproveTD, L("Permission:TrainingPlans.ApproveTD"));
        plans.AddChild(TrainingPermissions.TrainingPlans.ApproveTH, L("Permission:TrainingPlans.ApproveTH"));

        var nominations = group.AddPermission(TrainingPermissions.Nominations.Default, L("Permission:Nominations"));
        nominations.AddChild(TrainingPermissions.Nominations.Create, L("Permission:Nominations.Create"));
        nominations.AddChild(TrainingPermissions.Nominations.ApproveUGM, L("Permission:Nominations.ApproveUGM"));
        nominations.AddChild(TrainingPermissions.Nominations.ApproveTD, L("Permission:Nominations.ApproveTD"));

        var casual = group.AddPermission(TrainingPermissions.CasualCourses.Default, L("Permission:CasualCourses"));
        casual.AddChild(TrainingPermissions.CasualCourses.Create,      L("Permission:CasualCourses.Create"));
        casual.AddChild(TrainingPermissions.CasualCourses.Edit,        L("Permission:CasualCourses.Edit"));
        casual.AddChild(TrainingPermissions.CasualCourses.Delete,      L("Permission:CasualCourses.Delete"));
        casual.AddChild(TrainingPermissions.CasualCourses.Submit,      L("Permission:CasualCourses.Submit"));
        casual.AddChild(TrainingPermissions.CasualCourses.Approve,     L("Permission:CasualCourses.Approve"));
        casual.AddChild(TrainingPermissions.CasualCourses.Review,      L("Permission:CasualCourses.Review"));
        casual.AddChild(TrainingPermissions.CasualCourses.TDApprove,   L("Permission:CasualCourses.TDApprove"));
        casual.AddChild(TrainingPermissions.CasualCourses.HeadApprove, L("Permission:CasualCourses.HeadApprove"));
        casual.AddChild(TrainingPermissions.CasualCourses.Return,      L("Permission:CasualCourses.Return"));
        casual.AddChild(TrainingPermissions.CasualCourses.Reject,      L("Permission:CasualCourses.Reject"));

        var finance = group.AddPermission(TrainingPermissions.Finance.Default, L("Permission:Finance"));
        finance.AddChild(TrainingPermissions.Finance.ManageItems, L("Permission:Finance.ManageItems"));
        finance.AddChild(TrainingPermissions.Finance.ManageBudgets, L("Permission:Finance.ManageBudgets"));
        finance.AddChild(TrainingPermissions.Finance.ManageDefaults, L("Permission:Finance.ManageDefaults"));
        finance.AddChild(TrainingPermissions.Finance.ManagePayments, L("Permission:Finance.ManagePayments"));

        var centers = group.AddPermission(TrainingPermissions.TrainingCenters.Default, L("Permission:TrainingCenters"));
        centers.AddChild(TrainingPermissions.TrainingCenters.ManagePlans, L("Permission:TrainingCenters.ManagePlans"));
        centers.AddChild(TrainingPermissions.TrainingCenters.ManageSessions, L("Permission:TrainingCenters.ManageSessions"));
        centers.AddChild(TrainingPermissions.TrainingCenters.ApprovePlans, L("Permission:TrainingCenters.ApprovePlans"));

        var reports = group.AddPermission(TrainingPermissions.Reports.Default, L("Permission:Reports"));
        reports.AddChild(TrainingPermissions.Reports.FinancialReport, L("Permission:Reports.FinancialReport"));
        reports.AddChild(TrainingPermissions.Reports.PlanProgress, L("Permission:Reports.PlanProgress"));
        reports.AddChild(TrainingPermissions.Reports.Reallocation, L("Permission:Reports.Reallocation"));




        var financialItemsGroup = group.AddPermission(
    TrainingPermissions.FinancialItems.Default,
    L("Permission:FinancialItems"));
        financialItemsGroup.AddChild(
            TrainingPermissions.FinancialItems.Create,
            L("Permission:FinancialItems.Create"));
        financialItemsGroup.AddChild(
            TrainingPermissions.FinancialItems.Edit,
            L("Permission:FinancialItems.Edit"));
        financialItemsGroup.AddChild(
            TrainingPermissions.FinancialItems.Delete,
            L("Permission:FinancialItems.Delete"));

        var budgetsGroup = group.AddPermission(
            TrainingPermissions.TrainingBudgets.Default,
            L("Permission:TrainingBudgets"));
        budgetsGroup.AddChild(
            TrainingPermissions.TrainingBudgets.Create,
            L("Permission:TrainingBudgets.Create"));
        budgetsGroup.AddChild(
            TrainingPermissions.TrainingBudgets.Edit,
            L("Permission:TrainingBudgets.Edit"));
        budgetsGroup.AddChild(
            TrainingPermissions.TrainingBudgets.Delete,
            L("Permission:TrainingBudgets.Delete"));


        // --- Centers ---
        var centersPermission = group.AddPermission(
            TrainingPermissions.Centers.Default,
            L("Permission:Centers"));
        centersPermission.AddChild(
            TrainingPermissions.Centers.Create,
            L("Permission:Centers.Create"));
        centersPermission.AddChild(
            TrainingPermissions.Centers.Edit,
            L("Permission:Centers.Edit"));
        centersPermission.AddChild(
            TrainingPermissions.Centers.Delete,
            L("Permission:Centers.Delete"));
        centersPermission.AddChild(
            TrainingPermissions.Centers.ManageRoles,
            L("Permission:Centers.ManageRoles"));
        centersPermission.AddChild(
            TrainingPermissions.Centers.ManageWindows,
            L("Permission:Centers.ManageWindows"));

        // --- Center Plans ---
        var centerPlansPermission = group.AddPermission(
            TrainingPermissions.CenterPlans.Default,
            L("Permission:CenterPlans"));
        centerPlansPermission.AddChild(
            TrainingPermissions.CenterPlans.Create,
            L("Permission:CenterPlans.Create"));
        centerPlansPermission.AddChild(
            TrainingPermissions.CenterPlans.Edit,
            L("Permission:CenterPlans.Edit"));
        centerPlansPermission.AddChild(
            TrainingPermissions.CenterPlans.Delete,
            L("Permission:CenterPlans.Delete"));
        centerPlansPermission.AddChild(
            TrainingPermissions.CenterPlans.Submit,
            L("Permission:CenterPlans.Submit"));
        centerPlansPermission.AddChild(
            TrainingPermissions.CenterPlans.Approve,
            L("Permission:CenterPlans.Approve"));

        // --- Center Plan Items ---
        var centerPlanItemsPermission = group.AddPermission(
            TrainingPermissions.CenterPlanItems.Default,
            L("Permission:CenterPlanItems"));
        centerPlanItemsPermission.AddChild(
            TrainingPermissions.CenterPlanItems.Create,
            L("Permission:CenterPlanItems.Create"));
        centerPlanItemsPermission.AddChild(
            TrainingPermissions.CenterPlanItems.Edit,
            L("Permission:CenterPlanItems.Edit"));
        centerPlanItemsPermission.AddChild(
            TrainingPermissions.CenterPlanItems.Delete,
            L("Permission:CenterPlanItems.Delete"));
        centerPlanItemsPermission.AddChild(
            TrainingPermissions.CenterPlanItems.SetUnits,
            L("Permission:CenterPlanItems.SetUnits"));
        // --- Phase 3: Annual Plans ---
        var trainingPlanGroup = group.AddPermission(
            TrainingPermissions.TrainingPlan.Default,
            L("Permission:TrainingPlan"));
        trainingPlanGroup.AddChild(
            TrainingPermissions.TrainingPlan.Create, L("Permission:TrainingPlan.Create"));
        trainingPlanGroup.AddChild(
            TrainingPermissions.TrainingPlan.Update, L("Permission:TrainingPlan.Update"));
        trainingPlanGroup.AddChild(
            TrainingPermissions.TrainingPlan.Delete, L("Permission:TrainingPlan.Delete"));
        trainingPlanGroup.AddChild(
            TrainingPermissions.TrainingPlan.Submit, L("Permission:TrainingPlan.Submit"));
        trainingPlanGroup.AddChild(
            TrainingPermissions.TrainingPlan.Review, L("Permission:TrainingPlan.Review"));
        trainingPlanGroup.AddChild(
            TrainingPermissions.TrainingPlan.Approve, L("Permission:TrainingPlan.Approve"));
        trainingPlanGroup.AddChild(
            TrainingPermissions.TrainingPlan.FinalApprove, L("Permission:TrainingPlan.FinalApprove"));
        trainingPlanGroup.AddChild(
            TrainingPermissions.TrainingPlan.ReturnToCreator, L("Permission:TrainingPlan.ReturnToCreator"));
        trainingPlanGroup.AddChild(
            TrainingPermissions.TrainingPlan.Resubmit, L("Permission:TrainingPlan.Resubmit"));

        var planItemGroup = group.AddPermission(
            TrainingPermissions.TrainingPlanItem.Default,
            L("Permission:TrainingPlanItem"));
        planItemGroup.AddChild(
            TrainingPermissions.TrainingPlanItem.Create, L("Permission:TrainingPlanItem.Create"));
        planItemGroup.AddChild(
            TrainingPermissions.TrainingPlanItem.Update, L("Permission:TrainingPlanItem.Update"));
        planItemGroup.AddChild(
            TrainingPermissions.TrainingPlanItem.Delete, L("Permission:TrainingPlanItem.Delete"));
        planItemGroup.AddChild(
            TrainingPermissions.TrainingPlanItem.AssignFinancials, L("Permission:TrainingPlanItem.AssignFinancials"));
        planItemGroup.AddChild(
            TrainingPermissions.TrainingPlanItem.Return, L("Permission:TrainingPlanItem.Return"));

        // --- Phase 3: Nominations ---
        var nominationGroup = group.AddPermission(
            TrainingPermissions.Nomination.Default,
            L("Permission:Nomination"));
        nominationGroup.AddChild(
            TrainingPermissions.Nomination.Create, L("Permission:Nomination.Create"));
        nominationGroup.AddChild(
            TrainingPermissions.Nomination.ApproveUGM, L("Permission:Nomination.ApproveUGM"));
        nominationGroup.AddChild(
            TrainingPermissions.Nomination.ApproveTD, L("Permission:Nomination.ApproveTD"));
        nominationGroup.AddChild(
            TrainingPermissions.Nomination.Return, L("Permission:Nomination.Return"));
        nominationGroup.AddChild(
            TrainingPermissions.Nomination.Replace, L("Permission:Nomination.Replace"));

        // --- Phase 3: Price Quotes ---
        var priceQuoteGroup = group.AddPermission(
            TrainingPermissions.PriceQuote.Default,
            L("Permission:PriceQuote"));
        priceQuoteGroup.AddChild(
            TrainingPermissions.PriceQuote.Create, L("Permission:PriceQuote.Create"));
        priceQuoteGroup.AddChild(
            TrainingPermissions.PriceQuote.Update, L("Permission:PriceQuote.Update"));
        priceQuoteGroup.AddChild(
            TrainingPermissions.PriceQuote.Delete, L("Permission:PriceQuote.Delete"));

        // --- Phase 3: Training Providers ---
        var providerGroup = group.AddPermission(
            TrainingPermissions.TrainingProvider.Default,
            L("Permission:TrainingProvider"));
        providerGroup.AddChild(
            TrainingPermissions.TrainingProvider.Create, L("Permission:TrainingProvider.Create"));
        providerGroup.AddChild(
            TrainingPermissions.TrainingProvider.Update, L("Permission:TrainingProvider.Update"));
        providerGroup.AddChild(
            TrainingPermissions.TrainingProvider.Delete, L("Permission:TrainingProvider.Delete"));

        // --- Phase 3: Course Sessions (Phase 4C-α v4.10.0 extension) ---
        var sessionGroup = group.AddPermission(
            TrainingPermissions.CourseSession.Default,
            L("Permission:CourseSession"));
        sessionGroup.AddChild(
            TrainingPermissions.CourseSession.Create, L("Permission:CourseSession.Create"));
        sessionGroup.AddChild(
            TrainingPermissions.CourseSession.Update, L("Permission:CourseSession.Update"));
        sessionGroup.AddChild(
            TrainingPermissions.CourseSession.Delete, L("Permission:CourseSession.Delete"));
        sessionGroup.AddChild(
            TrainingPermissions.CourseSession.SelectQuote, L("Permission:CourseSession.SelectQuote"));
        sessionGroup.AddChild(
            TrainingPermissions.CourseSession.MarkInProgress, L("Permission:CourseSession.MarkInProgress"));
        sessionGroup.AddChild(
            TrainingPermissions.CourseSession.MarkCompleted, L("Permission:CourseSession.MarkCompleted"));
        sessionGroup.AddChild(
            TrainingPermissions.CourseSession.Cancel, L("Permission:CourseSession.Cancel"));

        // --- Phase 3 v4.4: New groups ---
        var financialItemRankAmountGroup = group.AddPermission(
            TrainingPermissions.FinancialItemRankAmount.Default,
            L("Permission:FinancialItemRankAmount"));
        financialItemRankAmountGroup.AddChild(
            TrainingPermissions.FinancialItemRankAmount.Create, L("Permission:FinancialItemRankAmount.Create"));
        financialItemRankAmountGroup.AddChild(
            TrainingPermissions.FinancialItemRankAmount.Update, L("Permission:FinancialItemRankAmount.Update"));
        financialItemRankAmountGroup.AddChild(
            TrainingPermissions.FinancialItemRankAmount.Delete, L("Permission:FinancialItemRankAmount.Delete"));

        var planItemFinancialItemRankGroup = group.AddPermission(
            TrainingPermissions.PlanItemFinancialItemRank.Default,
            L("Permission:PlanItemFinancialItemRank"));
        planItemFinancialItemRankGroup.AddChild(
            TrainingPermissions.PlanItemFinancialItemRank.UpdateRate, L("Permission:PlanItemFinancialItemRank.UpdateRate"));

        var planNoteGroup = group.AddPermission(
            TrainingPermissions.PlanNote.Default,
            L("Permission:PlanNote"));
        planNoteGroup.AddChild(
            TrainingPermissions.PlanNote.Create, L("Permission:PlanNote.Create"));

        // ── Phase 4B-α — pre-execution permissions (separate group) ─────────
        var executionGroup = context.AddGroup(
            TrainingExecutionPermissions.GroupName,
            L("Permission:TrainingExecution"));

        var priceQuotesPermission = executionGroup.AddPermission(
            TrainingExecutionPermissions.PriceQuotes.Default,
            L("Permission:TrainingExecution.PriceQuotes"));
        priceQuotesPermission.AddChild(
            TrainingExecutionPermissions.PriceQuotes.Create,
            L("Permission:TrainingExecution.PriceQuotes.Create"));
        priceQuotesPermission.AddChild(
            TrainingExecutionPermissions.PriceQuotes.Edit,
            L("Permission:TrainingExecution.PriceQuotes.Edit"));
        priceQuotesPermission.AddChild(
            TrainingExecutionPermissions.PriceQuotes.Delete,
            L("Permission:TrainingExecution.PriceQuotes.Delete"));
        priceQuotesPermission.AddChild(
            TrainingExecutionPermissions.PriceQuotes.Select,
            L("Permission:TrainingExecution.PriceQuotes.Select"));

        var travelInstructionsPermission = executionGroup.AddPermission(
            TrainingExecutionPermissions.TravelInstructions.Default,
            L("Permission:TrainingExecution.TravelInstructions"));
        travelInstructionsPermission.AddChild(
            TrainingExecutionPermissions.TravelInstructions.Edit,
            L("Permission:TrainingExecution.TravelInstructions.Edit"));
        travelInstructionsPermission.AddChild(
            TrainingExecutionPermissions.TravelInstructions.Issue,
            L("Permission:TrainingExecution.TravelInstructions.Issue"));
        travelInstructionsPermission.AddChild(
            TrainingExecutionPermissions.TravelInstructions.Cancel,
            L("Permission:TrainingExecution.TravelInstructions.Cancel"));

        // ── Phase 4B-β — payments + auto-reallocation (separate group) ──────
        var paymentsGroup = context.AddGroup(
            TrainingPaymentsPermissions.GroupName,
            L("Permission:TrainingPayments"));

        var travelAllowance = paymentsGroup.AddPermission(
            TrainingPaymentsPermissions.TravelAllowance.Default,
            L("Permission:TrainingPayments.TravelAllowance"));
        travelAllowance.AddChild(
            TrainingPaymentsPermissions.TravelAllowance.Create,
            L("Permission:TrainingPayments.TravelAllowance.Create"));
        travelAllowance.AddChild(
            TrainingPaymentsPermissions.TravelAllowance.Update,
            L("Permission:TrainingPayments.TravelAllowance.Update"));
        travelAllowance.AddChild(
            TrainingPaymentsPermissions.TravelAllowance.Delete,
            L("Permission:TrainingPayments.TravelAllowance.Delete"));
        travelAllowance.AddChild(
            TrainingPaymentsPermissions.TravelAllowance.Confirm,
            L("Permission:TrainingPayments.TravelAllowance.Confirm"));

        var coursePayments = paymentsGroup.AddPermission(
            TrainingPaymentsPermissions.CoursePayments.Default,
            L("Permission:TrainingPayments.CoursePayments"));
        coursePayments.AddChild(
            TrainingPaymentsPermissions.CoursePayments.Create,
            L("Permission:TrainingPayments.CoursePayments.Create"));
        coursePayments.AddChild(
            TrainingPaymentsPermissions.CoursePayments.Update,
            L("Permission:TrainingPayments.CoursePayments.Update"));
        coursePayments.AddChild(
            TrainingPaymentsPermissions.CoursePayments.Delete,
            L("Permission:TrainingPayments.CoursePayments.Delete"));
        coursePayments.AddChild(
            TrainingPaymentsPermissions.CoursePayments.Confirm,
            L("Permission:TrainingPayments.CoursePayments.Confirm"));
        coursePayments.AddChild(
            TrainingPaymentsPermissions.CoursePayments.UploadInvoice,
            L("Permission:TrainingPayments.CoursePayments.UploadInvoice"));
        coursePayments.AddChild(
            TrainingPaymentsPermissions.CoursePayments.DownloadInvoice,
            L("Permission:TrainingPayments.CoursePayments.DownloadInvoice"));

        var reallocations = paymentsGroup.AddPermission(
            TrainingPaymentsPermissions.Reallocations.Default,
            L("Permission:TrainingPayments.Reallocations"));
        reallocations.AddChild(
            TrainingPaymentsPermissions.Reallocations.View,
            L("Permission:TrainingPayments.Reallocations.View"));
        reallocations.AddChild(
            TrainingPaymentsPermissions.Reallocations.MarkApproved,
            L("Permission:TrainingPayments.Reallocations.MarkApproved"));

        // ── Phase 4C-α — annual plan session creation + dashboard (separate group) ──
        var annualPlanSessions = context.AddGroup(
            AnnualPlanSessionPermissions.GroupName,
            L("Permission:AnnualPlanSessions"));
        annualPlanSessions.AddPermission(
            AnnualPlanSessionPermissions.Default,
            L("Permission:AnnualPlanSessions.Default"));
        annualPlanSessions.AddPermission(
            AnnualPlanSessionPermissions.Create,
            L("Permission:AnnualPlanSessions.Create"));
        annualPlanSessions.AddPermission(
            AnnualPlanSessionPermissions.Substitute,
            L("Permission:AnnualPlanSessions.Substitute"));
        annualPlanSessions.AddPermission(
            AnnualPlanSessionPermissions.Dashboard,
            L("Permission:AnnualPlanSessions.Dashboard"));
    }

    private static LocalizableString L(string name)
        => LocalizableString.Create<TrainingResource>(name);
}
