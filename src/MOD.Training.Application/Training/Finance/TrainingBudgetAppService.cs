using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance.Dtos;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Payments;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Plans;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Finance;

[Authorize(TrainingPermissions.TrainingBudgets.Default)]
public class TrainingBudgetAppService(
    IRepository<TrainingBudget, Guid> budgetRepo,
    IRepository<FinancialItem, Guid> financialItemRepo,
    IRepository<TrainingPlan, Guid> planRepo,
    IRepository<TrainingPlanItem, Guid> planItemRepo,
    IRepository<PlanItemFinancialItem, Guid> planFinancialRepo,
    IRepository<CourseSession, Guid> sessionRepo,
    IRepository<CoursePayment, Guid> coursePaymentRepo,
    IRepository<TravelAllowancePayment, Guid> travelPaymentRepo,
    IRepository<CasualCourse, Guid> casualCourseRepo,
    IRepository<TrainingExpenseRecovery, Guid> recoveryRepo,
    IRepository<TrainingExpenseRecoveryItem, Guid> recoveryItemRepo,
    CourseNameResolver courseNameResolver)
    : ApplicationService, ITrainingBudgetAppService
{
    private const string AllocationActivity = "AnnualPlanAllocation";
    private const string CoursePaymentActivity = "CoursePayment";
    private const string TravelPaymentActivity = "TravelPayment";
    private const string CasualTravelExpenseActivity = "CasualTravelExpense";

    public async Task<TrainingBudgetDto> GetAsync(Guid id)
    {
        var budget = await budgetRepo.GetAsync(id);
        return (await BuildBudgetDtosAsync(budget.Year, budget.FinancialItemId)).Single();
    }

    public async Task<PagedResultDto<TrainingBudgetDto>> GetListAsync(
        TrainingBudgetGetListInput input)
    {
        var year = input.Year ?? Clock.Now.Year;
        var items = await BuildBudgetDtosAsync(year, input.FinancialItemId);
        var totalCount = items.Count;
        var page = items
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount > 0 ? input.MaxResultCount : totalCount)
            .ToList();

        return new PagedResultDto<TrainingBudgetDto>(totalCount, page);
    }

    public async Task<List<int>> GetYearsAsync()
    {
        var years = new HashSet<int> { Clock.Now.Year };

        var budgetQuery = await budgetRepo.GetQueryableAsync();
        years.UnionWith(await AsyncExecuter.ToListAsync(budgetQuery.Select(x => x.Year).Distinct()));

        var planQuery = await planRepo.GetQueryableAsync();
        years.UnionWith(await AsyncExecuter.ToListAsync(planQuery.Select(x => x.Year).Distinct()));

        var sessionQuery = await sessionRepo.GetQueryableAsync();
        years.UnionWith(await AsyncExecuter.ToListAsync(sessionQuery.Select(x => x.PlanYear).Distinct()));

        var recoveryQuery = await recoveryRepo.GetQueryableAsync();
        years.UnionWith(await AsyncExecuter.ToListAsync(
            recoveryQuery.Select(x => x.ExpenseDate.Year).Distinct()));

        return years.Where(x => x > 0).OrderByDescending(x => x).ToList();
    }

    [Authorize(TrainingPermissions.TrainingBudgets.Edit)]
    public async Task<TrainingBudgetDto> UpdateAsync(
        Guid id,
        UpdateAlertThresholdDto input)
    {
        var entity = await budgetRepo.GetAsync(id);
        entity.AlertThreshold = input.AlertThreshold;
        await budgetRepo.UpdateAsync(entity, autoSave: true);

        return (await BuildBudgetDtosAsync(entity.Year, entity.FinancialItemId)).Single();
    }

    [Authorize(TrainingPermissions.TrainingBudgets.Edit)]
    public async Task<TrainingBudgetDto> SetThresholdAsync(
        Guid financialItemId,
        int year,
        UpdateAlertThresholdDto input)
    {
        await financialItemRepo.GetAsync(financialItemId);
        var entity = await budgetRepo.FindAsync(x =>
            x.Year == year && x.FinancialItemId == financialItemId);

        if (entity == null)
        {
            entity = new TrainingBudget(GuidGenerator.Create())
            {
                TenantId = CurrentTenant.Id,
                Year = year,
                FinancialItemId = financialItemId,
                TotalAmount = 0m,
                SpentAmount = 0m,
                AlertThreshold = input.AlertThreshold
            };
            await budgetRepo.InsertAsync(entity, autoSave: true);
        }
        else
        {
            entity.AlertThreshold = input.AlertThreshold;
            await budgetRepo.UpdateAsync(entity, autoSave: true);
        }

        return (await BuildBudgetDtosAsync(year, financialItemId)).Single();
    }

    private async Task<List<TrainingBudgetDto>> BuildBudgetDtosAsync(
        int year,
        Guid? financialItemFilter)
    {
        var financialItems = await financialItemRepo.GetListAsync();
        var financialItemsById = financialItems.ToDictionary(x => x.Id);
        var financialItemsByVoteCode = financialItems
            .Where(x => !string.IsNullOrWhiteSpace(x.VoteCode))
            .GroupBy(x => x.VoteCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);

        var budgets = await budgetRepo.GetListAsync(x => x.Year == year);
        var budgetsByFinancialItem = budgets
            .GroupBy(x => x.FinancialItemId)
            .ToDictionary(x => x.Key, x => x.Last());

        var planQuery = await planRepo.GetQueryableAsync();
        var approvedPlans = await AsyncExecuter.ToListAsync(planQuery.Where(x =>
            x.Year == year && x.Status == PlanStatus.THApproved));
        var approvedPlanIds = approvedPlans.Select(x => x.Id).ToList();

        var planItemQuery = await planItemRepo.GetQueryableAsync();
        var planItems = approvedPlanIds.Count == 0
            ? []
            : await AsyncExecuter.ToListAsync(
                planItemQuery.Where(x => approvedPlanIds.Contains(x.PlanId)));
        var planItemsById = planItems.ToDictionary(x => x.Id);
        var planItemIds = planItems.Select(x => x.Id).ToList();

        var planFinancialQuery = await planFinancialRepo.GetQueryableAsync();
        var planFinancials = planItemIds.Count == 0
            ? []
            : await AsyncExecuter.ToListAsync(
                planFinancialQuery.Where(x => planItemIds.Contains(x.PlanItemId)));
        var planFinancialsByPlanItem = planFinancials
            .GroupBy(x => x.PlanItemId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var sessionQuery = await sessionRepo.GetQueryableAsync();
        var sessions = await AsyncExecuter.ToListAsync(sessionQuery.Where(x =>
            x.PlanYear == year && x.Status != SessionStatus.Cancelled));
        var sessionsById = sessions.ToDictionary(x => x.Id);
        var sessionIds = sessions.Select(x => x.Id).ToList();

        var coursePaymentQuery = await coursePaymentRepo.GetQueryableAsync();
        var annualCoursePayments = sessionIds.Count == 0
            ? []
            : await AsyncExecuter.ToListAsync(coursePaymentQuery.Where(x =>
                x.Status == PaymentStatus.Confirmed
                && x.SessionId.HasValue
                && sessionIds.Contains(x.SessionId.Value)));

        var travelPaymentQuery = await travelPaymentRepo.GetQueryableAsync();
        var annualTravelPayments = sessionIds.Count == 0
            ? []
            : await AsyncExecuter.ToListAsync(travelPaymentQuery.Where(x =>
                x.Status == PaymentStatus.Confirmed
                && x.SessionId.HasValue
                && sessionIds.Contains(x.SessionId.Value)));

        var recoveryQuery = await recoveryRepo.GetQueryableAsync();
        var recoveries = await AsyncExecuter.ToListAsync(recoveryQuery.Where(x =>
            x.ExpenseDate.Year == year));
        var recoveriesById = recoveries.ToDictionary(x => x.Id);
        var recoveryIds = recoveries.Select(x => x.Id).ToList();

        var recoveryItemQuery = await recoveryItemRepo.GetQueryableAsync();
        var recoveryItems = recoveryIds.Count == 0
            ? []
            : await AsyncExecuter.ToListAsync(recoveryItemQuery.Where(x =>
                recoveryIds.Contains(x.TrainingExpenseRecoveryId)));

        var casualCourseIds = recoveries.Select(x => x.CasualCourseId).Distinct().ToList();
        var casualCourseQuery = await casualCourseRepo.GetQueryableAsync();
        var casualCourses = casualCourseIds.Count == 0
            ? []
            : await AsyncExecuter.ToListAsync(
                casualCourseQuery.Where(x => casualCourseIds.Contains(x.Id)));
        var casualCoursesById = casualCourses.ToDictionary(x => x.Id);

        var tenantCourseIds = planItems.Select(x => x.TenantCourseId)
            .Concat(sessions.Select(x => x.TenantCourseId))
            .Concat(casualCourses.Select(x => x.TenantCourseId))
            .Distinct()
            .ToList();
        var courseNames = await courseNameResolver.BatchResolveAsync(tenantCourseIds);

        var accumulators = new Dictionary<Guid, BudgetAccumulator>();

        BudgetAccumulator GetAccumulator(Guid financialItemId)
        {
            if (!accumulators.TryGetValue(financialItemId, out var accumulator))
            {
                accumulator = new BudgetAccumulator();
                accumulators[financialItemId] = accumulator;
            }
            return accumulator;
        }

        string ResolveCourseName(Guid tenantCourseId)
            => courseNames.TryGetValue(tenantCourseId, out var name)
                ? name.NameAr
                : string.Empty;

        Guid? ResolveFinancialItemId(
            Guid? trainingPlanItemId,
            FinancialItemType type)
        {
            if (trainingPlanItemId.HasValue
                && planFinancialsByPlanItem.TryGetValue(trainingPlanItemId.Value, out var assigned))
            {
                var assignedMatch = assigned.FirstOrDefault(x =>
                    financialItemsById.TryGetValue(x.FinancialItemId, out var item)
                    && item.ItemType == type);
                if (assignedMatch != null)
                {
                    return assignedMatch.FinancialItemId;
                }
            }

            return financialItems
                .Where(x => x.IsActive && x.ItemType == type)
                .OrderBy(x => x.CreationTime)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefault();
        }

        foreach (var assignment in planFinancials)
        {
            if (!planItemsById.TryGetValue(assignment.PlanItemId, out var planItem))
            {
                continue;
            }

            var accumulator = GetAccumulator(assignment.FinancialItemId);
            accumulator.Allocated += assignment.EstimatedAmountOMR;
            if (assignment.EstimatedAmountOMR == 0m)
            {
                continue;
            }

            accumulator.Activities.Add(new TrainingBudgetActivityDto
            {
                ActivityType = AllocationActivity,
                SourceId = assignment.Id,
                TrainingCourseId = planItem.Id,
                CourseNameAr = ResolveCourseName(planItem.TenantCourseId),
                ActivityDate = assignment.CreationTime,
                AllocatedAmountOMR = assignment.EstimatedAmountOMR,
                StatusCode = PlanStatus.THApproved.ToString()
            });
        }

        foreach (var payment in annualCoursePayments)
        {
            if (!payment.SessionId.HasValue
                || !sessionsById.TryGetValue(payment.SessionId.Value, out var session))
            {
                continue;
            }

            var financialItemId = ResolveFinancialItemId(
                session.TrainingPlanItemId,
                FinancialItemType.CourseCost);
            if (!financialItemId.HasValue)
            {
                continue;
            }

            var accumulator = GetAccumulator(financialItemId.Value);
            accumulator.GrossSpent += payment.InvoiceAmountOMR;
            accumulator.Activities.Add(new TrainingBudgetActivityDto
            {
                ActivityType = CoursePaymentActivity,
                SourceId = payment.Id,
                TrainingCourseId = session.Id,
                CourseNameAr = ResolveCourseName(session.TenantCourseId),
                ActivityDate = payment.InvoiceDate,
                SpentAmountOMR = payment.InvoiceAmountOMR,
                StatusCode = payment.Status.ToString()
            });
        }

        foreach (var sessionGroup in annualTravelPayments
                     .Where(x => x.SessionId.HasValue)
                     .GroupBy(x => x.SessionId!.Value))
        {
            if (!sessionsById.TryGetValue(sessionGroup.Key, out var session))
            {
                continue;
            }

            var activityDate = sessionGroup
                .Select(x => x.ConfirmedAt ?? x.ExternalResponseAt ?? x.CreationTime)
                .Max();
            var components = new[]
            {
                (Type: FinancialItemType.Ticket, Amount: sessionGroup.Sum(x => x.TicketAmountOMR)),
                (Type: FinancialItemType.Visa, Amount: sessionGroup.Sum(x => x.VisaFeesOMR)),
                (Type: FinancialItemType.Insurance, Amount: sessionGroup.Sum(x => x.InsuranceOMR)),
                (Type: FinancialItemType.Allowance, Amount: sessionGroup.Sum(x => x.TravelAllowanceOMR)),
                (Type: FinancialItemType.Clothing, Amount: sessionGroup.Sum(x => x.ClothingAllowanceOMR))
            };

            foreach (var component in components.Where(x => x.Amount != 0m))
            {
                var financialItemId = ResolveFinancialItemId(
                    session.TrainingPlanItemId,
                    component.Type);
                if (!financialItemId.HasValue)
                {
                    continue;
                }

                var accumulator = GetAccumulator(financialItemId.Value);
                accumulator.GrossSpent += component.Amount;
                accumulator.Activities.Add(new TrainingBudgetActivityDto
                {
                    ActivityType = TravelPaymentActivity,
                    SourceId = session.Id,
                    TrainingCourseId = session.Id,
                    CourseNameAr = ResolveCourseName(session.TenantCourseId),
                    ActivityDate = activityDate,
                    SpentAmountOMR = component.Amount,
                    StatusCode = PaymentStatus.Confirmed.ToString()
                });
            }
        }

        foreach (var item in recoveryItems)
        {
            if (!recoveriesById.TryGetValue(item.TrainingExpenseRecoveryId, out var recovery))
            {
                continue;
            }

            var financialItemId = item.FinancialItemId;
            if (!financialItemId.HasValue
                && financialItemsByVoteCode.TryGetValue(item.FundingSourceVoteCode, out var mappedItem))
            {
                financialItemId = mappedItem.Id;
            }
            if (!financialItemId.HasValue)
            {
                continue;
            }

            casualCoursesById.TryGetValue(recovery.CasualCourseId, out var casualCourse);
            var accumulator = GetAccumulator(financialItemId.Value);
            accumulator.GrossSpent += item.AmountOMR;
            if (item.IsSettled)
            {
                accumulator.Recovered += item.AmountOMR;
            }
            else
            {
                accumulator.PendingRecovery += item.AmountOMR;
            }

            accumulator.Activities.Add(new TrainingBudgetActivityDto
            {
                ActivityType = CasualTravelExpenseActivity,
                SourceId = recovery.Id,
                TrainingCourseId = recovery.CasualCourseId,
                CourseNameAr = casualCourse == null
                    ? string.Empty
                    : ResolveCourseName(casualCourse.TenantCourseId),
                ActivityDate = recovery.ExpenseDate,
                SpentAmountOMR = item.AmountOMR,
                RecoveredAmountOMR = item.IsSettled ? item.AmountOMR : 0m,
                PendingRecoveryAmountOMR = item.IsSettled ? 0m : item.AmountOMR,
                StatusCode = item.IsSettled ? "Settled" : "PendingRecovery",
                Reference = item.SettlementReference
            });
        }

        var financialItemIdsWithValues = accumulators.Keys.ToHashSet();
        var candidateItems = financialItems
            .Where(x =>
                (!financialItemFilter.HasValue || x.Id == financialItemFilter.Value)
                && (x.ItemType.HasValue || financialItemIdsWithValues.Contains(x.Id))
                && (x.IsActive || financialItemIdsWithValues.Contains(x.Id)))
            .OrderBy(x => x.ParentId)
            .ThenBy(x => x.ItemType)
            .ThenBy(x => x.NameAr)
            .ToList();

        var result = new List<TrainingBudgetDto>(candidateItems.Count);
        foreach (var financialItem in candidateItems)
        {
            accumulators.TryGetValue(financialItem.Id, out var accumulator);
            accumulator ??= new BudgetAccumulator();
            budgetsByFinancialItem.TryGetValue(financialItem.Id, out var budget);

            var allocated = accumulator.Allocated;
            var grossSpent = accumulator.GrossSpent;
            var recovered = accumulator.Recovered;
            var netSpent = Math.Max(0m, grossSpent - recovered);
            var remaining = allocated - netSpent;
            var threshold = budget?.AlertThreshold ?? 80m;
            var spentPercent = allocated > 0m
                ? Math.Round(netSpent / allocated * 100m, 1)
                : netSpent > 0m ? 100m : 0m;

            var categoryName = financialItem.ParentId.HasValue
                && financialItemsById.TryGetValue(financialItem.ParentId.Value, out var parent)
                    ? parent.NameAr
                    : string.Empty;

            result.Add(new TrainingBudgetDto
            {
                Id = budget?.Id ?? Guid.Empty,
                Year = year,
                FinancialItemId = financialItem.Id,
                FinancialItemNameAr = financialItem.NameAr,
                FinancialItemNameEn = financialItem.NameEn,
                FinancialItemVoteCode = financialItem.VoteCode,
                FinancialItemType = financialItem.ItemType,
                BudgetCategoryNameAr = categoryName,
                TotalAmount = allocated,
                SpentAmount = netSpent,
                AllocatedAmountOMR = allocated,
                GrossSpentAmountOMR = grossSpent,
                RecoveredAmountOMR = recovered,
                AmountToRecoverOMR = accumulator.PendingRecovery,
                NetSpentAmountOMR = netSpent,
                Remaining = remaining,
                AlertThreshold = threshold,
                IsOverBudget = remaining < 0m,
                IsOverThreshold = netSpent > 0m
                    && (allocated <= 0m || spentPercent >= threshold),
                SpentPercent = spentPercent,
                IsFinancialItemActive = financialItem.IsActive,
                Activities = accumulator.Activities
                    .OrderByDescending(x => x.ActivityDate)
                    .ThenBy(x => x.ActivityType)
                    .ToList()
            });
        }

        return result;
    }

    private sealed class BudgetAccumulator
    {
        public decimal Allocated { get; set; }
        public decimal GrossSpent { get; set; }
        public decimal Recovered { get; set; }
        public decimal PendingRecovery { get; set; }
        public List<TrainingBudgetActivityDto> Activities { get; } = [];
    }
}
