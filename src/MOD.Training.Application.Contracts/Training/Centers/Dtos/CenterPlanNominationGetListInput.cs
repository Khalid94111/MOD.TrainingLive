using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Centers.Dtos;

public class CenterPlanNominationGetListInput : PagedAndSortedResultRequestDto
{
    public Guid? CenterPlanId { get; set; }
    public Guid? CenterId { get; set; }
}
