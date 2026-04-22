using System;
using System.Linq;
using System.Threading.Tasks;
using MOD.Training.Training.Plans;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace MOD.Training.Training.Managers;

/// <summary>
/// Centralizes the "is the current user scoped to their own unit?" decision and
/// provides helpers to apply the UnitId filter consistently on plan item queries.
///
/// UTM (UnitTrainingManager) and UGM (UnitGeneralManager) may only see/act on plan
/// items whose <see cref="TrainingPlanItem.UnitId"/> matches their employee record's
/// MainUnitId. TH (TenantHead), TD (TrainingDirector) and Training Directorate Staff
/// are unscoped and see everything in the tenant.
/// </summary>
public class PlanItemUnitScope(
    ICurrentUser currentUser,
    EmployeeResolver employeeResolver,
    IRepository<TrainingPlanItem, Guid> planItemRepository)
    : ITransientDependency
{
    public static readonly string[] UnitScopedRoles =
    {
        "UnitTrainingManager",
        "UnitGeneralManager",
    };

    public Task<bool> IsCurrentUserUnitScopedAsync()
    {
        var roles = currentUser.Roles ?? Array.Empty<string>();
        return Task.FromResult(roles.Any(r => UnitScopedRoles.Contains(r)));
    }

    public Task<Guid?> GetCurrentUserUnitIdAsync()
        => employeeResolver.GetCurrentUserUnitIdAsync();

    /// <summary>
    /// Narrows a plan-item queryable to the current user's unit when they are unit-scoped.
    /// Returns the unchanged queryable for unscoped roles (TH/TD/Staff).
    /// A unit-scoped user without an employee record sees nothing (intentional fail-closed).
    /// </summary>
    public async Task<IQueryable<TrainingPlanItem>> ApplyScopeAsync(IQueryable<TrainingPlanItem> queryable)
    {
        if (!await IsCurrentUserUnitScopedAsync()) return queryable;

        var unitId = await GetCurrentUserUnitIdAsync();
        return unitId.HasValue
            ? queryable.Where(x => x.UnitId == unitId.Value)
            : queryable.Where(_ => false);
    }

    /// <summary>
    /// Throws <see cref="AbpAuthorizationException"/> when a unit-scoped user targets a
    /// plan item outside their own unit. No-op for unscoped roles.
    /// </summary>
    public async Task EnsureCanAccessAsync(TrainingPlanItem planItem)
    {
        if (!await IsCurrentUserUnitScopedAsync()) return;

        var unitId = await GetCurrentUserUnitIdAsync();
        if (!unitId.HasValue || planItem.UnitId != unitId.Value)
            throw new AbpAuthorizationException(
                "You are not authorized to access plan items from other units.");
    }

    /// <summary>
    /// Loads the plan item by id and runs the unit-scope check. Used by services that
    /// work with child entities (PIFIs, Nominations) and only have the plan-item id in hand.
    /// </summary>
    public async Task EnsureCanAccessPlanItemAsync(Guid planItemId)
    {
        if (!await IsCurrentUserUnitScopedAsync()) return;

        var planItem = await planItemRepository.GetAsync(planItemId);
        await EnsureCanAccessAsync(planItem);
    }
}
