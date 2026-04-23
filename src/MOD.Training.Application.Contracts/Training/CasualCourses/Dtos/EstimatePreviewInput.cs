using MOD.Training.Training.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class EstimatePreviewInput
{
    [Required]
    public Guid TenantCourseId { get; set; }

    [Required]
    public CourseType CourseType { get; set; }

    [Range(1, int.MaxValue)]
    public int DurationDays { get; set; }

    [Range(0, int.MaxValue)]
    public int NomineeCount { get; set; }
}
