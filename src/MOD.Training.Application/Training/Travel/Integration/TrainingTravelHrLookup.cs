using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MOD.Training.Training.Hr;
using Travel.Allowances;
using Travel.Ranks;
using Travel.TravelRequests;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Travel.Integration;

/// <summary>
/// Exposes Training's HR read model to the embedded Travel module.
/// No employee or rank data is copied into Travel; lookups always use the authoritative HR tables.
/// </summary>
public class TrainingTravelHrLookup(
    IRepository<Employee, Guid> employeeRepository,
    IRepository<Rank, Guid> rankRepository,
    IOrganizationUnitRepository organizationUnitRepository,
    ICurrentTenant currentTenant)
    : ITravelEmployeeLookup, IRankLookup, ITransientDependency
{
    public async Task<List<TravelEmployeeLookupResult>> FindByEmployeeNumbersAsync(
        IEnumerable<string> employeeNumbers)
    {
        var requestedNumbers = employeeNumbers
            .Where(number => !string.IsNullOrWhiteSpace(number))
            .Select(number => number.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (requestedNumbers.Count == 0)
        {
            return [];
        }

        var employeeQuery = await employeeRepository.GetQueryableAsync();
        employeeQuery = employeeQuery.Where(employee =>
            employee.IsActive && requestedNumbers.Contains(employee.ServiceNumber));
        if (currentTenant.Id.HasValue)
        {
            employeeQuery = employeeQuery.Where(employee => employee.TenantId == currentTenant.Id);
        }

        var employees = await employeeRepository.AsyncExecuter.ToListAsync(employeeQuery);
        var rankIds = employees.Select(employee => employee.RankId).Distinct().ToList();
        var rankQuery = await rankRepository.GetQueryableAsync();
        var ranks = rankIds.Count == 0
            ? []
            : await rankRepository.AsyncExecuter.ToListAsync(
                rankQuery.Where(rank => rankIds.Contains(rank.Id)));
        var ranksById = ranks.ToDictionary(rank => rank.Id);

        var unitsById = new Dictionary<Guid, string>();
        foreach (var unitId in employees.Select(employee => employee.MainUnitId).Distinct())
        {
            var unit = await organizationUnitRepository.FindAsync(unitId);
            if (unit != null)
            {
                unitsById[unitId] = unit.DisplayName;
            }
        }

        return employees.Select(employee =>
        {
            ranksById.TryGetValue(employee.RankId, out var rank);
            return new TravelEmployeeLookupResult
            {
                Id = employee.Id,
                Name = employee.FullNameAr,
                EmployeeNumber = employee.ServiceNumber.Trim(),
                Department = unitsById.GetValueOrDefault(employee.MainUnitId) ?? string.Empty,
                RankId = employee.RankId,
                RankName = rank?.NameAr ?? string.Empty,
                Category = ResolveAllowanceCategory(rank)
            };
        }).ToList();
    }

    public async Task<List<RankLookupDto>> GetRanksAsync()
    {
        var query = await rankRepository.GetQueryableAsync();
        var ranks = await rankRepository.AsyncExecuter.ToListAsync(
            query.Where(rank => rank.IsActive).OrderBy(rank => rank.SortOrder));

        return ranks.Select(MapRank).ToList();
    }

    public async Task<RankLookupDto?> GetRankByIdAsync(Guid id)
    {
        var rank = await rankRepository.FindAsync(id);
        return rank is { IsActive: true } ? MapRank(rank) : null;
    }

    private static RankLookupDto MapRank(Rank rank)
        => new()
        {
            Id = rank.Id,
            Name = rank.NameAr,
            Category = ResolveAllowanceCategory(rank),
            Level = rank.SortOrder
        };

    private static AllowanceCategory ResolveAllowanceCategory(Rank? rank)
        => string.Equals(rank?.PersonnelType, "Officer", StringComparison.OrdinalIgnoreCase)
            ? AllowanceCategory.A
            : AllowanceCategory.B;
}
