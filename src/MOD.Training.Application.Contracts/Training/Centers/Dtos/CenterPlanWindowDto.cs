using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Centers.Dtos;

public class CenterPlanWindowDto : FullAuditedEntityDto<Guid>
{
    public int Year { get; set; }
    public DateTime OpenDate { get; set; }
    public DateTime CloseDate { get; set; }
    public bool IsOpen { get; set; }
    public Guid OpenedById { get; set; }
    public DateTime OpenedAt { get; set; }
}
