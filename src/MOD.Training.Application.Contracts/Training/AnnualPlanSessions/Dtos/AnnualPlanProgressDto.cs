using System;
using System.Collections.Generic;
using MOD.Training.Training.Enums;

namespace MOD.Training.Training.AnnualPlanSessions.Dtos;

// Phase 4C-α (v4.10.0) — feeds PAGE E (dashboard). Aggregates the year's plan items
// vs their spawned sessions: status counts, per-quarter and per-unit progress bars,
// plus two alert streams (overdue plan items + stuck-Planned sessions).
public class AnnualPlanProgressDto
{
    public int Year { get; set; }
    public List<int> AvailableYears { get; set; } = new();

    public int TotalPlanItems { get; set; }
    public int AwaitingSessionCount { get; set; }
    public int PlannedSessionCount { get; set; }
    public int ScheduledSessionCount { get; set; }
    public int InProgressSessionCount { get; set; }
    public int CompletedSessionCount { get; set; }
    public int CancelledSessionCount { get; set; }
    public int OverdueCount { get; set; }
    public int AttentionCount { get; set; }

    public int OverallProgressPercent { get; set; }

    public List<QuarterProgressDto> ProgressByQuarter { get; set; } = new();
    public List<UnitProgressDto> ProgressByUnit { get; set; } = new();
    public List<OverdueAlertDto> Alerts { get; set; } = new();
}

public class QuarterProgressDto
{
    public int Quarter { get; set; }      // 1..4
    public int Total { get; set; }
    public int AwaitingSession { get; set; }
    public int Planned { get; set; }
    public int Scheduled { get; set; }
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int Pending { get; set; }      // Compatibility: every non-completed item.
}

public class UnitProgressDto
{
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public int Total { get; set; }
    public int AwaitingSession { get; set; }
    public int Planned { get; set; }
    public int Scheduled { get; set; }
    public int InProgress { get; set; }
    public int Completed { get; set; }
}

public class OverdueAlertDto
{
    // "OverduePlanItem" | "StuckPlanned"
    public string Type { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    // "PlanItem" | "Session"
    public string EntityType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int DaysOverdue { get; set; }
    public string CourseNameAr { get; set; } = string.Empty;
    public string CourseNameEn { get; set; } = string.Empty;
    public CourseType CourseType { get; set; }
    public int PreferredQuarter { get; set; }
    public DateTime? DueDate { get; set; }
}
