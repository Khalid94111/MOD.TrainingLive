using MOD.Training.Training.Enums;
using System;

namespace MOD.Training.Training.AnnualPlanSessions.Dtos;

// Phase 4C-α (v4.10.0) — one row in PAGE A's queue grid. IsOverdue is computed server-side
// (year < current OR (year == current AND quarter < current quarter)) so the row's red
// highlighting state is authoritative even without timezone trust on the client.
public class PlanItemQueueItemDto
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public int PlanYear { get; set; }

    public Guid TenantCourseId { get; set; }
    public string TenantCourseNameAr { get; set; } = string.Empty;
    public string TenantCourseNameEn { get; set; } = string.Empty;

    public CourseType CourseType { get; set; }
    public PreferredQuarter PreferredQuarter { get; set; }
    public int Priority { get; set; }
    public Guid? UnitId { get; set; }

    public int NomineesCount { get; set; }
    public bool IsOverdue { get; set; }

    public DateTime CreationTime { get; set; }
}
