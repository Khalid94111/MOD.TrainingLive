using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Finance;

public class PlanItemFinancialItem : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid PlanItemId { get; set; }
    public Guid FinancialItemId { get; set; }
    public decimal EstimatedAmountOMR { get; set; }
    public decimal? EstimatedAmountUSD { get; set; }
    public decimal? ActualAmountOMR { get; set; }
    public decimal? ActualAmountUSD { get; set; }
    public string? Notes { get; set; }

    protected PlanItemFinancialItem() { }

    public PlanItemFinancialItem(
        Guid id,
        Guid planItemId,
        Guid financialItemId,
        decimal estimatedAmountOMR) : base(id)
    {
        PlanItemId = planItemId;
        FinancialItemId = financialItemId;
        EstimatedAmountOMR = estimatedAmountOMR;
    }
}
