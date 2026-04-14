using MOD.Training.Training.Finance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
 
namespace MOD.Training.Training.Managers;

/// <summary>
/// Calculates estimated total cost for plan items from their assigned financial items.
/// Replaces the stored EstimatedCost field — cost is always computed at query time.
/// Used by GetListAsync in TrainingPlanItemAppService and by the cost gate validator.
/// </summary>
public class PlanItemCostCalculator(
    IRepository<PlanItemFinancialItem, Guid> financialItemRepo)
    : ITransientDependency
{
    /// <summary>
    /// Gets the total estimated cost (OMR) for a single plan item.
    /// </summary>
    public async Task<decimal> GetEstimatedCostAsync(Guid planItemId)
    {
        var queryable = await financialItemRepo.GetQueryableAsync();
        var items = await financialItemRepo.AsyncExecuter.ToListAsync(
            queryable.Where(x => x.PlanItemId == planItemId));

        return items.Sum(x => x.EstimatedAmountOMR);
    }

    /// <summary>
    /// Batch-loads estimated costs for multiple plan items.
    /// Returns Dictionary&lt;PlanItemId, TotalEstimatedCostOMR&gt;.
    /// </summary>
    public async Task<Dictionary<Guid, decimal>> BatchGetEstimatedCostsAsync(List<Guid> planItemIds)
    {
        if (planItemIds.Count == 0) return new();

        var queryable = await financialItemRepo.GetQueryableAsync();
        var items = await financialItemRepo.AsyncExecuter.ToListAsync(
            queryable.Where(x => planItemIds.Contains(x.PlanItemId)));

        return items
            .GroupBy(x => x.PlanItemId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.EstimatedAmountOMR));
    }

    /// <summary>
    /// Checks if all external plan items in a plan have financial items with amounts > 0.
    /// Used by the cost gate (MOD-13).
    /// Returns list of PlanItemIds that are missing cost.
    /// </summary>
    public async Task<List<Guid>> GetItemsMissingCostAsync(
        List<Guid> externalPlanItemIds)
    {
        if (externalPlanItemIds.Count == 0) return new();

        var costs = await BatchGetEstimatedCostsAsync(externalPlanItemIds);

        return externalPlanItemIds
            .Where(id => !costs.ContainsKey(id) || costs[id] <= 0)
            .ToList();
    }
}
