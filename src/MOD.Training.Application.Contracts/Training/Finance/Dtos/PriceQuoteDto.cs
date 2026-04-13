using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;
 
namespace MOD.Training.Training.Finance.Dtos;

public class PriceQuoteDto : FullAuditedEntityDto<Guid>
{
    public Guid SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty; // Resolved
    public string CourseName { get; set; } = string.Empty;  // Resolved
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty; // Resolved
    public PricingType PricingType { get; set; }
    public decimal QuotedPrice { get; set; }
    public decimal? PricePerPerson { get; set; }
    public decimal? TotalPrice { get; set; }
    public int ParticipantsCount { get; set; }
    public ApprovalStatus Status { get; set; }
    public string? Notes { get; set; }
}
