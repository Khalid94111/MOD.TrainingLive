using MOD.Training.Training.Catalog;
using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.TenantCourses;

/// <summary>
/// Tenant-level course record. Mandatory layer between Catalog and plans.
/// Nothing references CatalogCourseId directly — all downstream references use TenantCourseId.
/// Supports defaults-and-override: copies defaults from catalog, tenant can override.
/// </summary>
public class TenantCourse : FullAuditedEntity<Guid>, IMultiTenant
{
    public TenantCourse(Guid id) : base(id)
    {
        
    }
    public TenantCourse()
    {
        
    }
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Immutable after creation. Cannot be changed once TenantCourse exists.
    /// </summary>
    public Guid CatalogCourseId { get; set; }

    /// <summary>Override catalog default. Null = use catalog default.</summary>
    public int? DefaultCapacity { get; set; }

    /// <summary>Override catalog default. Null = use catalog default.</summary>
    public int? DefaultDurationWeeks { get; set; }

    public ResultType ResultType { get; set; }
    public bool RequiresEvaluation { get; set; }
    public bool RequiresProviderEvaluation { get; set; }
    public bool HasCertificate { get; set; }
    public bool EvaluationBlocksCertificate { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>UserId of who added this course to the tenant.</summary>
    public Guid AddedById { get; set; }

    public DateTime AddedAt { get; set; }

    // Navigation
    public CourseCatalog? CatalogCourse { get; set; }
}
