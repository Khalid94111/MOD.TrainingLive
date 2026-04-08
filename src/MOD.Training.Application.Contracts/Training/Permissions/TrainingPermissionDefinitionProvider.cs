using MOD.Training.Training.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Permissions;

public class TrainingPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var group = context.AddGroup(TrainingPermissions.GroupName, L("Permission:Training"));

        group.AddPermission(TrainingPermissions.Dashboard.Host, L("Permission:Dashboard"), MultiTenancySides.Host);
        group.AddPermission(TrainingPermissions.Dashboard.Tenant, L("Permission:Dashboard"), MultiTenancySides.Tenant);

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
    }

    private static LocalizableString L(string name)
        => LocalizableString.Create<TrainingResource>(name);
}
