using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Payments;

public class TrainingExpenseRecoveryItem : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid TrainingExpenseRecoveryId { get; set; }
    public Guid? FinancialItemId { get; set; }
    public string ExpenseTypeCode { get; set; } = string.Empty;
    public string FundingSourceVoteCode { get; set; } = string.Empty;
    public decimal AmountOMR { get; set; }
    public bool IsSettled { get; set; }
    public DateTime? SettledAt { get; set; }
    public Guid? SettledById { get; set; }
    public string? SettlementReference { get; set; }
    public string? SettlementNote { get; set; }

    protected TrainingExpenseRecoveryItem() { }

    public TrainingExpenseRecoveryItem(Guid id) : base(id) { }
}
