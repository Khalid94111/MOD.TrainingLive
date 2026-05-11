using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Payments.Dtos;

public class TravelAllowancePaymentGetListInput : PagedAndSortedResultRequestDto
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? NominationId { get; set; }
    public PaymentStatus? Status { get; set; }
    public PersonnelType? PersonnelType { get; set; }

    /// <summary>Free-text search applied to enriched <c>EmployeeNameAr</c>.</summary>
    public string? Search { get; set; }
}
