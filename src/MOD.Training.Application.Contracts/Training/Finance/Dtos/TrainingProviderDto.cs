using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Finance.Dtos;

public class TrainingProviderDto : FullAuditedEntityDto<Guid>
{
    public string ProviderNameAr { get; set; } = string.Empty;
    public string ProviderNameEn { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }

    // Phase 4B-α — read-only display fields
    public bool IsFromNebras { get; set; }
    public string? NebrasId { get; set; }
    public ProviderScope Scope { get; set; }
    public Guid? CountryId { get; set; }
    public string? CountryNameAr { get; set; }
    public string? CountryNameEn { get; set; }
}
