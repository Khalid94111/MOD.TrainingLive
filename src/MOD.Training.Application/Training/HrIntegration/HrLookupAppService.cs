using MOD.Training.Training.Hr;
using MOD.Training.Training.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.HrIntegration;

/// <summary>
/// Provides read-only access to HR data (Employee, Rank) for GTMS.
/// Replaces the Phase 2B placeholder implementation.
/// Used by nomination forms, center role assignments, and employee lookups.
/// </summary>
public class HrLookupAppService(
    IRepository<Employee, Guid> employeeRepo,
    IRepository<Rank, Guid> rankRepo,
    EmployeeResolver employeeResolver)
    : ApplicationService, IHrLookupAppService
{
    /// <summary>
    /// Gets employees by OrgUnit — for nomination employee picker.
    /// Returns only active employees in the specified unit.
    /// UTM sees employees in their own unit.
    /// </summary>
    public async Task<List<EmployeeLookupDto>> GetEmployeesByUnitAsync(Guid unitId)
    {
        var employees = await employeeResolver.GetEmployeesByUnitAsync(unitId);
        return employees.Select(e => new EmployeeLookupDto
        {
            Id = e.Id,
            UserId = e.UserId,
            ServiceNumber = e.ServiceNumber,
            FullNameAr = e.FullNameAr,
            FullNameEn = e.FullNameEn,
            RankNameAr = e.Rank?.NameAr ?? "",
            RankNameEn = e.Rank?.NameEn ?? "",
            RankSortOrder = e.Rank?.SortOrder ?? 0,
            PersonnelType = e.Rank?.PersonnelType ?? "",
            MainUnitId = e.MainUnitId,
        }).OrderBy(e => e.RankSortOrder).ToList();
    }

    /// <summary>
    /// Search employee by service number within an OrgUnit.
    /// Used by center role assignment (MOD-20).
    /// </summary>
    public async Task<EmployeeLookupDto?> GetByServiceNumberAsync(string serviceNumber, Guid orgUnitId)
    {
        var queryable = await employeeRepo.WithDetailsAsync(x => x.Rank!);
        var employee = await employeeRepo.AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(x => x.ServiceNumber == serviceNumber && x.MainUnitId == orgUnitId && x.IsActive));

        if (employee == null) return null;

        return new EmployeeLookupDto
        {
            Id = employee.Id,
            UserId = employee.UserId,
            ServiceNumber = employee.ServiceNumber,
            FullNameAr = employee.FullNameAr,
            FullNameEn = employee.FullNameEn,
            RankNameAr = employee.Rank?.NameAr ?? "",
            RankNameEn = employee.Rank?.NameEn ?? "",
            RankSortOrder = employee.Rank?.SortOrder ?? 0,
            PersonnelType = employee.Rank?.PersonnelType ?? "",
            MainUnitId = employee.MainUnitId,
        };
    }

    /// <summary>
    /// Gets all ranks — for condition display and rank select dropdowns.
    /// </summary>
    public async Task<List<RankLookupDto>> GetAllRanksAsync()
    {
        var ranks = await employeeResolver.GetAllRanksAsync();
        return ranks.Select(r => new RankLookupDto
        {
            Id = r.Id,
            NameAr = r.NameAr,
            NameEn = r.NameEn,
            SortOrder = r.SortOrder,
            PersonnelType = r.PersonnelType,
        }).ToList();
    }

    /// <summary>
    /// Gets current logged-in user's employee info.
    /// Used by frontend to determine UTM's unit, display user profile.
    /// </summary>
    public async Task<EmployeeLookupDto?> GetCurrentEmployeeAsync()
    {
        var employee = await employeeResolver.GetCurrentEmployeeAsync();
        if (employee == null) return null;

        // Load rank
        var withRank = await employeeResolver.GetWithRankAsync(employee.Id);

        return new EmployeeLookupDto
        {
            Id = employee.Id,
            UserId = employee.UserId,
            ServiceNumber = employee.ServiceNumber,
            FullNameAr = employee.FullNameAr,
            FullNameEn = employee.FullNameEn,
            RankNameAr = withRank?.Rank?.NameAr ?? "",
            RankNameEn = withRank?.Rank?.NameEn ?? "",
            RankSortOrder = withRank?.Rank?.SortOrder ?? 0,
            PersonnelType = withRank?.Rank?.PersonnelType ?? "",
            MainUnitId = employee.MainUnitId,
        };
    }
}

// --- DTOs ---
public class EmployeeLookupDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ServiceNumber { get; set; } = string.Empty;
    public string FullNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;
    public string RankNameAr { get; set; } = string.Empty;
    public string RankNameEn { get; set; } = string.Empty;
    public int RankSortOrder { get; set; }
    public string PersonnelType { get; set; } = string.Empty;
    public Guid MainUnitId { get; set; }
}

public class RankLookupDto
{
    public Guid Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string PersonnelType { get; set; } = string.Empty;
}

public interface IHrLookupAppService
{
    Task<List<EmployeeLookupDto>> GetEmployeesByUnitAsync(Guid unitId);
    Task<EmployeeLookupDto?> GetByServiceNumberAsync(string serviceNumber, Guid orgUnitId);
    Task<List<RankLookupDto>> GetAllRanksAsync();
    Task<EmployeeLookupDto?> GetCurrentEmployeeAsync();
}
