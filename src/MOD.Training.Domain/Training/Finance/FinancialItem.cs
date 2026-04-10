using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Finance;

public class FinancialItem : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid? ParentId { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string VoteCode { get; set; } = default!;
    public bool IsGeneral { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public FinancialItem? Parent { get; set; }
}
