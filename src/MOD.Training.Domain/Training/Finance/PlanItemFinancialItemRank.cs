using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Finance;

public class PlanItemFinancialItemRank : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid PlanItemFinancialItemId { get; set; }
    public Guid RankId { get; set; }
    public int NomineeCount { get; set; }
    public decimal RatePerUnitOMR { get; set; }
    public decimal SubtotalOMR { get; set; }

    protected PlanItemFinancialItemRank() { }

    public PlanItemFinancialItemRank(
        Guid id, Guid pifiId, Guid rankId,
        int nomineeCount, decimal ratePerUnit, decimal subtotal) : base(id)
    {
        PlanItemFinancialItemId = pifiId;
        RankId = rankId;
        NomineeCount = nomineeCount;
        RatePerUnitOMR = ratePerUnit;
        SubtotalOMR = subtotal;
    }
}
