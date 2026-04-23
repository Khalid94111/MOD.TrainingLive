using MOD.Training.Training.CasualCourses;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace MOD.Training.Training.Managers;

/// <summary>
/// Mirrors PlanItemUnitScope for casual courses. UTM/UGM are unit-scoped; Staff/TD/TH see the tenant.
/// </summary>
public class CasualCourseUnitScope(
    ICurrentUser currentUser,
    EmployeeResolver employeeResolver,
    IRepository<CasualCourse, Guid> casualCourseRepository)
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

    public async Task<IQueryable<CasualCourse>> ApplyScopeAsync(IQueryable<CasualCourse> queryable)
    {
        if (!await IsCurrentUserUnitScopedAsync()) return queryable;

        var unitId = await GetCurrentUserUnitIdAsync();
        return unitId.HasValue
            ? queryable.Where(x => x.UnitId == unitId.Value)
            : queryable.Where(_ => false);
    }

    public async Task EnsureCanAccessAsync(CasualCourse casualCourse)
    {
        if (!await IsCurrentUserUnitScopedAsync()) return;

        var unitId = await GetCurrentUserUnitIdAsync();
        if (!unitId.HasValue || casualCourse.UnitId != unitId.Value)
            throw new AbpAuthorizationException(
                "You are not authorized to access casual courses from other units.");
    }

    public async Task EnsureCanAccessCasualCourseAsync(Guid casualCourseId)
    {
        if (!await IsCurrentUserUnitScopedAsync()) return;

        var cc = await casualCourseRepository.GetAsync(casualCourseId);
        await EnsureCanAccessAsync(cc);
    }
}
