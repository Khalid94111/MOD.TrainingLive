using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using System;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training;

public class FinanceDataSeeder(
    IRepository<FinancialItem, Guid> financialItemRepo,
    IRepository<CourseTypeFinancialItemDefault, Guid> defaultRepo,
    IRepository<ExchangeRate, Guid> exchangeRateRepo,
    IRepository<TrainingBudget, Guid> budgetRepo)
    : IDataSeedContributor, ITransientDependency
{
    public async Task SeedAsync(DataSeedContext context)
    {
        if (await financialItemRepo.AnyAsync()) return;

        // ============================================================
        // Parent 1: التدريب الخارجي الدولي (External Int'l Training)
        // ============================================================
        var parentExtIntl = new FinancialItem
        {
            NameAr = "التدريب الخارجي الدولي",
            NameEn = "External International Training",
            Code = "ت.خ.د",
            VoteCode = "",
            IsGeneral = true,
            IsActive = true
        };
        await financialItemRepo.InsertAsync(parentExtIntl, autoSave: true);

        // Sub-items under External Int'l
        var expatriation = new FinancialItem
        {
            ParentId = parentExtIntl.Id,
            NameAr = "بدل الغربة",
            NameEn = "Expatriation Allowance",
            Code = "ب.غ",
            VoteCode = "VT-EXP-001",
            IsGeneral = false,
            IsActive = true
        };
        await financialItemRepo.InsertAsync(expatriation, autoSave: true);

        var tickets = new FinancialItem
        {
            ParentId = parentExtIntl.Id,
            NameAr = "تذاكر السفر",
            NameEn = "Flight Tickets",
            Code = "ت.س",
            VoteCode = "VT-TKT-002",
            IsGeneral = false,
            IsActive = true
        };
        await financialItemRepo.InsertAsync(tickets, autoSave: true);

        var insurance = new FinancialItem
        {
            ParentId = parentExtIntl.Id,
            NameAr = "التأمين الصحي",
            NameEn = "Health Insurance",
            Code = "ت.ص",
            VoteCode = "VT-INS-003",
            IsGeneral = false,
            IsActive = true
        };
        await financialItemRepo.InsertAsync(insurance, autoSave: true);

        var clothing = new FinancialItem
        {
            ParentId = parentExtIntl.Id,
            NameAr = "بدل الملابس",
            NameEn = "Clothing Allowance",
            Code = "ب.م",
            VoteCode = "VT-CLT-004",
            IsGeneral = false,
            IsActive = true
        };
        await financialItemRepo.InsertAsync(clothing, autoSave: true);

        var visa = new FinancialItem
        {
            ParentId = parentExtIntl.Id,
            NameAr = "التأشيرة",
            NameEn = "Visa",
            Code = "ت.أ",
            VoteCode = "VT-VSA-005",
            IsGeneral = false,
            IsActive = true
        };
        await financialItemRepo.InsertAsync(visa, autoSave: true);

        // ============================================================
        // Parent 2: تكلفة الدورة (Course Cost)
        // ============================================================
        var parentCourseCost = new FinancialItem
        {
            NameAr = "تكلفة الدورة",
            NameEn = "Course Cost",
            Code = "ت.د",
            VoteCode = "",
            IsGeneral = true,
            IsActive = true
        };
        await financialItemRepo.InsertAsync(parentCourseCost, autoSave: true);

        var courseFees = new FinancialItem
        {
            ParentId = parentCourseCost.Id,
            NameAr = "رسوم الدورة",
            NameEn = "Course Fees",
            Code = "ر.د",
            VoteCode = "VT-CRS-006",
            IsGeneral = false,
            IsActive = true
        };
        await financialItemRepo.InsertAsync(courseFees, autoSave: true);

        // ============================================================
        // CourseType Financial Item Defaults
        // ============================================================

        // External International: 6 items
        await defaultRepo.InsertAsync(new CourseTypeFinancialItemDefault
        {
            CourseType = CourseType.ExternalInternational,
            FinancialItemId = courseFees.Id,
            SortOrder = 1
        }, autoSave: true);

        await defaultRepo.InsertAsync(new CourseTypeFinancialItemDefault
        {
            CourseType = CourseType.ExternalInternational,
            FinancialItemId = expatriation.Id,
            SortOrder = 2
        }, autoSave: true);

        await defaultRepo.InsertAsync(new CourseTypeFinancialItemDefault
        {
            CourseType = CourseType.ExternalInternational,
            FinancialItemId = tickets.Id,
            SortOrder = 3
        }, autoSave: true);

        await defaultRepo.InsertAsync(new CourseTypeFinancialItemDefault
        {
            CourseType = CourseType.ExternalInternational,
            FinancialItemId = insurance.Id,
            SortOrder = 4
        }, autoSave: true);

        await defaultRepo.InsertAsync(new CourseTypeFinancialItemDefault
        {
            CourseType = CourseType.ExternalInternational,
            FinancialItemId = clothing.Id,
            SortOrder = 5
        }, autoSave: true);

        await defaultRepo.InsertAsync(new CourseTypeFinancialItemDefault
        {
            CourseType = CourseType.ExternalInternational,
            FinancialItemId = visa.Id,
            SortOrder = 6
        }, autoSave: true);

        // External Local: 1 item (course fees only)
        await defaultRepo.InsertAsync(new CourseTypeFinancialItemDefault
        {
            CourseType = CourseType.ExternalLocal,
            FinancialItemId = courseFees.Id,
            SortOrder = 1
        }, autoSave: true);

        // ============================================================
        // Exchange Rate: USD → OMR
        // ============================================================
        await exchangeRateRepo.InsertAsync(new ExchangeRate
        {
            FromCurrency = "USD",
            ToCurrency = "OMR",
            Rate = 0.385m,
            IsActive = true,
            SetAt = DateTime.UtcNow
        }, autoSave: true);

        // ============================================================
        // Training Budgets: 4 types for current year
        // ============================================================
        var currentYear = DateTime.Now.Year;

        await budgetRepo.InsertAsync(new TrainingBudget
        {
            Year = currentYear,
            BudgetType = BudgetType.Internal,
            TotalAmount = 500_000m,
            SpentAmount = 0m,
            AlertThreshold = 80m
        }, autoSave: true);

        await budgetRepo.InsertAsync(new TrainingBudget
        {
            Year = currentYear,
            BudgetType = BudgetType.ExternalInternational,
            TotalAmount = 1_000_000m,
            SpentAmount = 0m,
            AlertThreshold = 80m
        }, autoSave: true);

        await budgetRepo.InsertAsync(new TrainingBudget
        {
            Year = currentYear,
            BudgetType = BudgetType.Planning,
            TotalAmount = 200_000m,
            SpentAmount = 0m,
            AlertThreshold = 80m
        }, autoSave: true);

        await budgetRepo.InsertAsync(new TrainingBudget
        {
            Year = currentYear,
            BudgetType = BudgetType.HigherEducation,
            TotalAmount = 300_000m,
            SpentAmount = 0m,
            AlertThreshold = 80m
        }, autoSave: true);
    }
}
