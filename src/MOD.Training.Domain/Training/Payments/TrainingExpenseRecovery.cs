using System;
using System.Collections.Generic;
using MOD.Training.Training.Enums;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Payments;

/// <summary>
/// One audit record for the Travel expenses paid from Training financial items for a casual course.
/// It is imported from Travel only when the funding source covers the course fee and Training
/// covers the remaining travel expenses.
/// </summary>
public class TrainingExpenseRecovery : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid CasualCourseId { get; set; }
    public Guid TravelRequestId { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string Currency { get; set; } = "OMR";
    public decimal TotalAmountOMR { get; set; }
    public TrainingExpenseRecoveryStatus Status { get; set; }

    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedById { get; set; }
    public string? ReviewNote { get; set; }

    public ICollection<TrainingExpenseRecoveryItem> Items { get; set; } = [];

    protected TrainingExpenseRecovery() { }

    public TrainingExpenseRecovery(Guid id) : base(id)
    {
        Status = TrainingExpenseRecoveryStatus.PendingReview;
    }
}
