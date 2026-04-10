using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Finance;

public class TrainingBudget : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public int Year { get; set; }
    public BudgetType BudgetType { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal AlertThreshold { get; set; } = 80m;
}
