using MOD.Training.Training.Consts;
using System;
using System.ComponentModel.DataAnnotations;
 
namespace MOD.Training.Training.Plans.Dtos;

public class CreateUpdateCourseSessionDto
{
    [Required]
    public Guid CourseId { get; set; }

    [Required]
    [MaxLength(TrainingConsts.MaxSessionCodeLength)]
    public string SessionCode { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [MaxLength(TrainingConsts.MaxLocationLength)]
    public string? Location { get; set; }

    [MaxLength(TrainingConsts.MaxLocationLength)]
    public string? Country { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int MaxSeats { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Cost { get; set; }
}
