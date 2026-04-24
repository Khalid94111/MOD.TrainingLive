using MOD.Training.Training.Hr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;

namespace MOD.Training.Training.Managers;

/// <summary>
/// Resolves the current user's Employee record from HR shared DbContext.
/// Used across AppServices for:
/// - UnitId auto-detection (Employee.MainUnitId)
/// - Condition validation (Rank, Age, ServiceYears, etc.)
/// - Employee name resolution for DTOs
/// </summary>
public class EmployeeResolver(
    IRepository<Employee, Guid> employeeRepository,
    IRepository<Rank, Guid> rankRepository,
    ICurrentUser currentUser)
    : ITransientDependency
{
    /// <summary>
    /// Gets the Employee record for the currently logged-in user.
    /// </summary>
    public async Task<Employee?> GetCurrentEmployeeAsync()
    {
        if (currentUser.Id == null) return null;

        var queryable = await employeeRepository.GetQueryableAsync();
        return await employeeRepository.FindAsync(x => x.UserId == currentUser.Id.Value);
    }

    /// <summary>
    /// Gets the MainUnitId for the currently logged-in user.
    /// Used for auto-setting TrainingPlanItem.UnitId.
    /// </summary>
    public async Task<Guid?> GetCurrentUserUnitIdAsync()
    {
        var employee = await GetCurrentEmployeeAsync();
        return employee?.MainUnitId;
    }

    /// <summary>
    /// Gets Employee record by UserId.
    /// </summary>
    public async Task<Employee?> GetByUserIdAsync(Guid userId)
    {
        return await employeeRepository.FindAsync(x => x.UserId == userId);
    }

    /// <summary>
    /// Gets Employee record by EmployeeId (the Employee table PK).
    /// </summary>
    public async Task<Employee?> GetByIdAsync(Guid employeeId)
    {
        var queryable = await employeeRepository.WithDetailsAsync(x => x.Rank!);
        return (await employeeRepository.AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(x => x.Id == employeeId)));
    }

    /// <summary>
    /// Gets Employee with Rank loaded.
    /// </summary>
    public async Task<Employee?> GetWithRankAsync(Guid employeeId)
    {
        var queryable = await employeeRepository.WithDetailsAsync(x => x.Rank!);
        return await employeeRepository.AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(x => x.Id == employeeId));
    }

    /// <summary>
    /// Gets all active employees in a given OrgUnit (for nomination employee picker).
    /// </summary>
    public async Task<List<Employee>> GetEmployeesByUnitAsync(Guid unitId, Guid tenantId)
    {
        var queryable = await employeeRepository.WithDetailsAsync(x => x.Rank!);
        return await employeeRepository.AsyncExecuter.ToListAsync(
            queryable.Where(x => x.MainUnitId == unitId && x.IsActive && x.TenantId==tenantId ));
    }

    /// <summary>
    /// Batch-loads employee names for a list of user IDs.
    /// Returns Dictionary&lt;UserId, Employee&gt;.
    /// </summary>
    public async Task< Dictionary<Guid, Employee>> BatchResolveByUserIdsAsync(
        List<Guid> userIds)
    {
        var queryable = await employeeRepository.WithDetailsAsync(x => x.Rank!);
        var employees = await employeeRepository.AsyncExecuter.ToListAsync(
            queryable.Where(x => userIds.Contains(x.UserId)));
        return employees.ToDictionary(x => x.UserId);
    }

    /// <summary>
    /// Batch-loads employee names for a list of employee IDs.
    /// Returns Dictionary&lt;EmployeeId, Employee&gt;.
    /// </summary>
    public async Task< Dictionary<Guid, Employee>> BatchResolveByIdsAsync(
        List<Guid> employeeIds)
    {
        var queryable = await employeeRepository.WithDetailsAsync(x => x.Rank!);
        var employees = await employeeRepository.AsyncExecuter.ToListAsync(
            queryable.Where(x => employeeIds.Contains(x.Id)));
        return employees.ToDictionary(x => x.Id);
    }

    /// <summary>
    /// Gets all ranks ordered by SortOrder.
    /// </summary>
    public async Task<List<Rank>> GetAllRanksAsync()
    {
        var queryable = await rankRepository.GetQueryableAsync();
        return await rankRepository.AsyncExecuter.ToListAsync(
            queryable.Where(x => x.IsActive).OrderBy(x => x.SortOrder));
    }

    /// <summary>
    /// Checks whether the current user is in any of the given role names.
    /// Reads from the ICurrentUser.Roles claim (token) — no DB hit.
    /// </summary>
    public Task<bool> IsCallerInRolesAsync(params string[] roleNames)
    {
        if (roleNames == null || roleNames.Length == 0) return Task.FromResult(false);
        var roles = currentUser.Roles ?? Array.Empty<string>();
        return Task.FromResult(roles.Any(r => roleNames.Contains(r)));
    }

    /// <summary>
    /// Batch-loads employees with their rank for a list of employee IDs.
    /// Used by estimate preview to group by rank.
    /// </summary>
    public async Task<List<EmployeeWithRankInfo>> GetEmployeesWithRanksAsync(List<Guid> employeeIds)
    {
        if (employeeIds == null || employeeIds.Count == 0)
            return new List<EmployeeWithRankInfo>();

        var queryable = await employeeRepository.WithDetailsAsync(x => x.Rank!);
        var employees = await employeeRepository.AsyncExecuter.ToListAsync(
            queryable.Where(x => employeeIds.Contains(x.Id) && x.IsActive));

        return employees.Select(e => new EmployeeWithRankInfo
        {
            EmployeeId = e.Id,
            RankId = e.RankId,
            RankNameAr = e.Rank?.NameAr ?? string.Empty,
            FullNameAr = e.FullNameAr,
        }).ToList();
    }
}

public class EmployeeWithRankInfo
{
    public Guid EmployeeId { get; set; }
    public Guid RankId { get; set; }
    public string RankNameAr { get; set; } = string.Empty;
    public string FullNameAr { get; set; } = string.Empty;
}
