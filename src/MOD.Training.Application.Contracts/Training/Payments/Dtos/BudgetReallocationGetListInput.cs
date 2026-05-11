using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Payments.Dtos;

public class BudgetReallocationGetListInput : PagedAndSortedResultRequestDto
{
    public Guid? CasualCourseId { get; set; }
    public Guid? CoursePaymentId { get; set; }
    public Guid? ToFinancialItemId { get; set; }
    public string? FundingSourceVoteCode { get; set; }
    public ReallocationStatus? Status { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
}
