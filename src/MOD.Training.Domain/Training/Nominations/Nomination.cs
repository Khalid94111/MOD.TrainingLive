using MOD.Training.Training.Enums;
using MOD.Training.Training.Plans;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Nominations;

public class Nomination : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid SessionId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid NominatedById { get; set; } // UTM (MOD-17)
    public NominationStatus Status { get; set; }
    public DateTime NominatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Post-course fields (v3.1)
    public AttendanceStatus? AttendanceStatus { get; set; }
    public ResultType? ResultType { get; set; }
    public string? ResultValue { get; set; }
    public Guid? ResultEnteredById { get; set; }
    public DateTime? ResultEnteredAt { get; set; }

    public CourseSession? Session { get; set; }

    protected Nomination() { }

    public Nomination(
        Guid id,
        Guid sessionId,
        Guid employeeId,
        Guid nominatedById) : base(id)
    {
        SessionId = sessionId;
        EmployeeId = employeeId;
        NominatedById = nominatedById;
        Status = NominationStatus.Nominated;
        NominatedAt = DateTime.Now;
    }
}
