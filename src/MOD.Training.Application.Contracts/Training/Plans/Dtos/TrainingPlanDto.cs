using System;
using Volo.Abp.Application.Dtos;
using MOD.Training.Training.Enums;

namespace MOD.Training.Training.Plans.Dtos;

public class TrainingPlanDto : FullAuditedEntityDto<Guid>
{
    public int Year { get; set; }
    public PlanStatus Status { get; set; }
    public DateTime? OpenDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public int ItemCount { get; set; }              // Calculated in GetListAsync
    public decimal TotalEstimatedCost { get; set; } // Calculated: SUM of all items' financial amounts
}
