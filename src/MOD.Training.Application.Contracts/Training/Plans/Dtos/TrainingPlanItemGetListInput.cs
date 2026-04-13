using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Plans.Dtos;

public class TrainingPlanItemGetListInput : PagedAndSortedResultRequestDto
{
    public Guid PlanId { get; set; }
    public CourseType? CourseType { get; set; }
    public PreferredQuarter? PreferredQuarter { get; set; }
    public Guid? UnitId { get; set; }
}
