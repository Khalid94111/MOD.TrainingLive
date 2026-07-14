using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.CasualCourses;

public class CasualCourseNomination : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid CasualCourseId { get; set; }
    public Guid EmployeeId { get; set; }

    public bool IsReturned { get; set; }
    public Guid? LastReturnNoteId { get; set; }

    protected CasualCourseNomination() { }

    public CasualCourseNomination(Guid id, Guid casualCourseId, Guid employeeId) : base(id)
    {
        CasualCourseId = casualCourseId;
        EmployeeId = employeeId;
    }
}
