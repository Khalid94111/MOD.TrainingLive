using Volo.Abp.Application.Dtos;

namespace Travel.Allowances;

public class GetAccommodationRuleListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}
