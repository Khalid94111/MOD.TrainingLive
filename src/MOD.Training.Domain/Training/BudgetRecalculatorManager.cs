using MOD.Training.Training.Finance;
using MOD.Training.Training.Plans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training
{
    /// <summary>
    /// Recalculates TrainingBudget.TotalAmount after a plan is finally approved.
    ///
    /// Logic:
    /// 1. Get all PlanItemFinancialItems for the approved plan
    /// 2. Look up each child FinancialItem → find its ParentId
    /// 3. SUM amounts grouped by ParentId
    /// 4. UPDATE or CREATE TrainingBudget for (Year, ParentFinancialItemId)
    ///
    /// Called from TrainingPlanAppService.FinalApproveAsync()
    /// </summary>
    public class BudgetRecalculatorManager(
        IRepository<TrainingPlanItem, Guid> planItemRepo,
        IRepository<PlanItemFinancialItem, Guid> planItemFiRepo,
        IRepository<FinancialItem, Guid> financialItemRepo,
        IRepository<TrainingBudget, Guid> budgetRepo)
        : ITransientDependency
    {
        public async Task RecalculateAsync(Guid planId, int year)
        {
            // 1. Get all plan items for this plan
            var itemQueryable = await planItemRepo.GetQueryableAsync();
            var planItemIds = await planItemRepo.AsyncExecuter.ToListAsync(
                itemQueryable.Where(x => x.PlanId == planId).Select(x => x.Id));

            if (planItemIds.Count == 0) return;

            // 2. Get all financial item assignments for these plan items
            var fiQueryable = await planItemFiRepo.GetQueryableAsync();
            var assignments = await planItemFiRepo.AsyncExecuter.ToListAsync(
                fiQueryable.Where(x => planItemIds.Contains(x.PlanItemId)));

            if (assignments.Count == 0) return;

            // 3. Load all financial items to resolve child → parent
            var allFiIds = assignments.Select(a => a.FinancialItemId).Distinct().ToList();
            var allFiQueryable = await financialItemRepo.GetQueryableAsync();
            var financialItems = await financialItemRepo.AsyncExecuter.ToListAsync(
                allFiQueryable.Where(fi => allFiIds.Contains(fi.Id)));

            var fiLookup = financialItems.ToDictionary(fi => fi.Id);

            // 4. Group amounts by parent financial item
            var parentTotals = new Dictionary<Guid, decimal>();

            foreach (var assignment in assignments)
            {
                if (!fiLookup.TryGetValue(assignment.FinancialItemId, out var fi)) continue;

                // Use ParentId if it's a child, otherwise it IS the parent
                var parentId = fi.ParentId ?? fi.Id;

                if (!parentTotals.ContainsKey(parentId))
                    parentTotals[parentId] = 0;

                parentTotals[parentId] += assignment.EstimatedAmountOMR;
            }

            // 5. Update or create budget records for each parent
            foreach (var (parentFiId, total) in parentTotals)
            {
                var budgetQueryable = await budgetRepo.GetQueryableAsync();
                var budget = await budgetRepo.AsyncExecuter.FirstOrDefaultAsync(
                    budgetQueryable.Where(b => b.Year == year && b.FinancialItemId == parentFiId));

                if (budget != null)
                {
                    budget.TotalAmount = total;
                    await budgetRepo.UpdateAsync(budget, autoSave: true);
                }
                else
                {
                    // Budget doesn't exist yet for this parent+year — create it
                    await budgetRepo.InsertAsync(new TrainingBudget(Guid.NewGuid())
                    {
                        Year = year,
                        FinancialItemId = parentFiId,
                        TotalAmount = total,
                        SpentAmount = 0,
                        AlertThreshold = 80,
                    }, autoSave: true);
                }
            }
        }
    }
}
 