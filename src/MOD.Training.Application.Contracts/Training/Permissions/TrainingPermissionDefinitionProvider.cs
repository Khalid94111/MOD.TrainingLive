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
        tenantCourses.AddChild(TrainingPermissions.TenantCourses.EditConditions, L("Permission:TenantCourses.EditConditions"));

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
        casual.AddChild(TrainingPermissions.CasualCourses.Create, L("Permission:CasualCourses.Create"));
        casual.AddChild(TrainingPermissions.CasualCourses.ApproveUGM, L("Permission:CasualCourses.ApproveUGM"));
        casual.AddChild(TrainingPermissions.CasualCourses.Review, L("Permission:CasualCourses.Review"));
        casual.AddChild(TrainingPermissions.CasualCourses.ApproveTD, L("Permission:CasualCourses.ApproveTD"));
        casual.AddChild(TrainingPermissions.CasualCourses.ApproveTH, L("Permission:CasualCourses.ApproveTH"));

        var finance = group.AddPermission(TrainingPermissions.Finance.Default, L("Permission:Finance"));
        finance.AddChild(TrainingPermissions.Finance.ManageItems, L("Permission:Finance.ManageItems"));
        finance.AddChild(TrainingPermissions.Finance.ManageBudgets, L("Permission:Finance.ManageBudgets"));
        finance.AddChild(TrainingPermissions.Finance.ManageExchangeRates, L("Permission:Finance.ManageExchangeRates"));
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

        var defaultsGroup = group.AddPermission(
            TrainingPermissions.CourseTypeFinancialDefaults.Default,
            L("Permission:CourseTypeFinancialDefaults"));
        defaultsGroup.AddChild(
            TrainingPermissions.CourseTypeFinancialDefaults.Create,
            L("Permission:CourseTypeFinancialDefaults.Create"));
        defaultsGroup.AddChild(
            TrainingPermissions.CourseTypeFinancialDefaults.Delete,
            L("Permission:CourseTypeFinancialDefaults.Delete"));

        var exchangeRatesGroup = group.AddPermission(
            TrainingPermissions.ExchangeRates.Default,
            L("Permission:ExchangeRates"));
        exchangeRatesGroup.AddChild(
            TrainingPermissions.ExchangeRates.Create,
            L("Permission:ExchangeRates.Create"));
        exchangeRatesGroup.AddChild(
            TrainingPermissions.ExchangeRates.Delete,
            L("Permission:ExchangeRates.Delete"));

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

        // --- Phase 3: Course Sessions ---
        var sessionGroup = group.AddPermission(
            TrainingPermissions.CourseSession.Default,
            L("Permission:CourseSession"));
        sessionGroup.AddChild(
            TrainingPermissions.CourseSession.Create, L("Permission:CourseSession.Create"));
        sessionGroup.AddChild(
            TrainingPermissions.CourseSession.Update, L("Permission:CourseSession.Update"));
        sessionGroup.AddChild(
            TrainingPermissions.CourseSession.Delete, L("Permission:CourseSession.Delete"));

    }

    private static LocalizableString L(string name)
        => LocalizableString.Create<TrainingResource>(name);
}
