using System;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace MOD.Training.Training.Finance;

/// <summary>
/// Recalculates TrainingBudgets from approved annual plan items (MOD-23).
/// Plan entities (TrainingPlan, TrainingPlanItem, PlanItemFinancialItem) arrive in Phase 3 —
/// the body below will be uncommented and wired to plan approval workflows then.
/// </summary>
public class BudgetRecalculationService(
    IRepository<TrainingBudget, Guid> budgetRepository,
    IRepository<FinancialItem, Guid> financialItemRepository)
    : DomainService, ITransientDependency
{
    public Task RecalculateAsync(Guid tenantId, int year)
    {
        // Phase 3 implementation (pseudocode — enable once TrainingPlan / PlanItemFinancialItem exist):
        //
        // var approvedPlanItems = await planItemRepository.GetListAsync(
        //     x => x.Plan.TenantId == tenantId
        //       && x.Plan.Year == year
        //       && x.Plan.Status == PlanStatus.THApproved);
        //
        // var planItemIds = approvedPlanItems.Select(x => x.Id).ToList();
        //
        // var allFinancials = await planItemFinancialRepository.GetListAsync(
        //     x => planItemIds.Contains(x.PlanItemId));
        //
        // var fiIds = allFinancials.Select(x => x.FinancialItemId).Distinct().ToList();
        // var financialItems = await financialItemRepository.GetListAsync(
        //     x => fiIds.Contains(x.Id));
        // var fiMap = financialItems.ToDictionary(x => x.Id);
        //
        // var grouped = allFinancials
        //     .Where(x => fiMap.ContainsKey(x.FinancialItemId))
        //     .GroupBy(x => fiMap[x.FinancialItemId].ParentId ?? fiMap[x.FinancialItemId].Id)
        //     .ToDictionary(g => g.Key, g => g.Sum(x => x.EstimatedAmount));
        //
        // foreach (var (parentFiId, total) in grouped)
        // {
        //     var budget = await budgetRepository.FindAsync(
        //         x => x.TenantId == tenantId
        //           && x.Year == year
        //           && x.FinancialItemId == parentFiId);
        //
        //     if (budget == null)
        //     {
        //         budget = new TrainingBudget
        //         {
        //             TenantId = tenantId,
        //             Year = year,
        //             FinancialItemId = parentFiId,
        //             TotalAmount = total,
        //             SpentAmount = 0,
        //             AlertThreshold = 80
        //         };
        //         await budgetRepository.InsertAsync(budget, autoSave: true);
        //     }
        //     else
        //     {
        //         budget.TotalAmount = total;
        //         await budgetRepository.UpdateAsync(budget, autoSave: true);
        //     }
        // }
        //
        // var existingBudgets = await budgetRepository.GetListAsync(
        //     x => x.TenantId == tenantId && x.Year == year);
        //
        // foreach (var budget in existingBudgets)
        // {
        //     if (!grouped.ContainsKey(budget.FinancialItemId))
        //     {
        //         budget.TotalAmount = 0;
        //         await budgetRepository.UpdateAsync(budget, autoSave: true);
        //     }
        // }

        return Task.CompletedTask;
    }
}
