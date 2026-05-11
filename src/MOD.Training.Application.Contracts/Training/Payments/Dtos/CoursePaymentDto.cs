using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Payments.Dtos;

public class CoursePaymentDto : EntityDto<Guid>
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid TrainingProviderId { get; set; }

    // Joined display fields (populated by AppService enrichment).
    public string? TrainingProviderName { get; set; }
    public string? CourseNameAr { get; set; }
    public FundingScenario? FundingScenario { get; set; }   // joined from CasualCourse; null on session arm

    public decimal InvoiceAmountOMR { get; set; }
    public decimal NebrasAmountOMR { get; set; }
    public decimal VarianceOMR { get; set; }                // computed: NebrasAmountOMR - InvoiceAmountOMR
    public DateTime InvoiceDate { get; set; }

    public string? InvoiceBlobName { get; set; }
    public string? InvoiceOriginalFileName { get; set; }
    public bool HasInvoice { get; set; }                    // server-set: !string.IsNullOrEmpty(InvoiceBlobName)

    public PaymentStatus Status { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedById { get; set; }
    public string? ConfirmedByName { get; set; }

    public string? Notes { get; set; }
    public DateTime CreationTime { get; set; }
}
