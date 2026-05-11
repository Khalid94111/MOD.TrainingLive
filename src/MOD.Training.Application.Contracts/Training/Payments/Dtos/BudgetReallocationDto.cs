using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Payments.Dtos;

public class BudgetReallocationDto : EntityDto<Guid>
{
    public Guid CasualCourseId { get; set; }
    public string? CasualCourseNameAr { get; set; }
    public FundingScenario? FundingScenario { get; set; }   // joined from CasualCourse
    public Guid CoursePaymentId { get; set; }

    public string FundingSourceVoteCode { get; set; } = string.Empty;
    public string? FundingSourceName { get; set; }          // joined from CasualCourse at the time of read

    public Guid ToFinancialItemId { get; set; }
    public string? ToFinancialItemNameAr { get; set; }

    public decimal AmountOMR { get; set; }

    public ReallocationStatus Status { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public string? ApprovalNote { get; set; }

    public DateTime CreationTime { get; set; }
}
