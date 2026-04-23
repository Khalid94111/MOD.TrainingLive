using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class RejectDto
{
    [Required]
    [MinLength(10)]
    [MaxLength(500)]
    public string Reason { get; set; } = "";
}
