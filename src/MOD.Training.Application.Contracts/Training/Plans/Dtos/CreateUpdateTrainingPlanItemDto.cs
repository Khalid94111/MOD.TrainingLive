using MOD.Training.Training.Consts;
using MOD.Training.Training.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Plans.Dtos;

public class CreateUpdateTrainingPlanItemDto
{
    [Required]
    public Guid PlanId { get; set; }

    [Required]
    public Guid TenantCourseId { get; set; }

    [Required]
    public CourseType CourseType { get; set; }

    [Required]
    public PreferredQuarter PreferredQuarter { get; set; }

    [Required]
    [Range(1, 5)]
    public int Priority { get; set; }

    [Required]
    [MaxLength(TrainingConsts.MaxJustificationLength)]
    public string Justification { get; set; } = string.Empty;

    [MaxLength(TrainingConsts.MaxDescriptionLength)]
    public string? DescriptionAr { get; set; }

    [MaxLength(TrainingConsts.MaxDescriptionLength)]
    public string? DescriptionEn { get; set; }

    [MaxLength(TrainingConsts.MaxObjectivesLength)]
    public string? ObjectivesAr { get; set; }

    [MaxLength(TrainingConsts.MaxObjectivesLength)]
    public string? ObjectivesEn { get; set; }

    public int DurationYears { get; set; }
    public int DurationMonths { get; set; }
    public int DurationDays { get; set; }
    public DateTime? EstimatedDateFrom { get; set; }
    public DateTime? EstimatedDateTo { get; set; }

    [MaxLength(TrainingConsts.MaxFundingSourceLength)]
    public string? FundingSource { get; set; }

    // CHG-01: required at creation, min 1
    [Required]
    [MinLength(1)]
    public List<Guid> NomineeEmployeeIds { get; set; } = [];
}
