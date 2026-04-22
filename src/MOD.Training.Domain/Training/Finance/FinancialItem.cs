using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Finance;

public class FinancialItem : FullAuditedEntity<Guid>, IMultiTenant
{
    public FinancialItem(Guid id) : base(id)
    {
        
    }
    public FinancialItem()
    {
        
    }
    public Guid? TenantId { get; set; }
    public Guid? ParentId { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string VoteCode { get; set; } = default!;
    public bool IsGeneral { get; set; }
    public bool IsActive { get; set; } = true;

    // CHG-02 + CHG-07
    public decimal DefaultAmountOMR { get; set; }
    public bool IsPerDay { get; set; }
    public bool IsPerNominee { get; set; }
    public int ExtraDaysBefore { get; set; }
    public int ExtraDaysAfter { get; set; }

    // Navigation
    public FinancialItem? Parent { get; set; }
}
