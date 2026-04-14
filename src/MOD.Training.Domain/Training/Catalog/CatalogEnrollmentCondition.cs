using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities;

namespace MOD.Training.Training.Catalog;

/// <summary>
/// Enrollment conditions attached to a catalog course.
/// Auto-copied to TenantCourseConditions when tenant adds the course.
/// </summary>
public class CatalogEnrollmentCondition : Entity<Guid>
{
    public CatalogEnrollmentCondition(Guid id) : base(id)
    {
        
    }
    public CatalogEnrollmentCondition()
    {
        
    }
    public Guid CatalogCourseId { get; set; }
    public ConditionType ConditionType { get; set; }

    /// <summary>
    /// JSON string describing the condition.
    /// Examples: {"min":"Captain","max":"Colonel"}, {"min":5}, {"level":"B2"}
    /// </summary>
    public string ConditionValue { get; set; } = null!;

    // Navigation
    public CourseCatalog? CatalogCourse { get; set; }
}
