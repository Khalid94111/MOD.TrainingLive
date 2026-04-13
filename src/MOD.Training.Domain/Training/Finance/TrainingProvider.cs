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

    protected TrainingProvider() { }

    public TrainingProvider(Guid id, string providerNameAr, string providerNameEn) : base(id)
    {
        ProviderNameAr = providerNameAr;
        ProviderNameEn = providerNameEn;
        IsActive = true;
    }
}
