using MOD.Training.Training.Enums;
using System;
using System.ComponentModel.DataAnnotations;
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

    /// <summary>CreationTime formatted for display (e.g. "2025-06-04 14:30").</summary>
    public string? CreationTimeFormatted { get; set; }

    /// <summary>ReviewedAt formatted for display.</summary>
    public string? ReviewedAtFormatted { get; set; }
}

public class CreateCourseProposalDto
{
    [Required]
    [MinLength(2, ErrorMessage = "Training:CourseProposal:NameArTooShort")]
    public string CourseNameAr { get; set; } = null!;

    [Required]
    [MinLength(2, ErrorMessage = "Training:CourseProposal:NameEnTooShort")]
    public string CourseNameEn { get; set; } = null!;

    [Required]
    public string Category { get; set; } = null!;

    [Required]
    public string Nature { get; set; } = null!;

    [Required]
    public Guid FieldId { get; set; }
}

public class ReviewCourseProposalDto
{
    [Required]
    public ProposalStatus Decision { get; set; }

    [MinLength(3, ErrorMessage = "Training:CourseProposal:RejectionReasonTooShort")]
    public string? RejectionReason { get; set; }
}

public class CourseProposalGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public ProposalStatus? Status { get; set; }
}
