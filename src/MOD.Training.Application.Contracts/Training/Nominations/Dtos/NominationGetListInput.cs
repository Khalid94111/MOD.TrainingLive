using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;
 
namespace MOD.Training.Training.Nominations.Dtos;

public class NominationGetListInput : PagedAndSortedResultRequestDto
{
    public Guid? PlanItemId { get; set; }
    public Guid? SessionId { get; set; }
    public NominationStatus? Status { get; set; }
    public Guid? EmployeeId { get; set; }
}
