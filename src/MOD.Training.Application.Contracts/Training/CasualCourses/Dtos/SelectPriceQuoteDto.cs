using System;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.CasualCourses.Dtos;

/// <summary>
/// Atomic quote-selection input: identifies the winning quote and writes the actual confirmed
/// dates the provider committed to. Drives <c>CasualCourseAppService.SelectPriceQuoteAsync</c>.
/// </summary>
public class SelectPriceQuoteDto
{
    [Required]
    public Guid PriceQuoteId { get; set; }

    [Required]
    public DateTime ActualStartDate { get; set; }

    [Required]
    public DateTime ActualEndDate { get; set; }
}
