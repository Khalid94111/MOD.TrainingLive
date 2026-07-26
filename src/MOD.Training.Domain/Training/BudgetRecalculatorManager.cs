using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Plans;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training;

/// <summary>
/// Rebuilds the allocated budget for a year from every finally-approved plan.
/// The financial item itself is the accounting line; the deprecated ParentId
/// hierarchy must not collapse ticket, visa, insurance, and allowance budgets.
/// </summary>
public class BudgetRecalculatorManager(
    IRepository<TrainingPlan, Guid> planRepo,
    IRepository<TrainingPlanItem, Guid> planItemRepo,
    IRepository<PlanItemFinancialItem, Guid> planItemFinancialRepo,
    IRepository<TrainingBudget, Guid> budgetRepo)
    : ITransientDependency
{
    public async Task RecalculateAsync(Guid planId, int year)
    {
        var planQueryable = await planRepo.GetQueryableAsync();
        var approvedPlanIds = await planRepo.AsyncExecuter.ToListAsync(
            planQueryable
                .Where(x => x.Year == year && x.Status == PlanStatus.THApproved)
                .Select(x => x.Id));

        var itemQueryable = await planItemRepo.GetQueryableAsync();
        var planItemIds = approvedPlanIds.Count == 0
            ? []
            : await planItemRepo.AsyncExecuter.ToListAsync(
                itemQueryable
                    .Where(x => approvedPlanIds.Contains(x.PlanId))
                    .Select(x => x.Id));

        var financialQueryable = await planItemFinancialRepo.GetQueryableAsync();
        var assignments = planItemIds.Count == 0
            ? []
            : await planItemFinancialRepo.AsyncExecuter.ToListAsync(
                financialQueryable.Where(x => planItemIds.Contains(x.PlanItemId)));
        var totals = assignments
            .GroupBy(x => x.FinancialItemId)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.EstimatedAmountOMR));

        var existingQueryable = await budgetRepo.GetQueryableAsync();
        var existingBudgets = await budgetRepo.AsyncExecuter.ToListAsync(
            existingQueryable.Where(x => x.Year == year));
        var existingByFinancialItem = existingBudgets
            .GroupBy(x => x.FinancialItemId)
            .ToDictionary(x => x.Key, x => x.Last());

        foreach (var (financialItemId, total) in totals)
        {
            if (existingByFinancialItem.TryGetValue(financialItemId, out var budget))
            {
                budget.TotalAmount = total;
                await budgetRepo.UpdateAsync(budget, autoSave: true);
                continue;
            }

            await budgetRepo.InsertAsync(new TrainingBudget(Guid.NewGuid())
            {
                Year = year,
                FinancialItemId = financialItemId,
                TotalAmount = total,
                SpentAmount = 0m,
                AlertThreshold = 80m
            }, autoSave: true);
        }

        foreach (var staleBudget in existingBudgets.Where(x =>
                     !totals.ContainsKey(x.FinancialItemId) && x.TotalAmount != 0m))
        {
            staleBudget.TotalAmount = 0m;
            await budgetRepo.UpdateAsync(staleBudget, autoSave: true);
        }
    }
}
