using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Payments.Dtos;

public class CoursePaymentGetListInput : PagedAndSortedResultRequestDto
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? TrainingProviderId { get; set; }
    public PaymentStatus? Status { get; set; }
    public DateTime? InvoiceDateFrom { get; set; }
    public DateTime? InvoiceDateTo { get; set; }
}
