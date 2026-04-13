using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Plans.Dtos;

public class TrainingPlanGetListInput : PagedAndSortedResultRequestDto
{
    public int? Year { get; set; }
}
