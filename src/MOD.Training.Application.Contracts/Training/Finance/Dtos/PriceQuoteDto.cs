using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Finance.Dtos;

public class PriceQuoteDto : FullAuditedEntityDto<Guid>
{
    // Polymorphic parent — exactly one is set.
    public Guid? SessionId { get; set; }
    public Guid? CasualCourseId { get; set; }

    public string SessionCode { get; set; } = string.Empty;  // joined (session arm)
    public string CourseName { get; set; } = string.Empty;   // joined

    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public ProviderScope ProviderScope { get; set; }

    // Legacy session-arm pricing
    public PricingType PricingType { get; set; }
    public decimal QuotedPrice { get; set; }
    public decimal? PricePerPerson { get; set; }
    public decimal? TotalPrice { get; set; }
    public int ParticipantsCount { get; set; }
    public ApprovalStatus Status { get; set; }
    public string? Notes { get; set; }

    // Phase 4B-α additions
    public decimal QuotedPriceOMR { get; set; }
    public bool IsSelected { get; set; }
    public Guid? CountryId { get; set; }
    public string? CountryNameAr { get; set; }
    public string? CountryNameEn { get; set; }
    public Guid? CityId { get; set; }
    public string? CityNameAr { get; set; }
    public string? CityNameEn { get; set; }
}
