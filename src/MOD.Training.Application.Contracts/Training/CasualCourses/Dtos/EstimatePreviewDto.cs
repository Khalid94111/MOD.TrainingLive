using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class EstimatePreviewDto
{
    public List<EstimatePreviewItemDto> Items { get; set; } = new();
    public decimal Total { get; set; }
    public CourseType CourseType { get; set; }
    public DateTime ComputedAt { get; set; }
}
