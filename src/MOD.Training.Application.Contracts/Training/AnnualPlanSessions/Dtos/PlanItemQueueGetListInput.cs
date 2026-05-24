using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.AnnualPlanSessions.Dtos;

// Phase 4C-α (v4.10.0) — filters for PAGE A (Sessions to Create queue).
// Year defaults to the current calendar year server-side when null.
public class PlanItemQueueGetListInput : PagedAndSortedResultRequestDto
{
    public int? Year { get; set; }
    public PreferredQuarter? Quarter { get; set; }
    public int? Priority { get; set; }
    public CourseType? CourseType { get; set; }
    public Guid? UnitId { get; set; }
}
