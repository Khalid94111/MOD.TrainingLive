using MOD.Training.Training.Consts;
using MOD.Training.Training.Enums;
using System;
using System.ComponentModel.DataAnnotations;
 

namespace MOD.Training.Training.Finance.Dtos;

public class CreateUpdatePriceQuoteDto
{
    [Required]
    public Guid SessionId { get; set; }

    [Required]
    public Guid ProviderId { get; set; }

    [Required]
    public PricingType PricingType { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal QuotedPrice { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int ParticipantsCount { get; set; }

    [MaxLength(TrainingConsts.MaxNotesLength)]
    public string? Notes { get; set; }
}
