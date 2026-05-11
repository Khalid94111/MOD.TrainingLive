using System;
using Volo.Abp.Domain.Entities;

namespace MOD.Training.Training.Hr;

/// <summary>
/// Read-only HR master-data reference. Hierarchical: countries have LocationParentId = null,
/// cities have LocationParentId = their country's Id. Owned by the HR module conceptually,
/// but lives in this DbContext (table HrGeographicalLocations) to match the existing
/// HrEmployees / HrRanks pattern in this repo.
/// </summary>
public class GeographicalLocation : Entity<Guid>
{
    public GeographicalLocation(Guid id) : base(id) { }

    public string ArabicName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public Guid? LocationParentId { get; set; }
}
