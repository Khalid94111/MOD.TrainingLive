using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.Plans;

public class PlanNote : CreationAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public PlanNoteEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string Note { get; set; } = string.Empty;
    public PlanNoteAuthorRole AuthorRole { get; set; }
    public bool IsReturnReason { get; set; }

    protected PlanNote() { }

    public PlanNote(
        Guid id, PlanNoteEntityType entityType, Guid entityId,
        string note, PlanNoteAuthorRole authorRole, bool isReturnReason) : base(id)
    {
        EntityType = entityType;
        EntityId = entityId;
        Note = note;
        AuthorRole = authorRole;
        IsReturnReason = isReturnReason;
    }
}
