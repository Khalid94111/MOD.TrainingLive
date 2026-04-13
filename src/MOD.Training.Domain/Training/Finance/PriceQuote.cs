using MOD.Training.Training.Enums;
using MOD.Training.Training.Plans;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
 
namespace MOD.Training.Training.Finance;

public class PriceQuote : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid SessionId { get; set; }
    public Guid ProviderId { get; set; }
    public PricingType PricingType { get; set; }
    public decimal QuotedPrice { get; set; }
    public decimal? PricePerPerson { get; set; } // Auto-calc if PricingType=Total
    public decimal? TotalPrice { get; set; }      // Auto-calc if PricingType=PerPerson
    public int ParticipantsCount { get; set; }
    public ApprovalStatus Status { get; set; }
    public string? Notes { get; set; }

    public TrainingProvider? Provider { get; set; }
    public CourseSession? Session { get; set; }

    protected PriceQuote() { }

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
            PricePerPerson = ParticipantsCount > 0 ? QuotedPrice / ParticipantsCount : 0;
        }
    }
}
