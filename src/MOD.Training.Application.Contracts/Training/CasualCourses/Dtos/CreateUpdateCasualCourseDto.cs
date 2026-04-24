using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class CreateUpdateCasualCourseDto
{
    [Required]
    public Guid TenantCourseId { get; set; }

    [Required]
    public Guid UnitId { get; set; }

    [Required]
    public CourseType CourseType { get; set; }

    [Range(1, 5)]
    public int Priority { get; set; } = 3;

    [Required]
    [MaxLength(500)]
    public string Justification { get; set; } = "";

    [MaxLength(1000)]
    public string? DescriptionAr { get; set; }

    [MaxLength(1000)]
    public string? ObjectivesAr { get; set; }

    public int DurationYears { get; set; }
    public int DurationMonths { get; set; }

    [Range(1, int.MaxValue)]
    public int DurationDays { get; set; }

    [Required]
    public DateTime EstimatedDateFrom { get; set; }

    [Required]
    public DateTime EstimatedDateTo { get; set; }

    [MaxLength(200)]
    public string? FundingSource { get; set; }

    public List<Guid> NomineeEmployeeIds { get; set; } = new();

    /// <summary>
    /// Optional — UTM's overrides on auto-populated default rates (v4.6.0).
    /// Each entry targets one rank row under one financial item.
    /// </summary>
    public List<FinancialOverrideDto>? FinancialOverrides { get; set; }
}
