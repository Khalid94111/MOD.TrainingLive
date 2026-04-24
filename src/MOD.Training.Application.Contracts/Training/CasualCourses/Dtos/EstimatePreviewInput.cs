using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
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

    [Required]
    [MinLength(1)]
    public List<Guid> NomineeEmployeeIds { get; set; } = new();
}
