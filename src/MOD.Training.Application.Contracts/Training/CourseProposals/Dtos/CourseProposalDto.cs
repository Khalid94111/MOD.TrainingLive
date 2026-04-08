using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.CourseProposals.Dtos;

public class CourseProposalDto : FullAuditedEntityDto<Guid>
{
    public string CourseNameAr { get; set; } = null!;
    public string CourseNameEn { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Nature { get; set; } = null!;
    public Guid FieldId { get; set; }
    public string? FieldNameAr { get; set; }
    public ProposalStatus Status { get; set; }
    public Guid ProposedById { get; set; }
    public string? ProposedByName { get; set; }
    public Guid? ReviewedById { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class CreateCourseProposalDto
{
    public string CourseNameAr { get; set; } = null!;
    public string CourseNameEn { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Nature { get; set; } = null!;
    public Guid FieldId { get; set; }
}

public class ReviewCourseProposalDto
{
    public ProposalStatus Decision { get; set; }
    public string? RejectionReason { get; set; }
}

public class CourseProposalGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public ProposalStatus? Status { get; set; }
}
