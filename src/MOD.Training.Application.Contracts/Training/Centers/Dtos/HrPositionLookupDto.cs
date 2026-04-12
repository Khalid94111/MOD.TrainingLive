using System;

namespace MOD.Training.Training.Centers.Dtos;

public class HrPositionLookupDto
{
    public Guid PositionId { get; set; }
    public string PositionNameAr { get; set; } = null!;
    public string PositionNameEn { get; set; } = null!;
}
