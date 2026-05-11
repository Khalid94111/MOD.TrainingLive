using MOD.Training.Training.Consts;
using MOD.Training.Training.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Finance.Dtos;

/// <summary>
/// Polymorphic-parent input — exactly one of <c>SessionId</c> / <c>CasualCourseId</c> is set
/// (server validates; rejected via <c>Training:PriceQuote:OnePolymorphicParentRequired</c>).
/// Legacy session-arm fields (PricingType / QuotedPrice / ParticipantsCount) are kept for the
/// Phase 3 sessions flow; casual-arm rows use <c>QuotedPriceOMR</c> + Country/City instead.
/// </summary>
public class CreateUpdatePriceQuoteDto
{
    public Guid? SessionId { get; set; }
    public Guid? CasualCourseId { get; set; }

    [Required]
    public Guid ProviderId { get; set; }

    // ── Legacy session-arm pricing (optional in casual arm) ──
    public PricingType PricingType { get; set; }

    [Range(0, double.MaxValue)]
    public decimal QuotedPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int ParticipantsCount { get; set; }

    // ── Phase 4B-α canonical price + location (casual arm) ──
    [Range(0, double.MaxValue)]
    public decimal QuotedPriceOMR { get; set; }

    public Guid? CountryId { get; set; }
    public Guid? CityId { get; set; }

    [MaxLength(TrainingConsts.MaxNotesLength)]
    public string? Notes { get; set; }
}
