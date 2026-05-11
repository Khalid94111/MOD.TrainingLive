using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Payments;

/// <summary>
/// Course-level invoice for a casual course OR an annual-plan session.
/// Polymorphic parent: exactly one of CasualCourseId / SessionId is set (DB CHECK enforces).
/// On Confirm, casual-course payments fire the BudgetReallocationGenerator (Phase 4B-β).
/// Invoice file is stored via BlobStoring.FileSystem in the CourseInvoiceContainer.
/// </summary>
public class CoursePayment : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    // Polymorphic parent — exactly one is set (DB CHECK constraint enforced).
    public Guid? SessionId { get; set; }
    public Guid? CasualCourseId { get; set; }

    public Guid TrainingProviderId { get; set; }       // FK — must match SelectedPriceQuote.ProviderId on casual arm

    public decimal InvoiceAmountOMR { get; set; }      // what provider charged
    public decimal NebrasAmountOMR { get; set; }       // what Nebras paid out
    public DateTime InvoiceDate { get; set; }

    // BlobStoring file refs — written by UploadInvoiceAsync, never via Create/Update DTO.
    public string? InvoiceBlobName { get; set; }          // unique blob ID inside the CourseInvoiceContainer
    public string? InvoiceOriginalFileName { get; set; }  // preserved for download UX

    public PaymentStatus Status { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedById { get; set; }

    public string? Notes { get; set; }

    protected CoursePayment() { }

    public CoursePayment(Guid id) : base(id)
    {
        Status = PaymentStatus.Draft;
    }
}
