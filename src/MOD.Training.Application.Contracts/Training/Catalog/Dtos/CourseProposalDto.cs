using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Catalog.Dtos;

 

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
