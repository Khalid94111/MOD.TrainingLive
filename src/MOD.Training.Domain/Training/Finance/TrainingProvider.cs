using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Finance;

public class TrainingProvider : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public string ProviderNameAr { get; set; } = string.Empty;
    public string ProviderNameEn { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }

    // Phase 4B-α additions
    public bool IsFromNebras { get; set; }              // sync-driven; never user-editable
    public string? NebrasId { get; set; }               // sync-driven; never user-editable
    public ProviderScope Scope { get; set; }            // Staff sets at create time
    public Guid? CountryId { get; set; }                // FK to HrGeographicalLocations; required when Scope != Internal

    protected TrainingProvider() { }

    public TrainingProvider(Guid id, string providerNameAr, string providerNameEn) : base(id)
    {
        ProviderNameAr = providerNameAr;
        ProviderNameEn = providerNameEn;
        IsActive = true;
        Scope = ProviderScope.Local;
    }
}
