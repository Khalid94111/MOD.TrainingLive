using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Plans;

public class Course : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid TenantCourseId { get; set; }
    public Guid? ProviderId { get; set; }
    public CourseType CourseType { get; set; }
    public CourseStatus Status { get; set; }

    protected Course() { }

    public Course(Guid id, Guid tenantCourseId, CourseType courseType) : base(id)
    {
        TenantCourseId = tenantCourseId;
        CourseType = courseType;
        Status = CourseStatus.Planned;
    }
}
