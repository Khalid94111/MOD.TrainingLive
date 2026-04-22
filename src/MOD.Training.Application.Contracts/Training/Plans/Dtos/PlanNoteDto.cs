using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.Plans.Dtos;

public class PlanNoteDto : CreationAuditedEntityDto<Guid>
{
    public PlanNoteEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string Note { get; set; } = string.Empty;
    public PlanNoteAuthorRole AuthorRole { get; set; }
    public bool IsReturnReason { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}

public class CreatePlanNoteDto
{
    public PlanNoteEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string Note { get; set; } = string.Empty;
    public bool IsReturnReason { get; set; }
}

public class PlanNoteGetListInput : PagedAndSortedResultRequestDto
{
    public PlanNoteEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
}
