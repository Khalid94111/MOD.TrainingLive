using System;

namespace MOD.Training.Training.Centers.Dtos;

public class HrEmployeeLookupDto
{
    public Guid EmployeeId { get; set; }
    public string ServiceNumber { get; set; } = null!;
    public string FullNameAr { get; set; } = null!;
    public string FullNameEn { get; set; } = null!;
    public string? RankName { get; set; }
    public string? PositionName { get; set; }
}
