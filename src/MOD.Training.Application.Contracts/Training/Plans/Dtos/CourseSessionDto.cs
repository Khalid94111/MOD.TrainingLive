using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Plans.Dtos;

// Phase 4C-α (v4.10.0). Used by GetListAsync (PAGE C row) and by the Create endpoints
// (post-create return). For full detail with nominations, see CourseSessionDetailDto.
//
// ExecutionStage is computed server-side per row by CourseSessionAppService.GetListAsync —
// see SessionExecutionStage enum for the computation matrix.
public class CourseSessionDto : FullAuditedEntityDto<Guid>
{
    public Guid? TrainingPlanItemId { get; set; }
    public Guid? TrainingCenterPlanItemId { get; set; }

    public Guid TenantCourseId { get; set; }
    public string? TenantCourseNameAr { get; set; }
    public string? TenantCourseNameEn { get; set; }

    public CourseType CourseType { get; set; }

    public int PreferredQuarter { get; set; }
    public int PlanYear { get; set; }

    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }

    public Guid? SelectedPriceQuoteId { get; set; }

    public SessionStatus Status { get; set; }
    public SessionExecutionStage ExecutionStage { get; set; }

    public int NomineesCount { get; set; }

    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledById { get; set; }
}
