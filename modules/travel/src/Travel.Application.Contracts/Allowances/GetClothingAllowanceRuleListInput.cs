using Volo.Abp.Application.Dtos;

namespace Travel.Allowances;

public class GetClothingAllowanceRuleListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
}
