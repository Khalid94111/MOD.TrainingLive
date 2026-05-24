using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Plans.Dtos;

// Phase 4C-α (v4.10.0) — list filters for PAGE C (sessions list).
// ExecutionStage filter is applied in-memory after the per-row stage computation, since
// stage is derived (not a column). All other filters are pushed to the DB query.
public class CourseSessionGetListInput : PagedAndSortedResultRequestDto
{
    public Guid? TrainingPlanItemId { get; set; }
    public Guid? TrainingCenterPlanItemId { get; set; }
    public Guid? TenantCourseId { get; set; }
    public CourseType? CourseType { get; set; }
    public SessionStatus? Status { get; set; }
    public SessionExecutionStage? ExecutionStage { get; set; }
    public int? PlanYear { get; set; }
    public int? PreferredQuarter { get; set; }
}
