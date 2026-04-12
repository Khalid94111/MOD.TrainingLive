using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Centers.Dtos;

public class CenterPlanWindowGetListInput : PagedAndSortedResultRequestDto
{
    public int? Year { get; set; }
}
