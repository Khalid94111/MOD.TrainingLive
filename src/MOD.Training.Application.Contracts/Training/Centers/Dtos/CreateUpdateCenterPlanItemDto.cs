using MOD.Training.Training.Enums;
using System;
using System.ComponentModel.DataAnnotations;
 
namespace MOD.Training.Training.Centers.Dtos;

public class CreateUpdateCenterPlanItemDto
{
    [Required]
    public Guid PlanId { get; set; }

    [Required]
    public Guid TenantCourseId { get; set; }

    [Required]
    public DateTime EstimatedStartDate { get; set; }

    [Required]
    public DateTime EstimatedEndDate { get; set; }

    [Required]
    public int Capacity { get; set; }

    public int DurationWeeks { get; set; }

    [StringLength(1000)]
    public string? Objective { get; set; }

    [Required]
    public BeneficiaryType BeneficiaryType { get; set; }
}
