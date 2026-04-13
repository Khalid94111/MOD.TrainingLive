using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Plans.Dtos;

public class CourseSessionGetListInput : PagedAndSortedResultRequestDto
{
    public Guid? CourseId { get; set; }
    public SessionStatus? Status { get; set; }
}
