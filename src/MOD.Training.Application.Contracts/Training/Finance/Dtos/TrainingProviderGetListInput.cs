using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Finance.Dtos;

public class TrainingProviderGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsApproved { get; set; }

    // Phase 4B-α additions
    public ProviderScope? Scope { get; set; }
    public Guid? CountryId { get; set; }
    public bool? IsFromNebras { get; set; }
}
