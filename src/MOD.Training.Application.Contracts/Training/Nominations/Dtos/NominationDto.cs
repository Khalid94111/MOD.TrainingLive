using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Nominations.Dtos;

public class NominationDto : FullAuditedEntityDto<Guid>
{
    public Guid? SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty; // Resolved
    public string CourseName { get; set; } = string.Empty;  // Resolved
    public Guid PlanItemId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty; // Resolved
    public string RankNameAr { get; set; } = string.Empty;   // Resolved from HR (Employee.Rank)
    public Guid NominatedById { get; set; }
    public string NominatedByName { get; set; } = string.Empty; // Resolved
    public NominationStatus Status { get; set; }
    public DateTime NominatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public AttendanceStatus? AttendanceStatus { get; set; }
    public ResultType? ResultType { get; set; }
    public string? ResultValue { get; set; }
    public bool IsReturned { get; set; }
    public Guid? LastReturnNoteId { get; set; }
}
