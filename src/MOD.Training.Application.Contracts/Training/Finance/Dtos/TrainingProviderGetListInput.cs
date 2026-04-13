using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Finance.Dtos;

public class TrainingProviderGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsApproved { get; set; }
}
