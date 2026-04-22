using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Finance;

public class FinancialItemRankAmount : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid FinancialItemId { get; set; }
    public Guid RankId { get; set; }
    public decimal AmountOMR { get; set; }

    protected FinancialItemRankAmount() { }

    public FinancialItemRankAmount(Guid id, Guid financialItemId, Guid rankId, decimal amountOMR) : base(id)
    {
        FinancialItemId = financialItemId;
        RankId = rankId;
        AmountOMR = amountOMR;
    }
}
