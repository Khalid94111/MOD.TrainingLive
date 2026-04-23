using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.CasualCourses;

public class CasualCourseFinancial : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid CasualCourseId { get; set; }
    public Guid FinancialItemId { get; set; }

    public decimal EstimatedAmountOMR { get; set; }
    public decimal? ActualAmountOMR { get; set; }

    public FinancialAmountSource Source { get; set; }
    public string? Notes { get; set; }

    protected CasualCourseFinancial() { }

    public CasualCourseFinancial(
        Guid id,
        Guid casualCourseId,
        Guid financialItemId,
        decimal estimatedAmount,
        FinancialAmountSource source) : base(id)
    {
        CasualCourseId = casualCourseId;
        FinancialItemId = financialItemId;
        EstimatedAmountOMR = estimatedAmount;
        Source = source;
    }
}
