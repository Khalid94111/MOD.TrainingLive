using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Catalog;
using MOD.Training.Training.Centers;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Hr;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Nominations;
using MOD.Training.Training.Plans;
using MOD.Training.Training.TenantCourses;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Volo.Saas.Tenants;

namespace MOD.Training.Training.DataSeeder;

public interface ITenantDataSeeder
{
    Task SeedTenantAsync(DataSeedContext context, Tenant? tenant , string prefix, TenantScenario scenario);
}

public class TenantDataSeeder(
    IGuidGenerator guidGenerator,
    ICurrentTenant currentTenant,
    ITenantRepository tenantRepository,
    ITenantManager tenantManager,
    IdentityUserManager userManager,
    IdentityRoleManager roleManager,
    IRepository<OrganizationUnit, Guid> orgUnitRepository,
    IRepository<Employee, Guid> employeeRepo,
    IRepository<CourseCatalog, Guid> catalogRepo,
    IRepository<CatalogEnrollmentCondition, Guid> catalogConditionRepo,
    IRepository<TenantCourse, Guid> tenantCourseRepo,
    IRepository<TenantCourseCondition, Guid> tenantConditionRepo,
    IRepository<FinancialItem, Guid> financialItemRepo,
    IRepository<CourseTypeFinancialItemDefault, Guid> financialDefaultRepo,
    IRepository<ExchangeRate, Guid> exchangeRateRepo,
    IRepository<TrainingBudget, Guid> budgetRepo,
    IRepository<TrainingCenter, Guid> centerRepo,
    IRepository<CenterRoleAssignment, Guid> centerRoleRepo,
    IRepository<CenterPlanWindow, Guid> centerWindowRepo,
    IRepository<TrainingCenterPlan, Guid> centerPlanRepo,
    IRepository<TrainingCenterPlanItem, Guid> centerPlanItemRepo,
    IRepository<TrainingCenterPlanItemUnit, Guid> centerPlanItemUnitRepo,
    IRepository<TrainingPlan, Guid> planRepo,
    IRepository<TrainingPlanItem, Guid> planItemRepo,
    IRepository<PlanItemCondition, Guid> planItemConditionRepo,
    IRepository<PlanItemFinancialItem, Guid> planItemFinancialRepo,
    IRepository<Course, Guid> courseRepo,
    IRepository<CourseSession, Guid> sessionRepo,
    IRepository<Nomination, Guid> nominationRepo,
    IRepository<NominationApproval, Guid> approvalRepo,
    IRepository<PriceQuote, Guid> quoteRepo,
    IRepository<CourseProposal, Guid> proposalRepo,
    IRepository<FinancialItemRankAmount, Guid> financialItemRankAmountRepo,
    IRepository<CasualCourse, Guid> casualCourseRepo,
    IRepository<CasualCourseFinancial, Guid> casualCourseFinancialRepo,
    IRepository<CasualCourseNomination, Guid> casualCourseNominationRepo,
    IRepository<PlanNote, Guid> planNoteRepo,
    FinancialItemDefaultResolver financialItemDefaultResolver,
    IUnitOfWorkManager uowManager,
    ILogger<TenantDataSeeder> logger)
    : ITransientDependency, ITenantDataSeeder
{
    private static readonly PropertyInfo OuCodeProp =
        typeof(OrganizationUnit).GetProperty(nameof(OrganizationUnit.Code))!;

    private Dictionary<string, Guid> _orgUnits = new();
    private Dictionary<string, Guid> _users = new();
    private Dictionary<string, Guid> _employees = new(); // key → employeeId (not userId)
    private Dictionary<Guid, Guid> _tenantCourses = new(); // catalogId → tenantCourseId
    private Dictionary<string, Guid> _fiIds = new(); // "Tuition" → Guid (per tenant)

    public async Task SeedTenantAsync(DataSeedContext context, Tenant? tenant , string prefix, TenantScenario scenario)
    {
        // Create tenant
        //var tenantName = prefix switch { "GF" => "القوات البرية", "AF" => "القوات الجوية", "NF" => "القوات البحرية", _ => prefix };
        //var tenant = await tenantManager.CreateAsync($"{tenantName} - {prefix} Forces");
        //await tenantRepository.InsertAsync(tenant, autoSave: true);

        using (currentTenant.Change(tenant.Id))
        {
            logger.LogInformation($"    🏛️ Tenant {prefix} ({scenario})...");

            // Each step in its own UoW to flush FK dependencies
            //await InUow(() => SeedRolesAsync(tenant.Id));
            //await InUow(() => SeedOrgUnitsAsync(prefix, tenant.Id));
            //await InUow(() => SeedUsersAsync(prefix, tenant.Id));
            //await InUow(() => SeedEmployeesAsync(tenant.Id));
            //await InUow(() => SeedTenantCoursesAsync(tenant.Id));

          await InUow(() => SeedFinancialItemsAsync(tenant.Id));
            //await InUow(() => SeedFinancialDefaultsAsync());
            //await InUow(() => SeedExchangeRatesAsync());
            //await InUow(() => SeedBudgetsAsync());
            //await InUow(() => SeedCentersAsync(prefix));
            //await InUow(() => SeedCenterPlanWindowsAsync());
            //await InUow(() => SeedCenterPlansAsync(tenant.Id));
            //await InUow(() => SeedAnnualPlanAsync(prefix, scenario,tenant.Id));
            //await InUow(() => SeedProposalsAsync(tenant.Id));
            await InUow(() => SeedCasualCoursesAsync(tenant.Id));
        }
    }

    private async Task InUow(Func<Task> action)
    {
        using var uow = uowManager.Begin(requiresNew: true);
        await action();
        await uow.CompleteAsync();
    }

    // ── ROLES ──
    private async Task SeedRolesAsync(Guid tenantId)
    {
        var roleNames = new[] { "TenantHead", "TrainingDirector", "TrainingDirectorateStaff", "SchedulingOfficer", "FinanceOfficer",
            "UnitGeneralManager", "UnitTrainingManager", "Employee", "TrainingCenterOfficer", "TrainingCenterManager",
            "InternalTrainingOfficer", "HROfficer" };

        foreach (var name in roleNames)
            if (await roleManager.FindByNameAsync(name) == null)
                await roleManager.CreateAsync(new IdentityRole(guidGenerator.Create(), name,tenantId));
    }

    // ── ORG UNITS ──
    private async Task SeedOrgUnitsAsync(string prefix, Guid tenantId)
    {
        _orgUnits = new();
        var tenantName = prefix switch { "GF" => "القوات البرية", "AF" => "القوات الجوية", _ => "القوات البحرية" };

        _orgUnits["HQ"]          = await InsertOu($"قيادة {tenantName}", null, "00001",tenantId);
        _orgUnits["TrainingDir"] = await InsertOu("مديرية التدريب", _orgUnits["HQ"], "00001.00001",tenantId);
        _orgUnits["HR"]          = await InsertOu("إدارة شؤون الموظفين", _orgUnits["HQ"], "00001.00002",tenantId);
        _orgUnits["Brigade1"]    = await InsertOu("اللواء الأول", _orgUnits["HQ"], "00001.00003", tenantId);
        _orgUnits["Brigade2"]    = await InsertOu("اللواء الثاني", _orgUnits["HQ"], "00001.00004", tenantId);
        _orgUnits["Engineering"] = await InsertOu("سلاح الهندسة", _orgUnits["HQ"], "00001.00005", tenantId);
        _orgUnits["Bn1_1"]      = await InsertOu("الكتيبة الأولى - اللواء الأول", _orgUnits["Brigade1"], "00001.00003.00001",tenantId);
        _orgUnits["Bn1_2"]      = await InsertOu("الكتيبة الثانية - اللواء الأول", _orgUnits["Brigade1"], "00001.00003.00002", tenantId);
        _orgUnits["Bn2_1"]      = await InsertOu("الكتيبة الأولى - اللواء الثاني", _orgUnits["Brigade2"], "00001.00004.00001", tenantId);
        _orgUnits["Center1"]    = await InsertOu("مركز التدريب الأساسي", _orgUnits["TrainingDir"], "00001.00001.00001", tenantId);
        _orgUnits["Center2"]    = await InsertOu("مركز التدريب المتقدم", _orgUnits["TrainingDir"], "00001.00001.00002", tenantId);
    }

    private async Task<Guid> InsertOu(string name, Guid? parentId, string code, Guid tenantId)
    {
        var id = guidGenerator.Create();
        var ou = new OrganizationUnit(id, name, parentId, tenantId);
        OuCodeProp.SetValue(ou, code);
        await orgUnitRepository.InsertAsync(ou, autoSave: true);
        return id;
    }

    // ── USERS ──
    private async Task SeedUsersAsync(string prefix,Guid tenantId)
    {
        _users = new();
        async Task<Guid> U(string key, string email, string name, string role)
        {
            var user = new IdentityUser(guidGenerator.Create(), $"{email}@{prefix}.mil.om", $"{email}@{prefix}.mil.om", tenantId);
            user.Name = name;
            await userManager.CreateAsync(user, "1q2w3E*");
            await userManager.AddToRoleAsync(user, role);
            _users[key] = user.Id;
            return user.Id;
        }

        await U("TH",        "th",       "سعيد بن حمد الراشدي",          "TenantHead");
        await U("TD",        "td",       "خالد بن سالم الهنائي",          "TrainingDirector");
        await U("Staff1",    "staff1",   "أحمد بن محمد البلوشي",          "TrainingDirectorateStaff");
        await U("Staff2",    "staff2",   "فيصل بن عبدالله المعمري",       "TrainingDirectorateStaff");
        await U("Scheduler", "sched",    "ماجد بن علي الكندي",           "SchedulingOfficer");
        await U("FO",        "finance",  "يوسف بن سعيد العامري",          "FinanceOfficer");
        await U("ITO",       "ito",      "هاشم بن خلفان البوسعيدي",      "InternalTrainingOfficer");
        await U("UGM1",      "ugm1",     "ناصر بن حمود الحارثي",         "UnitGeneralManager");
        await U("UTM1",      "utm1",     "سليمان بن راشد الشكيلي",       "UnitTrainingManager");
        await U("UTM2",      "utm2",     "حمد بن خميس الجابري",          "UnitTrainingManager");
        await U("Emp1",      "emp1",     "عمر بن سالم المشرفي",          "Employee");
        await U("Emp2",      "emp2",     "مالك بن حمد السعدي",           "Employee");
        await U("Emp3",      "emp3",     "علي بن خلفان الوهيبي",         "Employee");
        await U("Emp4",      "emp4",     "محمد بن سالم البلوشي",         "Employee");
        await U("Emp5",      "emp5",     "طارق بن حمد الريامي",          "Employee");
        await U("UGM2",      "ugm2",     "سعود بن هلال المعولي",         "UnitGeneralManager");
        await U("UTM3",      "utm3",     "بدر بن عبدالله السيابي",       "UnitTrainingManager");
        await U("Emp6",      "emp6",     "هيثم بن سعيد الغافري",         "Employee");
        await U("UGM_Eng",   "ugm.eng",  "منصور بن حميد الوهيبي",        "UnitGeneralManager");
        await U("UTM_Eng",   "utm.eng",  "خالد بن سعيد الرواحي",         "UnitTrainingManager");
        await U("TCO1",      "tco1",     "سامي بن علي المقبالي",          "TrainingCenterOfficer");
        await U("TCO2",      "tco2",     "إبراهيم بن محمد الكلباني",     "TrainingCenterOfficer");
        await U("TCM1",      "tcm1",     "عادل بن سليم الحسني",          "TrainingCenterManager");
        await U("TCM2",      "tcm2",     "زاهر بن حمد العبري",           "TrainingCenterManager");
        await U("HROfficer", "hr",       "فهد بن سالم الشحي",            "HROfficer");
    }

    // ── EMPLOYEES (HR records) ──
    private async Task SeedEmployeesAsync(Guid tenantId)
    {
        _employees = new();
        // (key, userId, unitKey, rankId, svcNum, nameAr, dob, joined, education, lang)
        var data = new (string k, string uKey, string ouKey, Guid rk, string svc, string nm, DateTime dob, DateTime jn, string edu, string lang)[]
        {
            ("TH",        "TH",        "HQ",          RankIds.MajorGeneral,   "TH-001",  "سعيد بن حمد الراشدي",      new(1968,3,15), new(1988,6,1),  "Master",     "{\"English\":\"C1\"}"),
            ("TD",        "TD",        "TrainingDir",  RankIds.Brigadier,      "TD-001",  "خالد بن سالم الهنائي",      new(1972,7,22), new(1992,8,1),  "Master",     "{\"English\":\"B2\"}"),
            ("Staff1",    "Staff1",    "TrainingDir",  RankIds.LtColonel,      "ST-001",  "أحمد بن محمد البلوشي",      new(1978,1,10), new(1998,6,15), "Bachelor",   "{\"English\":\"B2\"}"),
            ("Staff2",    "Staff2",    "TrainingDir",  RankIds.Major,          "ST-002",  "فيصل بن عبدالله المعمري",   new(1982,5,5),  new(2002,7,1),  "Bachelor",   "{\"English\":\"B1\"}"),
            ("FO",        "FO",        "TrainingDir",  RankIds.Captain,        "FO-001",  "يوسف بن سعيد العامري",      new(1986,11,3), new(2006,8,1),  "Bachelor",   "{\"English\":\"B1\"}"),
            ("UGM1",      "UGM1",      "Brigade1",     RankIds.Colonel,        "UG-001",  "ناصر بن حمود الحارثي",      new(1975,8,30), new(1995,6,1),  "Master",     "{\"English\":\"B2\"}"),
            ("UTM1",      "UTM1",      "Bn1_1",        RankIds.Major,          "UM-001",  "سليمان بن راشد الشكيلي",    new(1983,2,14), new(2003,7,1),  "Bachelor",   "{\"English\":\"B1\"}"),
            ("UTM2",      "UTM2",      "Bn1_2",        RankIds.Major,          "UM-002",  "حمد بن خميس الجابري",       new(1984,6,25), new(2004,6,1),  "Bachelor",   "{\"English\":\"B1\"}"),
            ("Emp1",      "Emp1",      "Bn1_1",        RankIds.Captain,        "EM-001",  "عمر بن سالم المشرفي",       new(1988,3,5),  new(2008,6,1),  "Bachelor",   "{\"English\":\"B2\"}"),
            ("Emp2",      "Emp2",      "Bn1_1",        RankIds.Captain,        "EM-002",  "مالك بن حمد السعدي",        new(1990,7,18), new(2010,8,1),  "Bachelor",   "{\"English\":\"B2\"}"),
            ("Emp3",      "Emp3",      "Bn1_1",        RankIds.StaffSergeant,  "EM-003",  "علي بن خلفان الوهيبي",      new(1992,1,20), new(2010,6,1),  "HighSchool", "{\"English\":\"A2\"}"),
            ("Emp4",      "Emp4",      "Bn1_2",        RankIds.Corporal,       "EM-004",  "محمد بن سالم البلوشي",      new(1995,12,1), new(2014,6,1),  "HighSchool", "{\"English\":\"A1\"}"),
            ("Emp5",      "Emp5",      "Bn1_2",        RankIds.FirstLieutenant,"EM-005",  "طارق بن حمد الريامي",       new(1993,9,10), new(2013,6,1),  "Bachelor",   "{\"English\":\"B1\"}"),
            ("UGM2",      "UGM2",      "Brigade2",     RankIds.Colonel,        "UG-002",  "سعود بن هلال المعولي",      new(1974,4,8),  new(1994,6,1),  "Master",     "{\"English\":\"B2\"}"),
            ("UTM3",      "UTM3",      "Bn2_1",        RankIds.Major,          "UM-003",  "بدر بن عبدالله السيابي",    new(1981,11,22),new(2001,7,1),  "Bachelor",   "{\"English\":\"B1\"}"),
            ("Emp6",      "Emp6",      "Bn2_1",        RankIds.Captain,        "EM-006",  "هيثم بن سعيد الغافري",      new(1987,6,15), new(2007,8,1),  "Bachelor",   "{\"English\":\"B2\"}"),
            ("UTM_Eng",   "UTM_Eng",   "Engineering",  RankIds.Major,          "UM-004",  "خالد بن سعيد الرواحي",      new(1980,10,5), new(2000,6,1),  "Bachelor",   "{\"English\":\"B1\"}"),
            ("TCO1",      "TCO1",      "Center1",      RankIds.Captain,        "TC-001",  "سامي بن علي المقبالي",      new(1987,3,12), new(2007,6,1),  "Bachelor",   "{\"English\":\"B1\"}"),
            ("TCM1",      "TCM1",      "Center1",      RankIds.LtColonel,      "TM-001",  "عادل بن سليم الحسني",       new(1977,5,30), new(1997,6,1),  "Bachelor",   "{\"English\":\"B1\"}"),
            ("TCO2",      "TCO2",      "Center2",      RankIds.Captain,        "TC-002",  "إبراهيم بن محمد الكلباني",  new(1988,8,25), new(2008,6,1),  "Bachelor",   "{\"English\":\"A2\"}"),
            ("TCM2",      "TCM2",      "Center2",      RankIds.LtColonel,      "TM-002",  "زاهر بن حمد العبري",        new(1979,1,18), new(1999,6,1),  "Bachelor",   "{\"English\":\"B1\"}"),
        };

        foreach (var d in data)
        {
            var empId = guidGenerator.Create();
            await employeeRepo.InsertAsync(new Employee(empId)
            {
                 UserId = _users[d.uKey], MainUnitId = _orgUnits[d.ouKey],
                RankId = d.rk, ServiceNumber = d.svc, FullNameAr = d.nm, FullNameEn = d.nm,
                DateOfBirth = d.dob, JoinDate = d.jn, Education = d.edu,
                Nationality = "Omani", MedicalFitness = true, SecurityClearance = "Active",
                LanguageLevel = d.lang, IsActive = true,
                TenantId= tenantId
            }, autoSave: true);
            _employees[d.k] = empId;
        }
    }

    // ── TENANT COURSES ──
    private async Task SeedTenantCoursesAsync(Guid tenantId)
    {
        _tenantCourses = new();
        var catalogIds = new[] { CatalogIds.Cybersecurity, CatalogIds.Leadership, CatalogIds.ProjectMgmt,
            CatalogIds.NetworkEng, CatalogIds.FirstAid, CatalogIds.StrategicPlanning, CatalogIds.DataAnalysis, CatalogIds.CombatEngineering };

        foreach (var catId in catalogIds)
        {
            var tcId = guidGenerator.Create();
            var cat = await catalogRepo.GetAsync(catId);
            var exist=await tenantCourseRepo.AnyAsync(c=> c.CatalogCourseId== catId && c.TenantId== tenantId);
            if (!exist)
            {
                await tenantCourseRepo.InsertAsync(new TenantCourse(tcId)
                {
                    CatalogCourseId = catId,
                    DefaultCapacity = 20,
                    DefaultDurationWeeks = 2,
                    ResultType = cat.ResultType,
                    RequiresEvaluation = cat.RequiresEvaluation,
                    RequiresProviderEvaluation = cat.RequiresProviderEvaluation,
                    HasCertificate = cat.HasCertificate,
                    EvaluationBlocksCertificate = cat.EvaluationBlocksCertificate,
                    IsActive = true,
                    AddedById = _users["Staff1"],
                    AddedAt = DateTime.Now.AddMonths(-6),
                    TenantId = tenantId
                }, autoSave: true);

                _tenantCourses[catId] = tcId;

                // Copy conditions
                var conds = await catalogConditionRepo.GetListAsync(x => x.CatalogCourseId == catId);
                foreach (var c in conds)
                    await tenantConditionRepo.InsertAsync(new TenantCourseCondition(guidGenerator.Create())
                    { TenantCourseId = tcId, ConditionType = c.ConditionType, ConditionValue = c.ConditionValue, IsActive = true }, autoSave: true);
            }
        }
        
    }

 
        // ── FINANCIAL ITEMS (unique per tenant) ──
private async Task SeedFinancialItemsAsync(Guid tenantId)
    {
        _fiIds = new();

        // Parents
        _fiIds["CourseCost"] = guidGenerator.Create();
        _fiIds["Travel"] = guidGenerator.Create();
 
        // Children
        _fiIds["Tuition"] = guidGenerator.Create();
        _fiIds["Materials"] = guidGenerator.Create();
        _fiIds["Tickets"] = guidGenerator.Create();
        _fiIds["TravelAllowance"] = guidGenerator.Create();
        _fiIds["ClothingAllowance"] = guidGenerator.Create();
        _fiIds["Insurance"] = guidGenerator.Create();
        _fiIds["Visa"] = guidGenerator.Create();
        _fiIds["Hotel"] = guidGenerator.Create();
        _fiIds["Meals"] = guidGenerator.Create();

        await financialItemRepo.InsertManyAsync(new List<FinancialItem>
    {
        // Parents (no ParentId)
        new (_fiIds["Travel"])            { TenantId=tenantId,  NameAr = "التدريب الخارجي",  NameEn = "External Training",    Code = "ET",    VoteCode = "VOT-CC-001", IsActive = true },
        new (_fiIds["CourseCost"])        { TenantId=tenantId,  NameAr = "التدريب الداخلي",   NameEn = "Internal Training", Code = "IT",    VoteCode = "VOT-TE-001", IsActive = true },
         // Children of Course Cost
        new (_fiIds["Tuition"])           { TenantId=tenantId,  NameAr = "تكلفة الدورات",     NameEn = "Course Cost",       Code = "CC-TF", VoteCode = "VOT-CC-TF", ParentId = _fiIds["CourseCost"],    IsActive = true },

        // Children of Travel — with IsPerDay/IsPerNominee flags
        new (_fiIds["Tickets"])          { TenantId=tenantId, NameAr = "التذاكر",       NameEn = "Tickets",            Code = "TE-TK", VoteCode = "VOT-TE-TK", ParentId = _fiIds["Travel"], IsActive = true, IsPerNominee = true, DefaultAmountOMR = 200m },
        new(_fiIds["TravelAllowance"]) { TenantId = tenantId, NameAr = "بدل السفر", NameEn = "Travel Allowance", Code = "TE-TA", VoteCode = "VOT-TE-TA", ParentId = _fiIds["Travel"], IsActive = true, IsPerDay = true, IsPerNominee = true, DefaultAmountOMR = 15m, ExtraDaysBefore = 1, ExtraDaysAfter = 1 },
        new(_fiIds["ClothingAllowance"]) { TenantId = tenantId, NameAr = "بدل الملابس", NameEn = "Clothing Allowance", Code = "TE-CA", VoteCode = "VOT-TE-CA", ParentId = _fiIds["Travel"], IsActive = true, IsPerNominee = true, DefaultAmountOMR = 75m },
        new(_fiIds["Insurance"]) { TenantId = tenantId, NameAr = "التأمين", NameEn = "Insurance", Code = "TE-IN", VoteCode = "VOT-TE-IN", ParentId = _fiIds["Travel"], IsActive = true, IsPerNominee = true, DefaultAmountOMR = 50m },
        new(_fiIds["Visa"]) { TenantId = tenantId, NameAr = "التأشيرة", NameEn = "Visa", Code = "TE-VS", VoteCode = "VOT-TE-VS", ParentId = _fiIds["Travel"], IsActive = true, IsPerNominee = true, DefaultAmountOMR = 25m },

    }, autoSave: true);

        // Seed FinancialItemRankAmounts for Travel Allowance (per-rank rates)
        await SeedFinancialItemRankAmountsAsync(tenantId);
    }

    private async Task SeedFinancialItemRankAmountsAsync(Guid tenantId)
    {
        var travelAllowanceId = _fiIds["TravelAllowance"];
        var rankAmounts = new (Guid rankId, decimal amount)[]
        {
            (RankIds.Colonel, 25m),
            (RankIds.Captain, 15m),
            (RankIds.FirstLieutenant, 12m),
        };
        foreach (var (rankId, amount) in rankAmounts)
        {
            await financialItemRankAmountRepo.InsertAsync(
                new FinancialItemRankAmount(guidGenerator.Create(), travelAllowanceId, rankId, amount)
                { TenantId = tenantId },
                autoSave: true);
        }
    }


    // ── FINANCIAL DEFAULTS (uses _fiIds) ──
    private async Task SeedFinancialDefaultsAsync()
    {
        var extIntlIds = new[] { _fiIds["Tuition"], _fiIds["Materials"], _fiIds["Tickets"], _fiIds["TravelAllowance"],
        _fiIds["ClothingAllowance"], _fiIds["Insurance"], _fiIds["Visa"], _fiIds["Hotel"], _fiIds["Meals"] };

        foreach (var id in extIntlIds)
            await financialDefaultRepo.InsertAsync(new CourseTypeFinancialItemDefault(guidGenerator.Create())
            { CourseType = CourseType.ExternalInternational, FinancialItemId = id }, autoSave: true);

        foreach (var id in new[] { _fiIds["Tuition"], _fiIds["Materials"] })
            await financialDefaultRepo.InsertAsync(new CourseTypeFinancialItemDefault(guidGenerator.Create())
            { CourseType = CourseType.ExternalLocal, FinancialItemId = id }, autoSave: true);
    }

    // ── EXCHANGE RATES ──
    private async Task SeedExchangeRatesAsync()
    {
        await exchangeRateRepo.InsertManyAsync(new List<ExchangeRate>
        {
            new(guidGenerator.Create()) { FromCurrency = "USD", ToCurrency = "OMR", Rate = 0.3850m, SetAt  = new(2026,1,1), IsActive = true },
            new(guidGenerator.Create()) { FromCurrency = "GBP", ToCurrency = "OMR", Rate = 0.4870m, SetAt = new(2026,1,1), IsActive = true },
            new(guidGenerator.Create()) { FromCurrency = "EUR", ToCurrency = "OMR", Rate = 0.4200m, SetAt = new(2026,1,1), IsActive = true },
        }, autoSave: true);
    }

    // ── BUDGETS (uses _fiIds for parent items) ──
    private async Task SeedBudgetsAsync()
    {
        var parentIds = new[] { _fiIds["CourseCost"], _fiIds["Travel"], _fiIds["Accommodation"] };
        foreach (var year in new[] { 2026, 2027 })
            foreach (var fiId in parentIds)
                await budgetRepo.InsertAsync(new TrainingBudget(guidGenerator.Create())
                { Year = year, FinancialItemId = fiId, TotalAmount = 0, SpentAmount = 0, AlertThreshold = 80 }, autoSave: true);
    }

    // ── CENTERS ──
    private async Task SeedCentersAsync(string prefix)
    {
        var c1 = guidGenerator.Create();
        await centerRepo.InsertAsync(new TrainingCenter(c1) { CenterNameAr = "مركز التدريب الأساسي", CenterNameEn = "Basic Training Center", OrgUnitId = _orgUnits["Center1"], Location = "معسكر السلطان قابوس", IsActive = true }, autoSave: true);

        var c2 = guidGenerator.Create();
        await centerRepo.InsertAsync(new TrainingCenter(c2) { CenterNameAr = "مركز التدريب المتقدم", CenterNameEn = "Advanced Training Center", OrgUnitId = _orgUnits["Center2"], Location = "معسكر الشموخ", IsActive = true }, autoSave: true);

        _orgUnits["CenterId1"] = c1;
        _orgUnits["CenterId2"] = c2;

        await centerRoleRepo.InsertAsync(new CenterRoleAssignment(guidGenerator.Create()) { CenterId = c1, RoleType = CenterRoleType.TCO, AssignmentType = CenterAssignmentType.Employee , EmployeeId = _employees["TCO1"] }, autoSave: true);
        await centerRoleRepo.InsertAsync(new CenterRoleAssignment(guidGenerator.Create()) { CenterId = c1, RoleType = CenterRoleType.TCM, AssignmentType = CenterAssignmentType.Employee, EmployeeId = _employees["TCM1"] }, autoSave: true);
        await centerRoleRepo.InsertAsync(new CenterRoleAssignment(guidGenerator.Create()) { CenterId = c2, RoleType = CenterRoleType.TCO, AssignmentType = CenterAssignmentType.Employee, EmployeeId = _employees["TCO2"] }, autoSave: true);
        await centerRoleRepo.InsertAsync(new CenterRoleAssignment(guidGenerator.Create()) { CenterId = c2, RoleType =  CenterRoleType.TCM, AssignmentType = CenterAssignmentType.Employee, EmployeeId = _employees["TCM2"] }, autoSave: true);
    }

    // ── CENTER PLAN WINDOWS ──
    private async Task SeedCenterPlanWindowsAsync()
    {
        await centerWindowRepo.InsertAsync(new CenterPlanWindow(guidGenerator.Create()) { Year = 2027, OpenDate = new(2026,7,1), CloseDate = new(2026,8,31) }, autoSave: true);
    }

    // ── CENTER PLANS ──
    private async Task SeedCenterPlansAsync(Guid tenantId)
    {
        var cpId = guidGenerator.Create();
        await centerPlanRepo.InsertAsync(new TrainingCenterPlan(cpId) { TenantId= tenantId, CenterId = _orgUnits["CenterId1"], Year = 2027, Status = CenterPlanStatus.Approved, SubmittedById = _users["TCO1"], ApprovedById = _users["TCM1"] }, autoSave: true);

        var item1 = guidGenerator.Create();
        await centerPlanItemRepo.InsertAsync(new TrainingCenterPlanItem(item1) { PlanId = cpId, TenantCourseId = _tenantCourses[CatalogIds.Leadership], BeneficiaryType = BeneficiaryType.Internal, EstimatedStartDate = new(2027,2,1), EstimatedEndDate = new(2027,2,14), Capacity = 30 }, autoSave: true);
        await centerPlanItemUnitRepo.InsertAsync(new TrainingCenterPlanItemUnit(guidGenerator.Create()) { PlanItemId = item1, UnitId = _orgUnits["Brigade1"] }, autoSave: true);

        var item2 = guidGenerator.Create();
        await centerPlanItemRepo.InsertAsync(new TrainingCenterPlanItem(item2) { PlanId = cpId, TenantCourseId = _tenantCourses[CatalogIds.FirstAid], BeneficiaryType = BeneficiaryType.Internal , EstimatedStartDate = new(2027,3,1), EstimatedEndDate = new(2027,3,5), Capacity = 40 }, autoSave: true);
    }

    // ── ANNUAL PLAN ──
    private async Task SeedAnnualPlanAsync(string prefix, TenantScenario scenario, Guid tenantId)
    {
        var planId = guidGenerator.Create();
        var plan = new TrainingPlan(planId, 2027)
        {
            OpenDate = new(2026, 9, 1),
            CloseDate = scenario == TenantScenario.EarlyStage ? new DateTime(2026, 11, 30) : new DateTime(2026, 10, 15),
        };
        plan.Status = scenario switch { TenantScenario.FullApproved => PlanStatus.THApproved, TenantScenario.MidWorkflow => PlanStatus.UnderReview, _ => PlanStatus.Open };
        await planRepo.InsertAsync(plan, autoSave: true);

        // Items
        var pi1 = await AddPlanItem(tenantId,planId, CatalogIds.Cybersecurity, CourseType.ExternalInternational, PreferredQuarter.Q2, 1, 5, 10, "تطوير قدرات الأمن السيبراني", "UTM1", "Bn1_1", "NEB-2027-EXT-001");
        var pi2 = await AddPlanItem(tenantId,planId, CatalogIds.Leadership, CourseType.Internal, PreferredQuarter.Q1, 2, 8, 22, "رفع الكفاءة القيادية", "UTM1", "Bn1_1", null);
        var pi3 = await AddPlanItem(tenantId,planId, CatalogIds.ProjectMgmt, CourseType.ExternalLocal, PreferredQuarter.Q3, 3, 3, 7, "تأهيل ضباط الهندسة لإدارة المشاريع", "UTM_Eng", "Engineering", "NEB-2027-LOC-005");
        var pi4 = await AddPlanItem(tenantId,planId, CatalogIds.DataAnalysis, CourseType.ExternalInternational, PreferredQuarter.Q4, 2, 2, 3, "بناء قدرات تحليل البيانات", "UTM2", "Bn1_2", "NEB-2027-EXT-010");

        // Financial assignments
        if (scenario == TenantScenario.FullApproved)
        {
            await AssignFinancials(pi1, true); await AssignFinancials(pi3, false); await AssignFinancials(pi4, true);
            await SeedPostApprovalAsync(pi1, pi2);
        }
        else if (scenario == TenantScenario.MidWorkflow)
        {
            await AssignFinancials(pi1, true); await AssignFinancials(pi4, true);
            // pi3 deliberately missing — cost gate test
        }
    }

    private async Task<Guid> AddPlanItem(Guid tenantId, Guid planId, Guid catId, CourseType ct, PreferredQuarter q, int pri, int off, int enl, string just, string utmKey, string unitKey, string? funding)
    {
        var id = guidGenerator.Create();
        await planItemRepo.InsertAsync(new TrainingPlanItem(id, planId, _tenantCourses[catId], ct, q, pri, just, _users[utmKey])
        {
            UnitId = _orgUnits[unitKey], FundingSource = funding, DurationDays = ct == CourseType.Internal ? 10 : 14,
            TenantId= tenantId
        }, autoSave: true);

        var conds = await tenantConditionRepo.GetListAsync(x => x.TenantCourseId == _tenantCourses[catId]);
        foreach (var c in conds)
            await planItemConditionRepo.InsertAsync(new PlanItemCondition(guidGenerator.Create(), id, c.ConditionType, c.ConditionValue), autoSave: true);

        return id;
    }

    // ── ASSIGN FINANCIALS (uses _fiIds) ──
    private async Task AssignFinancials(Guid planItemId, bool isIntl)
    {
        if (isIntl)
        {
            var items = new (string key, decimal amt)[]
            {
            ("Tuition", 850m), ("Materials", 50m), ("Tickets", 200m),
            ("TravelAllowance", 150m), ("ClothingAllowance", 75m),
            ("Insurance", 50m), ("Visa", 25m), ("Hotel", 300m), ("Meals", 100m),
            };
            foreach (var (key, amt) in items)
                await planItemFinancialRepo.InsertAsync(new PlanItemFinancialItem(
                    guidGenerator.Create(), planItemId, _fiIds[key], amt)
                { EstimatedAmountUSD = Math.Round(amt / 0.385m, 2) }, autoSave: true);
        }
        else
        {
            await planItemFinancialRepo.InsertAsync(new PlanItemFinancialItem(
                guidGenerator.Create(), planItemId, _fiIds["Tuition"], 350m)
            { EstimatedAmountUSD = 909.09m }, autoSave: true);
            await planItemFinancialRepo.InsertAsync(new PlanItemFinancialItem(
                guidGenerator.Create(), planItemId, _fiIds["Materials"], 25m)
            { EstimatedAmountUSD = 64.94m }, autoSave: true);
        }
    }

    // ── POST-APPROVAL (FullApproved only) ──
    private async Task SeedPostApprovalAsync(Guid cyberPlanItemId, Guid leaderPlanItemId)
    {
        var cyberCourse = guidGenerator.Create();
        await courseRepo.InsertAsync(new Course(cyberCourse, _tenantCourses[CatalogIds.Cybersecurity], CourseType.ExternalInternational) { Status = CourseStatus.Active }, autoSave: true);

        var cyberSession = guidGenerator.Create();
        await sessionRepo.InsertAsync(new CourseSession(cyberSession, cyberCourse, "S-2027-001", new(2027,4,15), new(2027,4,28), 15)
            { Location = "واشنطن، الولايات المتحدة", Country = "US", Cost = 1800m, Status = SessionStatus.Scheduled }, autoSave: true);

        var leaderCourse = guidGenerator.Create();
        await courseRepo.InsertAsync(new Course(leaderCourse, _tenantCourses[CatalogIds.Leadership], CourseType.Internal) { Status = CourseStatus.Active }, autoSave: true);

        var leaderSession = guidGenerator.Create();
        await sessionRepo.InsertAsync(new CourseSession(leaderSession, leaderCourse, "S-2027-002", new(2027,2,1), new(2027,2,14), 30)
            { Location = "مركز التدريب الأساسي", Country = "OM", Status = SessionStatus.Scheduled }, autoSave: true);

        // Nominations — use PlanItemId + Employee IDs
        await CreateNomination(cyberPlanItemId, cyberSession, _employees["Emp1"], _users["UTM1"], _users["UGM1"], _users["TD"], NominationStatus.TDApproved);
        await CreateNomination(cyberPlanItemId, cyberSession, _employees["Emp2"], _users["UTM1"], _users["UGM1"], null, NominationStatus.UGMApproved);
        await CreateNomination(cyberPlanItemId, cyberSession, _employees["Emp3"], _users["UTM1"], null, null, NominationStatus.UTMApproved);
        await CreateNominationRejected(cyberPlanItemId, cyberSession, _employees["Emp5"], _users["UTM2"], _users["UGM1"], "الرتبة أقل من المطلوب");
        await CreateNomination(leaderPlanItemId, leaderSession, _employees["Emp1"], _users["UTM1"], _users["UGM1"], _users["TD"], NominationStatus.TDApproved);
        await CreateNomination(leaderPlanItemId, leaderSession, _employees["Emp6"], _users["UTM3"], _users["UGM2"], _users["TD"], NominationStatus.TDApproved);

        // Price Quotes
        await quoteRepo.InsertAsync(new PriceQuote(guidGenerator.Create(), cyberSession, ProviderIds.SANS, PricingType.PerPerson, 56.667m, 15) { Status = ApprovalStatus.Approved }, autoSave: true);
        await quoteRepo.InsertAsync(new PriceQuote(guidGenerator.Create(), cyberSession, ProviderIds.LocalAcademy, PricingType.Total, 1000m, 15) { Status = ApprovalStatus.Rejected }, autoSave: true);
    }

    private async Task CreateNomination(Guid planItemId, Guid sessionId, Guid employeeId, Guid utmId, Guid? ugmId, Guid? tdId, NominationStatus status)
    {
        var nom = new Nomination(guidGenerator.Create(), planItemId, employeeId, utmId)
        {
            SessionId = sessionId,
            Status = status,
            ApprovedAt = status == NominationStatus.TDApproved ? DateTime.Now.AddDays(-10) : null
        };
        await nominationRepo.InsertAsync(nom, autoSave: true);

        await approvalRepo.InsertAsync(new NominationApproval(guidGenerator.Create(), nom.Id, 1) { ApprovedById = utmId, Status = ApprovalStatus.Approved, ActionDate = DateTime.Now.AddDays(-15) }, autoSave: true);
        await approvalRepo.InsertAsync(new NominationApproval(guidGenerator.Create(), nom.Id, 2) { ApprovedById = ugmId, Status = ugmId.HasValue ? ApprovalStatus.Approved : ApprovalStatus.Pending, ActionDate = ugmId.HasValue ? DateTime.Now.AddDays(-12) : null }, autoSave: true);
        await approvalRepo.InsertAsync(new NominationApproval(guidGenerator.Create(), nom.Id, 3) { ApprovedById = tdId, Status = tdId.HasValue ? ApprovalStatus.Approved : ApprovalStatus.Pending, ActionDate = tdId.HasValue ? DateTime.Now.AddDays(-10) : null }, autoSave: true);
    }

    private async Task CreateNominationRejected(Guid planItemId, Guid sessionId, Guid employeeId, Guid utmId, Guid ugmId, string reason)
    {
        var nom = new Nomination(guidGenerator.Create(), planItemId, employeeId, utmId)
        {
            SessionId = sessionId,
            Status = NominationStatus.Rejected
        };
        await nominationRepo.InsertAsync(nom, autoSave: true);

        await approvalRepo.InsertAsync(new NominationApproval(guidGenerator.Create(), nom.Id, 1) { ApprovedById = utmId, Status = ApprovalStatus.Approved, ActionDate = DateTime.Now.AddDays(-7) }, autoSave: true);
        await approvalRepo.InsertAsync(new NominationApproval(guidGenerator.Create(), nom.Id, 2) { ApprovedById = ugmId, Status = ApprovalStatus.Rejected, ActionDate = DateTime.Now.AddDays(-5), Notes = reason }, autoSave: true);
        await approvalRepo.InsertAsync(new NominationApproval(guidGenerator.Create(), nom.Id, 3), autoSave: true);
    }

    // ── PROPOSALS ──
    private async Task SeedProposalsAsync(Guid tenantId)
    {
        await proposalRepo.InsertAsync(new CourseProposal(guidGenerator.Create()) { TenantId=tenantId,  CourseNameAr = "تحليل المخاطر الأمنية", CourseNameEn = "Security Risk Analysis", Category = "Military", Nature = "Qualifying", FieldId = FieldIds.Security, Status = ProposalStatus.Pending, ProposedById = _users["UTM1"] }, autoSave: true);
        await proposalRepo.InsertAsync(new CourseProposal(guidGenerator.Create()) { TenantId = tenantId, CourseNameAr = "صيانة المدرعات", CourseNameEn = "Armored Vehicle Maintenance", Category = "Military", Nature = "Mandatory", FieldId = FieldIds.Engineering, Status = ProposalStatus.Approved, ProposedById = _users["UTM3"] }, autoSave: true);
    }

    // ── CASUAL COURSES (Phase 4A) ──
    // Seeds 3 casual courses per tenant at Draft / UnderReview / THApproved.
    // Queries the DB directly so it survives even when other Seed* methods are
    // disabled — if required prerequisites (tenant courses, UTM user, units) are
    // missing, the method logs a warning and exits cleanly per Risk #6.
    private async Task SeedCasualCoursesAsync(Guid tenantId)
    {
        if (await casualCourseRepo.AnyAsync(x => x.TenantId == tenantId))
            return;

        var tenantCourses = await tenantCourseRepo.GetListAsync(x => x.TenantId == tenantId && x.IsActive);
        if (tenantCourses.Count < 3)
        {
            logger.LogWarning("Skipping casual course seed — tenant {TenantId} has fewer than 3 active tenant courses ({Count}).", tenantId, tenantCourses.Count);
            return;
        }

        var utm = await employeeRepo.FirstOrDefaultAsync(x => x.TenantId == tenantId);
        if (utm == null)
        {
            logger.LogWarning("Skipping casual course seed — tenant {TenantId} has no employees.", tenantId);
            return;
        }

        var picked = tenantCourses.Take(3).ToList();
        var today = DateTime.Today;
        var scenarios = new[]
        {
            (status: CasualCourseStatus.Draft,       courseType: CourseType.Internal,              durationDays: 5,  fromOffset: 60, fundingCode: (string?)null),
            (status: CasualCourseStatus.UnderReview, courseType: CourseType.ExternalLocal,         durationDays: 7,  fromOffset: 75, fundingCode: (string?)"NEBR-2026-1"),
            (status: CasualCourseStatus.THApproved,  courseType: CourseType.ExternalInternational, durationDays: 10, fromOffset: 90, fundingCode: (string?)"NEBR-2026-2"),
        };

        for (int i = 0; i < scenarios.Length; i++)
        {
            var s = scenarios[i];
            var from = today.AddDays(s.fromOffset);
            var to = from.AddDays(s.durationDays - 1);
            var cc = new CasualCourse(
                guidGenerator.Create(),
                picked[i].Id,
                utm.MainUnitId,
                utm.UserId,
                s.courseType,
                priority: i + 1,
                justification: $"دورة تجريبية رقم {i + 1} — حالة {s.status}",
                durationDays: s.durationDays,
                from: from,
                to: to)
            {
                TenantId = tenantId,
                FundingSource = s.fundingCode,
                Status = s.status,
                DescriptionAr = "بيانات تجريبية لأغراض العرض",
                ObjectivesAr = "تجربة تدفق الدورات العارضة",
            };

            // Financials + scenario only for THApproved
            if (s.status == CasualCourseStatus.THApproved)
            {
                cc.FundingScenario = FundingScenario.FundingSourceCoversAll;
                var defaults = await financialDefaultRepo.GetListAsync(x => x.CourseType == s.courseType && x.TenantId == tenantId);
                if (defaults.Any())
                {
                    decimal total = 0;
                    foreach (var def in defaults)
                    {
                        var fi = await financialItemRepo.FindAsync(def.FinancialItemId);
                        if (fi == null) continue;
                        var amount = financialItemDefaultResolver.ComputeSubtotal(
                            fi.DefaultAmountOMR, fi.IsPerDay, fi.IsPerNominee,
                            s.durationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, nomineeCount: 3);
                        total += amount;
                    }
                    cc.EstimatedTotalCost = total;
                }
            }

            await casualCourseRepo.InsertAsync(cc, autoSave: true);

            // Nominations (2 or 3 depending on scenario)
            var nomineeCount = s.status == CasualCourseStatus.THApproved ? 3 : 2;
            var employees = await employeeRepo.GetListAsync(x => x.TenantId == tenantId && x.IsActive);
            foreach (var emp in employees.Take(nomineeCount))
            {
                await casualCourseNominationRepo.InsertAsync(
                    new CasualCourseNomination(guidGenerator.Create(), cc.Id, emp.Id)
                    {
                        TenantId = tenantId,
                    },
                    autoSave: true);
            }

            // Financial rows for THApproved
            if (s.status == CasualCourseStatus.THApproved && cc.EstimatedTotalCost.HasValue)
            {
                var defaults = await financialDefaultRepo.GetListAsync(x => x.CourseType == s.courseType && x.TenantId == tenantId);
                foreach (var def in defaults)
                {
                    var fi = await financialItemRepo.FindAsync(def.FinancialItemId);
                    if (fi == null) continue;
                    var amount = financialItemDefaultResolver.ComputeSubtotal(
                        fi.DefaultAmountOMR, fi.IsPerDay, fi.IsPerNominee,
                        s.durationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, nomineeCount: 3);
                    await casualCourseFinancialRepo.InsertAsync(
                        new CasualCourseFinancial(
                            guidGenerator.Create(), cc.Id, fi.Id, amount, FinancialAmountSource.FundingSource)
                        {
                            TenantId = tenantId,
                        },
                        autoSave: true);
                }
            }

            // Approval-chain notes on UnderReview and THApproved
            if (s.status == CasualCourseStatus.UnderReview)
            {
                await planNoteRepo.InsertAsync(
                    new PlanNote(guidGenerator.Create(),
                        PlanNoteEntityType.CasualCourse, cc.Id,
                        "موافقة — يُرفع للاعتماد المالي",
                        PlanNoteAuthorRole.UGM, false)
                    { TenantId = tenantId },
                    autoSave: true);
            }
            else if (s.status == CasualCourseStatus.THApproved)
            {
                await planNoteRepo.InsertManyAsync(new[]
                {
                    new PlanNote(guidGenerator.Create(), PlanNoteEntityType.CasualCourse, cc.Id,
                        "موافقة UGM", PlanNoteAuthorRole.UGM, false) { TenantId = tenantId },
                    new PlanNote(guidGenerator.Create(), PlanNoteEntityType.CasualCourse, cc.Id,
                        "مراجعة مالية مكتملة", PlanNoteAuthorRole.Staff, false) { TenantId = tenantId },
                    new PlanNote(guidGenerator.Create(), PlanNoteEntityType.CasualCourse, cc.Id,
                        "اعتماد المدير", PlanNoteAuthorRole.TD, false) { TenantId = tenantId },
                    new PlanNote(guidGenerator.Create(), PlanNoteEntityType.CasualCourse, cc.Id,
                        "اعتماد القائد", PlanNoteAuthorRole.TH, false) { TenantId = tenantId },
                }, autoSave: true);
            }
        }
    }
}
