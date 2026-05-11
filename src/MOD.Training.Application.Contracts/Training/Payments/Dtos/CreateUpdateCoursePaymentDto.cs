using System;

namespace MOD.Training.Training.Payments.Dtos;

/// <summary>
/// Payload for creating or updating a CoursePayment in Draft state. Invoice file refs are set
/// only via <c>UploadInvoiceAsync</c>; lifecycle fields (Status / ConfirmedAt / ConfirmedById)
/// are server-controlled and intentionally absent.
/// </summary>
public class CreateUpdateCoursePaymentDto
{
    // Polymorphic parent — exactly one must be set.
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }

    public Guid TrainingProviderId { get; set; }

    public decimal InvoiceAmountOMR { get; set; }
    public decimal NebrasAmountOMR { get; set; }
    public DateTime InvoiceDate { get; set; }

    public string? Notes { get; set; }
}
