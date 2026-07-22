using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.CasualCourses.Dtos;

public class CasualCourseNominationDto : FullAuditedEntityDto<Guid>
{
    public Guid? TenantId { get; set; }
    public Guid CasualCourseId { get; set; }
    public Guid EmployeeId { get; set; }

    public string ServiceNumber { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public string RankName { get; set; } = "";
    public string UnitName { get; set; } = "";

    public bool IsReturned { get; set; }
    public string? LastReturnNote { get; set; }
}
