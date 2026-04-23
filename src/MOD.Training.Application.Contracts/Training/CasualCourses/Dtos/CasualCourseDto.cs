using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class CasualCourseDto : FullAuditedEntityDto<Guid>
{
    public Guid? TenantId { get; set; }

    public Guid TenantCourseId { get; set; }
    public Guid UnitId { get; set; }
    public Guid RequestedById { get; set; }

    public CourseType CourseType { get; set; }
    public int Priority { get; set; }
    public string Justification { get; set; } = "";
    public string? DescriptionAr { get; set; }
    public string? ObjectivesAr { get; set; }

    public int DurationYears { get; set; }
    public int DurationMonths { get; set; }
    public int DurationDays { get; set; }

    public DateTime EstimatedDateFrom { get; set; }
    public DateTime EstimatedDateTo { get; set; }

    public string? FundingSource { get; set; }

    public decimal? EstimatedTotalCost { get; set; }
    public FundingScenario? FundingScenario { get; set; }

    public Guid? SelectedPriceQuoteId { get; set; }

    public CasualCourseStatus Status { get; set; }
    public CasualCourseStatus? ReturnedFromStatus { get; set; }

    public bool IsReturned { get; set; }
    public Guid? LastReturnNoteId { get; set; }

    public string? RejectedReason { get; set; }

    // Resolved display fields
    public string CourseNameAr { get; set; } = "";
    public string UnitName { get; set; } = "";
    public string RequesterName { get; set; } = "";
    public int NomineesCount { get; set; }
    public int FinancialsCount { get; set; }
    public string? LatestReturnReason { get; set; }
}
