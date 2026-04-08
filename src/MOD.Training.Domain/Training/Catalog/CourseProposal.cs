using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Catalog;

/// <summary>
/// UTM proposes new courses to be added to the catalog.
/// System Admin reviews: Approve → auto-creates CourseCatalog record.
/// </summary>
public class CourseProposal : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string CourseNameAr { get; set; } = null!;
    public string CourseNameEn { get; set; } = null!;

    /// <summary>Military / Civilian</summary>
    public string Category { get; set; } = null!;

    /// <summary>Mandatory / Qualifying</summary>
    public string Nature { get; set; } = null!;

    public Guid FieldId { get; set; }

    public ProposalStatus Status { get; set; } = ProposalStatus.Pending;

    /// <summary>UserId of the UTM who proposed this course.</summary>
    public Guid ProposedById { get; set; }

    /// <summary>UserId of the System Admin who reviewed (nullable until reviewed).</summary>
    public Guid? ReviewedById { get; set; }

    public string? RejectionReason { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // Navigation
    public CourseField? Field { get; set; }
}
