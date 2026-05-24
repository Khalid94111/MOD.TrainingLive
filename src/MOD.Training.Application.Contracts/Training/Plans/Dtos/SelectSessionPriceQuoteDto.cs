using System;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Plans.Dtos;

// Phase 4C-α (v4.10.0) — payload for CourseSessionAppService.SelectPriceQuoteAsync.
// Atomic: three writes happen in one transaction — PriceQuote.IsSelected (and old quote
// flipped off if any), CourseSession.SelectedPriceQuoteId, and ActualStart/End dates,
// with status transitioning Planned → Scheduled.
public class SelectSessionPriceQuoteDto
{
    [Required]
    public Guid PriceQuoteId { get; set; }

    [Required]
    public DateTime ActualStartDate { get; set; }

    [Required]
    public DateTime ActualEndDate { get; set; }
}
