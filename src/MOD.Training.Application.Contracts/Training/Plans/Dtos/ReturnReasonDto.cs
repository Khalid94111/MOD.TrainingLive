using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Plans.Dtos;

public class ReturnReasonDto
{
    [Required]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;
}
