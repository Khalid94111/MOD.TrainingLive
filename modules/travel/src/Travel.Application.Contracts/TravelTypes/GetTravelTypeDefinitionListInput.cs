using Volo.Abp.Application.Dtos;

namespace Travel.TravelTypes;

public class GetTravelTypeDefinitionListInput : PagedAndSortedResultRequestDto
{
    public bool? IsActive { get; set; }
    public string? Filter { get; set; }
}
