using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Travel.TravelTypes;

namespace Travel.Allowances;

public class CreateUpdateAllowanceRuleDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public AllowanceType AllowanceType { get; set; }

    public Guid? TravelTypeDefinitionId { get; set; }

    public AllowanceCategory? Category { get; set; }

    public bool? AppliesWhenAccommodationIncluded { get; set; }

    public decimal? AccommodationMultiplier { get; set; }

    public int Priority { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CreateUpdateAllowanceRuleSegmentDto> Segments { get; set; } = new();
}
