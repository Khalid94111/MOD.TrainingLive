using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Centers.Dtos;

public class CenterPlanItemGetListInput : PagedAndSortedResultRequestDto
{
    public Guid PlanId { get; set; }
}
