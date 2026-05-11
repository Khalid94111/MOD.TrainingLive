using MOD.Training.Training.Consts;
using MOD.Training.Training.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Finance.Dtos;

public class CreateUpdateTrainingProviderDto
{
    [Required]
    [MaxLength(TrainingConsts.MaxProviderNameLength)]
    public string ProviderNameAr { get; set; } = string.Empty;

    [Required]
    [MaxLength(TrainingConsts.MaxProviderNameLength)]
    public string ProviderNameEn { get; set; } = string.Empty;

    [MaxLength(TrainingConsts.MaxContactPersonLength)]
    public string? ContactPerson { get; set; }

    [MaxLength(TrainingConsts.MaxEmailLength)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(TrainingConsts.MaxPhoneLength)]
    public string? Phone { get; set; }

    [MaxLength(TrainingConsts.MaxAddressLength)]
    public string? Address { get; set; }

    [MaxLength(TrainingConsts.MaxWebsiteLength)]
    public string? Website { get; set; }

    public bool IsApproved { get; set; }
    public bool IsActive { get; set; } = true;

    // Phase 4B-α — Staff-editable
    public ProviderScope Scope { get; set; } = ProviderScope.Local;
    public Guid? CountryId { get; set; }

    // INTENTIONALLY ABSENT (sync-driven, server-controlled): IsFromNebras, NebrasId,
    // AverageRating, TotalRatings.
}
