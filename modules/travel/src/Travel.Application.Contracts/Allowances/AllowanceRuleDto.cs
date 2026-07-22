using System;
using System.Collections.Generic;
using Travel.TravelTypes;
using Volo.Abp.Application.Dtos;

namespace Travel.Allowances;

public class AllowanceRuleDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public AllowanceType AllowanceType { get; set; }
    public Guid? TravelTypeDefinitionId { get; set; }
    public AllowanceCategory? Category { get; set; }
    public bool? AppliesWhenAccommodationIncluded { get; set; }
    public decimal? AccommodationMultiplier { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public List<AllowanceRuleSegmentDto> Segments { get; set; } = new();
}
