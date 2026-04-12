using System;
using MOD.Training.Training.Enums;
 using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Centers.Dtos;

public class CenterPlanGetListInput : PagedAndSortedResultRequestDto
{
    public Guid? CenterId { get; set; }
    public int? Year { get; set; }
    public CenterPlanStatus? Status { get; set; }
}
