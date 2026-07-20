using MOD.Training.Training.Enums;
using MOD.Training.Training.Plans;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Finance;

/// <summary>
/// Polymorphic-parented price quote: bound to either an annual-plan CourseSession (Phase 3 arm)
/// or a CasualCourse (Phase 4B-α arm). Exactly one of SessionId / CasualCourseId is set.
/// </summary>
public class PriceQuote : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    // Polymorphic parent — exactly one is set (DB CHECK constraint enforced).
    public Guid? SessionId { get; set; }
    public Guid? CasualCourseId { get; set; }

    public Guid ProviderId { get; set; }

    // Legacy session-arm pricing shape (kept for the Phase 3 sessions flow).
    public PricingType PricingType { get; set; }
    public decimal QuotedPrice { get; set; }
    public decimal? PricePerPerson { get; set; }
    public decimal? TotalPrice { get; set; }
    public int ParticipantsCount { get; set; }
    public ApprovalStatus Status { get; set; }
    public string? Notes { get; set; }

    // Phase 4B-α additions
    public decimal QuotedPriceOMR { get; set; }   // Canonical total in OMR for session quotes; direct amount for casual quotes.
    public bool IsSelected { get; set; }          // Server-controlled; only flipped by SelectPriceQuoteAsync.
    public Guid? CountryId { get; set; }          // Soft FK to HrGeographicalLocations
    public Guid? CityId { get; set; }             // Soft FK to HrGeographicalLocations (child of CountryId)

    public TrainingProvider? Provider { get; set; }
    public CourseSession? Session { get; set; }

    protected PriceQuote() { }

    /// <summary>Casual-arm-friendly constructor — Phase 4B-α.</summary>
    public PriceQuote(Guid id) : base(id)
    {
        Status = ApprovalStatus.Pending;
    }

    /// <summary>Legacy session-arm constructor (Phase 3).</summary>
    public PriceQuote(
        Guid id,
        Guid sessionId,
        Guid providerId,
        PricingType pricingType,
        decimal quotedPrice,
        int participantsCount) : base(id)
    {
        SessionId = sessionId;
        ProviderId = providerId;
        PricingType = pricingType;
        QuotedPrice = quotedPrice;
        QuotedPriceOMR = quotedPrice;
        ParticipantsCount = participantsCount;
        Status = ApprovalStatus.Pending;

        CalculatePrices();
    }

    public void CalculatePrices()
    {
        if (PricingType == PricingType.PerPerson)
        {
            PricePerPerson = QuotedPrice;
            TotalPrice = QuotedPrice * ParticipantsCount;
        }
        else
        {
            TotalPrice = QuotedPrice;
            PricePerPerson = ParticipantsCount > 0
                ? Math.Round(QuotedPrice / ParticipantsCount, 3, MidpointRounding.AwayFromZero)
                : 0;
        }

        // All downstream session workflows compare and pay the complete course amount.
        QuotedPriceOMR = TotalPrice ?? QuotedPrice;
    }
}
