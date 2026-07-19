using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Centers.Dtos;

public class AdjustCenterPlanItemCapacityDto
{
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }
}
