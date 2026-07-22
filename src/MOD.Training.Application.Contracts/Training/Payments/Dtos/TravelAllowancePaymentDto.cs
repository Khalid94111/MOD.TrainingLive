using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Payments.Dtos;

public class TravelAllowancePaymentDto : EntityDto<Guid>
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid NominationId { get; set; }

    // Joined display fields populated by AppService enrichment.
    public string? EmployeeNameAr { get; set; }
    public string? RankNameAr { get; set; }
    public PersonnelType PersonnelType { get; set; }
    public string? CourseNameAr { get; set; }
    public int EffectiveTravelDays { get; set; }   // resolved from the completed Travel request

    public decimal TicketAmountOMR { get; set; }
    public decimal TravelAllowanceOMR { get; set; }
    public decimal ClothingAllowanceOMR { get; set; }
    public decimal InsuranceOMR { get; set; }
    public decimal VisaFeesOMR { get; set; }
    public decimal TotalOMR { get; set; }

    public PaymentStatus Status { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedById { get; set; }
    public string? ConfirmedByName { get; set; }

    public string? ExternalRequestId { get; set; }
    public string? ExternalStatus { get; set; }
    public DateTime? ExternalResponseAt { get; set; }

    public string? Notes { get; set; }
    public DateTime CreationTime { get; set; }
}
