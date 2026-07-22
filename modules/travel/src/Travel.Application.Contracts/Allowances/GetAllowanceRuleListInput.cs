using System;
using Travel.TravelTypes;
using Volo.Abp.Application.Dtos;

namespace Travel.Allowances;

public class GetAllowanceRuleListInput : PagedAndSortedResultRequestDto
{
    public AllowanceType? AllowanceType { get; set; }
    public Guid? TravelTypeDefinitionId { get; set; }
    public AllowanceCategory? Category { get; set; }
    public bool? AppliesWhenAccommodationIncluded { get; set; }
    public string? Filter { get; set; }
}
