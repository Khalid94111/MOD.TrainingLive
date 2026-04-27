using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;

namespace MOD.Training.Training.CasualCourses.Dtos;

/// <summary>
/// Patch 5 — input for <c>POST /api/app/casual-courses/calculate-preview</c>. Pure
/// calculator: no DB writes. Powers the read-only Section E breakdown on PAGE 4.2 (UTM)
/// and PAGE 4.4 (UGM). FundingScenario is intentionally NOT on the input — it isn't
/// picked yet at UTM/UGM time, so the preview cannot derive a Source per row.
/// </summary>
public class CalculatePreviewInput
{
    public CourseType CourseType { get; set; }
    public int DurationDays { get; set; }
    public List<Guid> NomineeEmployeeIds { get; set; } = new();
    public decimal? CourseCost { get; set; }
}

public class CalculatePreviewDto
{
    public List<PreviewItemDto> Items { get; set; } = new();
    public decimal TotalOMR { get; set; }
    public CourseType CourseType { get; set; }
    public DateTime ComputedAt { get; set; }
}

public class PreviewItemDto
{
    public Guid FinancialItemId { get; set; }
    public string FinancialItemNameAr { get; set; } = "";
    public bool IsPerDay { get; set; }
    public bool IsPerNominee { get; set; }
    public int EffectiveDays { get; set; }
    public decimal SubtotalOMR { get; set; }

    /// <summary>
    /// One entry per rank for per-nominee items; null for flat items.
    /// </summary>
    public List<PreviewRankRowDto>? RankBreakdown { get; set; }
}

public class PreviewRankRowDto
{
    public Guid RankId { get; set; }
    public string RankNameAr { get; set; } = "";
    public int NomineeCount { get; set; }
    public decimal RatePerUnitOMR { get; set; }
    public decimal SubtotalOMR { get; set; }
}
