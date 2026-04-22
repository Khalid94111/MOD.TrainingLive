# GTMS Phase 3 Changes — Backend Code Delta Report

**Version:** v1.0
**Date:** April 22, 2026
**Repo:** `https://github.com/Khalid94111/MOD.TrainingLive.git` (commit at clone time)
**Scope:** Backend only — Domain.Shared / Domain / Application.Contracts / Application / EntityFrameworkCore
**Companion docs:** Implementation Prompt v1.1, Design v2, Mockup v2

---

## 0. Defaulted Decisions

| Question | Decision | Rationale |
|---|---|---|
| Table prefix | **Keep `App*`** in new migrations (matches existing 9 migrations) | Avoids destructive table renames; `TrainingConsts.DbTablePrefix = "Trn"` stays as documentation only (or fix later) |
| `NominationStatus` enum simplification | **Keep all values for history** — add `Returned = 5` only | Preserves historical row meaning; deprecate writes only |
| `Nomination.SessionId` cardinality | **Make nullable + add `PlanItemId` (required)** | Nomination created before session exists; session linked later |

---

## 1. Files to DELETE — none

No physical file deletions. Some classes get marked `[Obsolete]` and methods stripped.

---

## 2. Files to MODIFY (15)

### 2.1 `Domain.Shared/Training/Enums/PlanStatus.cs`
**Change:** add one enum value.

```csharp
public enum PlanStatus
{
    Draft = 0,
    Open = 1,
    Submitted = 2,
    UnderReview = 3,
    TDApproved = 4,
    THApproved = 5,
    Closed = 6,
    ReturnedToCreator = 7,   // NEW — CHG-05
    Rejected = 8             // NEW — explicit rejected state (was reusing Draft)
}
```

### 2.2 `Domain.Shared/Training/Enums/NominationStatus.cs`
**Change:** add one enum value, preserve existing values for history.

```csharp
public enum NominationStatus
{
    Nominated = 0,
    UTMApproved = 1,    // [Deprecated v4.4] kept for historical rows
    UGMApproved = 2,    // [Deprecated v4.4] kept for historical rows
    TDApproved = 3,     // [Deprecated v4.4] kept for historical rows
    Rejected = 4,
    Returned = 5        // NEW — CHG-05
}
```

### 2.3 `Domain/Training/Finance/FinancialItem.cs`
**Change:** add 5 fields.

```csharp
public class FinancialItem : FullAuditedEntity<Guid>, IMultiTenant
{
    // ... existing fields ...

    // NEW — CHG-02 + CHG-07
    public decimal DefaultAmountOMR { get; set; }      // default 0
    public bool IsPerDay { get; set; }                  // default false
    public bool IsPerNominee { get; set; }              // default false
    public int ExtraDaysBefore { get; set; }            // default 0 — travel-out day(s)
    public int ExtraDaysAfter { get; set; }             // default 0 — travel-back day(s)
}
```

### 2.4 `Domain/Training/Plans/TrainingPlan.cs`
**Change:** no schema change yet (status enum value added in 2.1). But add a domain method for clean state transitions.

Optional polish: add domain methods `MarkReturned()`, `MarkResubmitted()`. Not required — AppService can mutate directly.

### 2.5 `Domain/Training/Plans/TrainingPlanItem.cs`
**Change:** remove `OfficersCount`/`EnlistedCount`/`Capacity` (now derived), add `IsReturned` + `LastReturnNoteId`. Constructor signature changes.

```csharp
public class TrainingPlanItem : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid PlanId { get; set; }
    public Guid TenantCourseId { get; set; }
    public CourseType CourseType { get; set; }
    public PreferredQuarter PreferredQuarter { get; set; }
    public int Priority { get; set; }

    // REMOVED — derived from Nominations.Count
    // public int OfficersCount { get; set; }
    // public int EnlistedCount { get; set; }
    // public int Capacity { get; set; }

    public string Justification { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? ObjectivesAr { get; set; }
    public string? ObjectivesEn { get; set; }
    public int DurationYears { get; set; }
    public int DurationMonths { get; set; }
    public int DurationDays { get; set; }
    public DateTime? EstimatedDateFrom { get; set; }
    public DateTime? EstimatedDateTo { get; set; }
    public string? FundingSource { get; set; }
    public Guid SubmittedById { get; set; }
    public Guid? UnitId { get; set; }

    // NEW — CHG-05
    public bool IsReturned { get; set; }              // default false
    public Guid? LastReturnNoteId { get; set; }       // → PlanNotes.Id

    public TrainingPlan? Plan { get; set; }

    protected TrainingPlanItem() { }

    public TrainingPlanItem(
        Guid id,
        Guid planId,
        Guid tenantCourseId,
        CourseType courseType,
        PreferredQuarter preferredQuarter,
        int priority,
        string justification,
        Guid submittedById) : base(id)
    {
        PlanId = planId;
        TenantCourseId = tenantCourseId;
        CourseType = courseType;
        PreferredQuarter = preferredQuarter;
        Priority = priority;
        Justification = justification;
        SubmittedById = submittedById;
    }
}
```

**Migration impact:** drop 3 columns, add 2 columns. The drop is destructive — document it.

### 2.6 `Domain/Training/Nominations/Nomination.cs`
**Change:** make `SessionId` nullable, add `PlanItemId` (required), add `IsReturned` + `LastReturnNoteId`. Constructor signature changes.

```csharp
public class Nomination : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    // CHANGED — was required, now nullable
    public Guid? SessionId { get; set; }

    // NEW — CHG-01: nominations live at plan-item level until session exists
    public Guid PlanItemId { get; set; }

    public Guid EmployeeId { get; set; }
    public Guid NominatedById { get; set; }
    public NominationStatus Status { get; set; }
    public DateTime NominatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Post-course fields (v3.1 — unchanged)
    public AttendanceStatus? AttendanceStatus { get; set; }
    public ResultType? ResultType { get; set; }
    public string? ResultValue { get; set; }
    public Guid? ResultEnteredById { get; set; }
    public DateTime? ResultEnteredAt { get; set; }

    // NEW — CHG-05
    public bool IsReturned { get; set; }              // default false
    public Guid? LastReturnNoteId { get; set; }

    public CourseSession? Session { get; set; }       // now optional
    public TrainingPlanItem? PlanItem { get; set; }   // NEW navigation

    protected Nomination() { }

    public Nomination(
        Guid id,
        Guid planItemId,
        Guid employeeId,
        Guid nominatedById) : base(id)
    {
        PlanItemId = planItemId;
        EmployeeId = employeeId;
        NominatedById = nominatedById;
        Status = NominationStatus.Nominated;
        NominatedAt = DateTime.Now;
    }
}
```

**Migration impact:** make SessionId nullable, drop `IX_AppNominations_SessionId_EmployeeId` unique index, add `PlanItemId` column + index, add new unique `(PlanItemId, EmployeeId)`.

### 2.7 `EntityFrameworkCore/Training/TrainingDbContextModelCreatingExtensions.cs`
**Change:** four edits.

(a) Update `FinancialItem` config — set decimal precision on `DefaultAmountOMR`:
```csharp
b.Property(x => x.DefaultAmountOMR).IsRequired().HasColumnType("decimal(18,3)");
```

(b) Update `TrainingPlanItem` config — drop `OfficersCount/EnlistedCount/Capacity` lines, no new constraint needed for `IsReturned`.

(c) Update `Nomination` config:
```csharp
b.Property(x => x.SessionId).IsRequired(false);     // was: IsRequired()
b.Property(x => x.PlanItemId).IsRequired();         // NEW

b.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId)
    .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
b.HasOne(x => x.PlanItem).WithMany().HasForeignKey(x => x.PlanItemId)
    .OnDelete(DeleteBehavior.Cascade);              // NEW

b.HasIndex(x => x.PlanItemId);                       // NEW
b.HasIndex(x => new { x.PlanItemId, x.EmployeeId }).IsUnique();  // NEW
// REMOVE old: b.HasIndex(x => new { x.SessionId, x.EmployeeId }).IsUnique();
```

(d) Add three new entity configurations (see Section 3.6).

### 2.8 `Application.Contracts/Training/Finance/Dtos/FinancialItemDtos.cs`
**Change:** add 5 fields to both DTOs.

```csharp
public class FinancialItemDto : EntityDto<Guid>
{
    public Guid? ParentId { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string VoteCode { get; set; } = default!;
    public bool IsGeneral { get; set; }
    public bool IsActive { get; set; }

    // NEW — CHG-02 + CHG-07
    public decimal DefaultAmountOMR { get; set; }
    public bool IsPerDay { get; set; }
    public bool IsPerNominee { get; set; }
    public int ExtraDaysBefore { get; set; }
    public int ExtraDaysAfter { get; set; }
}

public class CreateUpdateFinancialItemDto
{
    public Guid? ParentId { get; set; }
    public string NameAr { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string VoteCode { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    // NEW
    public decimal DefaultAmountOMR { get; set; }
    public bool IsPerDay { get; set; }
    public bool IsPerNominee { get; set; }
    public int ExtraDaysBefore { get; set; }
    public int ExtraDaysAfter { get; set; }
}
```

(`Code` is missing from CreateUpdate but exists on entity — pre-existing bug, leave alone.)

### 2.9 `Application.Contracts/Training/Plans/Dtos/CreateUpdateTrainingPlanItemDto.cs`
**Change:** drop `OfficersCount` + `EnlistedCount`, add `NomineeEmployeeIds` (required, min 1).

```csharp
public class CreateUpdateTrainingPlanItemDto
{
    [Required] public Guid PlanId { get; set; }
    [Required] public Guid TenantCourseId { get; set; }
    [Required] public CourseType CourseType { get; set; }
    [Required] public PreferredQuarter PreferredQuarter { get; set; }
    [Required, Range(1, 5)] public int Priority { get; set; }

    // REMOVED
    // public int OfficersCount { get; set; }
    // public int EnlistedCount { get; set; }

    [Required, MaxLength(TrainingConsts.MaxJustificationLength)]
    public string Justification { get; set; } = string.Empty;

    [MaxLength(TrainingConsts.MaxDescriptionLength)] public string? DescriptionAr { get; set; }
    [MaxLength(TrainingConsts.MaxDescriptionLength)] public string? DescriptionEn { get; set; }
    [MaxLength(TrainingConsts.MaxObjectivesLength)] public string? ObjectivesAr { get; set; }
    [MaxLength(TrainingConsts.MaxObjectivesLength)] public string? ObjectivesEn { get; set; }

    public int DurationYears { get; set; }
    public int DurationMonths { get; set; }
    public int DurationDays { get; set; }
    public DateTime? EstimatedDateFrom { get; set; }
    public DateTime? EstimatedDateTo { get; set; }

    [MaxLength(TrainingConsts.MaxFundingSourceLength)]
    public string? FundingSource { get; set; }

    // NEW — CHG-01: required at creation, min 1
    [Required, MinLength(1)]
    public List<Guid> NomineeEmployeeIds { get; set; } = [];
}
```

### 2.10 `Application.Contracts/Training/Plans/Dtos/TrainingPlanItemDto.cs`
**Change:** drop `OfficersCount/EnlistedCount/Capacity`, add `NomineesCount` (derived) + `IsReturned`.

(Need to view file first — assumed shape based on entity. Will be in spec when written.)

### 2.11 `Application/Training/Mapper/TrainingPlanItemToDtoMapper.cs`
**Change:** Mapperly handles auto-mapping; only add explicit `[MapperIgnore]` on `NomineesCount` (computed in AppService) and on resolved fields. Pattern unchanged.

```csharp
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TrainingPlanItemToDtoMapper : MapperBase<TrainingPlanItem, TrainingPlanItemDto>
{
    [MapperIgnoreTarget(nameof(TrainingPlanItemDto.NomineesCount))]
    [MapperIgnoreTarget(nameof(TrainingPlanItemDto.TenantCourseNameAr))]
    [MapperIgnoreTarget(nameof(TrainingPlanItemDto.TenantCourseNameEn))]
    [MapperIgnoreTarget(nameof(TrainingPlanItemDto.SubmittedByName))]
    [MapperIgnoreTarget(nameof(TrainingPlanItemDto.SubmittedByRank))]
    [MapperIgnoreTarget(nameof(TrainingPlanItemDto.UnitName))]
    [MapperIgnoreTarget(nameof(TrainingPlanItemDto.EstimatedCost))]
    public override partial TrainingPlanItemDto Map(TrainingPlanItem source);
    public override partial void Map(TrainingPlanItem source, TrainingPlanItemDto destination);
}
```

### 2.12 `Application.Contracts/Training/Permissions/TrainingPermissions.cs`
**Change:** add new permission groups.

```csharp
public static class TrainingPlan  // existing, ADD:
{
    // ... existing ...
    public const string ReturnToCreator = Default + ".ReturnToCreator";  // NEW
    public const string Resubmit = Default + ".Resubmit";                // NEW
}

public static class TrainingPlanItem  // existing, ADD:
{
    // ... existing ...
    public const string Return = Default + ".Return";   // NEW — Staff/TD/TH return individual item
}

public static class Nomination  // existing, ADD:
{
    // ... existing ...
    public const string Return = Default + ".Return";   // NEW
    public const string Replace = Default + ".Replace"; // NEW — UTM replaces returned nominee
}

// NEW groups
public static class FinancialItemRankAmount
{
    public const string Default = GroupName + ".FinancialItemRankAmount";
    public const string Create = Default + ".Create";
    public const string Update = Default + ".Update";
    public const string Delete = Default + ".Delete";
}

public static class PlanItemFinancialItemRank
{
    public const string Default = GroupName + ".PlanItemFinancialItemRank";
    public const string UpdateRate = Default + ".UpdateRate";
}

public static class PlanNote
{
    public const string Default = GroupName + ".PlanNote";
    public const string Create = Default + ".Create";
}
```

Also update `TrainingPermissionDefinitionProvider.cs` to register them.

### 2.13 `Application/Training/Plans/TrainingPlanAppService.cs`
**Change:** add 2 new endpoints + tighten approve guards.

```csharp
[Authorize(TrainingPermissions.TrainingPlan.ReturnToCreator)]
public async Task ReturnToCreatorAsync(Guid id, ReturnReasonDto input)
{
    var entity = await repository.GetAsync(id);
    if (entity.Status != PlanStatus.UnderReview && entity.Status != PlanStatus.TDApproved)
        throw new BusinessException("Training:TrainingPlan:CannotReturnInThisStatus");

    var noteId = await planNoteAppService.CreateAsync(new CreatePlanNoteDto
    {
        EntityType = PlanNoteEntityType.Plan,
        EntityId = id,
        Note = input.Reason,
        IsReturnReason = true
    });

    entity.Status = PlanStatus.ReturnedToCreator;
    await repository.UpdateAsync(entity, autoSave: true);
}

[Authorize(TrainingPermissions.TrainingPlan.Resubmit)]
public async Task ResubmitAsync(Guid id)
{
    var entity = await repository.GetAsync(id);
    if (entity.Status != PlanStatus.ReturnedToCreator)
        throw new BusinessException("Training:TrainingPlan:NotInReturnedStatus");

    // Block if any unresolved item/nomination return
    await ValidateNoUnresolvedReturnsAsync(id);

    entity.Status = PlanStatus.UnderReview;
    await repository.UpdateAsync(entity, autoSave: true);
}

// MODIFY existing ApproveAsync + FinalApproveAsync — add guard
[Authorize(TrainingPermissions.TrainingPlan.Approve)]
public async Task ApproveAsync(Guid id)
{
    var entity = await repository.GetAsync(id);
    await ValidateCostGateAsync(id);
    await ValidateNoUnresolvedReturnsAsync(id);  // NEW guard
    entity.Status = PlanStatus.TDApproved;
    await repository.UpdateAsync(entity, autoSave: true);
}

private async Task ValidateNoUnresolvedReturnsAsync(Guid planId)
{
    var itemQ = await planItemRepository.GetQueryableAsync();
    var hasReturnedItem = await AsyncExecuter.AnyAsync(
        itemQ.Where(x => x.PlanId == planId && x.IsReturned));
    if (hasReturnedItem)
        throw new BusinessException("Training:TrainingPlan:UnresolvedReturnedItems");

    var nomQ = await nominationRepository.GetQueryableAsync();
    var hasReturnedNomination = await AsyncExecuter.AnyAsync(
        nomQ.Where(x => x.PlanItem!.PlanId == planId && x.IsReturned));
    if (hasReturnedNomination)
        throw new BusinessException("Training:TrainingPlan:UnresolvedReturnedNominations");
}
```

Constructor adds: `IRepository<Nomination, Guid> nominationRepository`, `IPlanNoteAppService planNoteAppService`.

### 2.14 `Application/Training/Plans/TrainingPlanItemAppService.cs`
**Change:** major — Create accepts nominees + creates them transactionally; Update detects DurationDays change → cascade; new `Return` endpoint; allow edits in `ReturnedToCreator`.

```csharp
[Authorize(TrainingPermissions.TrainingPlanItem.Create)]
public async Task<TrainingPlanItemDto> CreateAsync(CreateUpdateTrainingPlanItemDto input)
{
    var plan = await planRepository.GetAsync(input.PlanId);
    if (plan.Status != PlanStatus.Open && plan.Status != PlanStatus.ReturnedToCreator)
        throw new BusinessException("Training:TrainingPlan:WindowNotOpen");

    var currentUnitId = await employeeResolver.GetCurrentUserUnitIdAsync();

    var entity = new TrainingPlanItem(
        GuidGenerator.Create(),
        input.PlanId,
        input.TenantCourseId,
        input.CourseType,
        input.PreferredQuarter,
        input.Priority,
        input.Justification,
        CurrentUser.Id!.Value)
    {
        UnitId = currentUnitId,
        DescriptionAr = input.DescriptionAr,
        DescriptionEn = input.DescriptionEn,
        ObjectivesAr = input.ObjectivesAr,
        ObjectivesEn = input.ObjectivesEn,
        DurationYears = input.DurationYears,
        DurationMonths = input.DurationMonths,
        DurationDays = input.DurationDays,
        EstimatedDateFrom = input.EstimatedDateFrom,
        EstimatedDateTo = input.EstimatedDateTo,
        FundingSource = input.FundingSource,
    };

    await repository.InsertAsync(entity, autoSave: true);

    // Copy conditions BEFORE validating nominees
    await CopyConditionsAsync(entity.Id, input.TenantCourseId);

    // CHG-01 — validate every nominee against conditions, then create
    var failures = new List<string>();
    foreach (var employeeId in input.NomineeEmployeeIds.Distinct())
    {
        var results = await conditionValidator.ValidateByPlanItemAsync(entity.Id, employeeId);
        var failed = results.Where(r => !r.Passed).ToList();
        if (failed.Any())
        {
            var emp = await employeeResolver.GetByIdAsync(employeeId);
            failures.Add($"{emp?.FullNameAr ?? employeeId.ToString()}: " +
                         string.Join(", ", failed.Select(f => f.Details)));
            continue;
        }

        var nomination = new Nomination(
            GuidGenerator.Create(),
            entity.Id,
            employeeId,
            CurrentUser.Id!.Value);
        await nominationRepository.InsertAsync(nomination, autoSave: true);
    }

    if (failures.Any())
    {
        throw new BusinessException("Training:Nomination:ConditionFailed")
            .WithData("Failures", string.Join(" | ", failures));
    }

    var dto = toDtoMapper.Map(entity);
    await EnrichSingleDtoAsync(dto, entity);
    return dto;
}

[Authorize(TrainingPermissions.TrainingPlanItem.Update)]
public async Task<TrainingPlanItemDto> UpdateAsync(Guid id, CreateUpdateTrainingPlanItemDto input)
{
    var entity = await repository.GetAsync(id);
    var plan = await planRepository.GetAsync(entity.PlanId);

    var canEdit = plan.Status == PlanStatus.Open ||
                  plan.Status == PlanStatus.Draft ||
                  (plan.Status == PlanStatus.ReturnedToCreator) ||
                  (plan.Status == PlanStatus.UnderReview && entity.IsReturned);
    if (!canEdit)
        throw new BusinessException("Training:TrainingPlan:NotEditable");

    var oldDays = entity.DurationDays;

    entity.TenantCourseId = input.TenantCourseId;
    entity.CourseType = input.CourseType;
    entity.PreferredQuarter = input.PreferredQuarter;
    entity.Priority = input.Priority;
    entity.Justification = input.Justification;
    entity.DescriptionAr = input.DescriptionAr;
    entity.DescriptionEn = input.DescriptionEn;
    entity.ObjectivesAr = input.ObjectivesAr;
    entity.ObjectivesEn = input.ObjectivesEn;
    entity.DurationYears = input.DurationYears;
    entity.DurationMonths = input.DurationMonths;
    entity.DurationDays = input.DurationDays;
    entity.EstimatedDateFrom = input.EstimatedDateFrom;
    entity.EstimatedDateTo = input.EstimatedDateTo;
    entity.FundingSource = input.FundingSource;
    entity.IsReturned = false;  // saving clears the return flag

    await repository.UpdateAsync(entity, autoSave: true);

    // CHG-07 cascade — DurationDays changed
    if (oldDays != input.DurationDays)
        await rankBreakdownManager.RefreshForDaysChangeAsync(entity.Id);

    var dto = toDtoMapper.Map(entity);
    await EnrichSingleDtoAsync(dto, entity);
    return dto;
}

[Authorize(TrainingPermissions.TrainingPlanItem.Return)]
public async Task ReturnAsync(Guid id, ReturnReasonDto input)
{
    var entity = await repository.GetAsync(id);
    var plan = await planRepository.GetAsync(entity.PlanId);
    if (plan.Status != PlanStatus.UnderReview && plan.Status != PlanStatus.TDApproved)
        throw new BusinessException("Training:TrainingPlanItem:CannotReturnInThisStatus");

    var noteId = await planNoteAppService.CreateAsync(new CreatePlanNoteDto
    {
        EntityType = PlanNoteEntityType.PlanItem,
        EntityId = id,
        Note = input.Reason,
        IsReturnReason = true
    });

    entity.IsReturned = true;
    entity.LastReturnNoteId = noteId.Id;
    await repository.UpdateAsync(entity, autoSave: true);
}
```

Constructor adds: `IRepository<Nomination, Guid>`, `NominationConditionValidator` (already there or move from NominationAppService), `PlanItemRankBreakdownManager`, `IPlanNoteAppService`.

### 2.15 `Application/Training/Nominations/NominationAppService.cs`
**Change:** strip approval-chain methods, repurpose to read-only + Return + Replace.

Methods to keep:
- `GetAsync` — modify to use PlanItemId for course/session resolution
- `GetListAsync` — modify filters: `PlanItemId`, drop `SessionId` filter (or make optional)

Methods to delete (remove implementations + interface):
- `CreateBatchAsync` — moved to `TrainingPlanItemAppService.CreateAsync`
- `ApproveAsync`
- `RejectAsync`
- `GetApprovalChainAsync`

Methods to add:
- `ReturnAsync(Guid id, ReturnReasonDto)` — Staff/TD/TH return single nominee
- `ReplaceAsync(Guid id, ReplaceNominationDto)` — UTM replaces a returned nominee

Keep `[Obsolete]` placeholders on the dropped public methods if you want graceful client migration; otherwise full delete.

### 2.16 `Application.Contracts/Training/Nominations/INominationAppService.cs`
Mirror the changes from 2.15.

### 2.17 `Application/Training/Plans/PlanItemFinancialItemAppService.cs`
**Change:** `AutoFillFromDefaultsAsync` becomes non-destructive + initializes rank breakdown for each new PIFI where `IsPerNominee == true`.

```csharp
public async Task AutoFillFromDefaultsAsync(Guid planItemId)
{
    var planItem = await planItemRepository.GetAsync(planItemId);

    var defaultsQ = await defaultsRepository.GetQueryableAsync();
    var defaults = await AsyncExecuter.ToListAsync(
        defaultsQ.Where(x => x.CourseType == planItem.CourseType));

    var existingQ = await repository.GetQueryableAsync();
    var existingFiIds = (await AsyncExecuter.ToListAsync(
        existingQ.Where(x => x.PlanItemId == planItemId)
            .Select(x => x.FinancialItemId))).ToHashSet();

    foreach (var def in defaults)
    {
        if (existingFiIds.Contains(def.FinancialItemId)) continue;  // skip if already added

        var pifi = new PlanItemFinancialItem(
            GuidGenerator.Create(), planItemId, def.FinancialItemId, 0);
        await repository.InsertAsync(pifi, autoSave: true);

        // CHG-03 + CHG-07 — initialize per-rank breakdown if applicable
        var fi = await financialItemRepository.GetAsync(def.FinancialItemId);
        if (fi.IsPerNominee)
            await rankBreakdownManager.InitializeAsync(pifi.Id);
    }
}
```

Constructor adds: `PlanItemRankBreakdownManager`.

### 2.18 `Application/Training/Finance/FinancialItemAppService.cs`
**Change:** add field mapping for new fields. The Mapperly mapper handles auto-map; only update DTO files (already in 2.8). Verify both Get and Create/Update flow new fields. Trivial.

### 2.19 `Application/Training/Mapper/CreateUpdateFinancialItemToEntityMapper.cs` *(verify exists)*
Mapperly auto-handles new fields once both source and target have them.

### 2.20 `Domain/Training/Managers/NominationConditionValidator.cs`
**Change:** add new method `ValidateByPlanItemAsync(Guid planItemId, Guid employeeId)` — same logic but queries `PlanItemConditions` instead of `SessionConditions`.

```csharp
public async Task<List<ConditionResult>> ValidateByPlanItemAsync(Guid planItemId, Guid employeeId)
{
    var results = new List<ConditionResult>();

    var employee = await employeeResolver.GetWithRankAsync(employeeId);
    if (employee == null)
    {
        results.Add(new ConditionResult(false, "الموظف", "الموظف غير موجود في النظام"));
        return results;
    }

    var allRanks = await employeeResolver.GetAllRanksAsync();

    var condQ = await planItemConditionRepo.GetQueryableAsync();
    var conditions = await planItemConditionRepo.AsyncExecuter.ToListAsync(
        condQ.Where(x => x.PlanItemId == planItemId));

    if (!conditions.Any()) return results;

    foreach (var condition in conditions)
    {
        // ... reuse same per-condition logic from existing ValidateAsync ...
    }

    return results;
}
```

Refactor: extract per-condition logic into a private helper used by both `ValidateAsync(sessionId,...)` and `ValidateByPlanItemAsync(planItemId,...)`.

### 2.21 `Domain/Training/DataSeeder/TenantDataSeeder.cs`
**Change:** modify `CreateNomination` + `CreateNominationRejected` to accept `planItemId`, drop the legacy `sessionId`. Stop creating `NominationApproval` records in fresh seeds. Backfill new fields on FinancialItems (`IsPerDay`, `IsPerNominee`, `ExtraDaysBefore`, `ExtraDaysAfter`, `DefaultAmountOMR`).

Add seed data for:
- `FinancialItemRankAmounts` — Travel + Accommodation × 3 ranks each (Colonel/Captain/Lieutenant)
- 1-2 example `PlanNote` entries (a return reason for a returned plan in the "MidWorkflow" Air Forces tenant scenario)

---

## 3. Files to ADD (new) (~26)

### 3.1 New Entities (3)

#### `Domain/Training/Finance/FinancialItemRankAmount.cs`
```csharp
public class FinancialItemRankAmount : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid FinancialItemId { get; set; }
    public Guid RankId { get; set; }
    public decimal AmountOMR { get; set; }

    protected FinancialItemRankAmount() { }
    public FinancialItemRankAmount(Guid id, Guid financialItemId, Guid rankId, decimal amountOMR) : base(id)
    {
        FinancialItemId = financialItemId;
        RankId = rankId;
        AmountOMR = amountOMR;
    }
}
```

#### `Domain/Training/Finance/PlanItemFinancialItemRank.cs`
```csharp
public class PlanItemFinancialItemRank : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid PlanItemFinancialItemId { get; set; }
    public Guid RankId { get; set; }
    public int NomineeCount { get; set; }
    public decimal RatePerUnitOMR { get; set; }
    public decimal SubtotalOMR { get; set; }

    protected PlanItemFinancialItemRank() { }
    public PlanItemFinancialItemRank(
        Guid id, Guid pifiId, Guid rankId,
        int nomineeCount, decimal ratePerUnit, decimal subtotal) : base(id)
    {
        PlanItemFinancialItemId = pifiId;
        RankId = rankId;
        NomineeCount = nomineeCount;
        RatePerUnitOMR = ratePerUnit;
        SubtotalOMR = subtotal;
    }
}
```

#### `Domain/Training/Plans/PlanNote.cs`
```csharp
public enum PlanNoteEntityType { Plan = 0, PlanItem = 1, Nomination = 2 }
public enum PlanNoteAuthorRole { UTM = 0, Staff = 1, TD = 2, TH = 3, UGM = 4 }

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
```

(Enums go in `Domain.Shared/Training/Enums/PlanNoteEntityType.cs` + `PlanNoteAuthorRole.cs` per project convention.)

### 3.2 New Domain Services (2)

#### `Domain/Training/Managers/FinancialItemDefaultResolver.cs`
```csharp
public class FinancialItemDefaultResolver(
    IRepository<FinancialItemRankAmount, Guid> rankAmountRepo,
    IRepository<FinancialItem, Guid> financialItemRepo)
    : DomainService
{
    public async Task<decimal> ResolveRateAsync(Guid financialItemId, Guid rankId)
    {
        var q = await rankAmountRepo.GetQueryableAsync();
        var rankAmount = await AsyncExecuter.FirstOrDefaultAsync(
            q.Where(x => x.FinancialItemId == financialItemId && x.RankId == rankId));
        if (rankAmount != null) return rankAmount.AmountOMR;

        var fi = await financialItemRepo.GetAsync(financialItemId);
        return fi.DefaultAmountOMR;
    }

    public async Task<Dictionary<Guid, decimal>> ResolveRatesAsync(
        Guid financialItemId, IEnumerable<Guid> rankIds)
    {
        var rankIdList = rankIds.Distinct().ToList();
        var q = await rankAmountRepo.GetQueryableAsync();
        var ranks = await AsyncExecuter.ToListAsync(
            q.Where(x => x.FinancialItemId == financialItemId && rankIdList.Contains(x.RankId)));

        var result = ranks.ToDictionary(x => x.RankId, x => x.AmountOMR);

        var fi = await financialItemRepo.GetAsync(financialItemId);
        foreach (var rid in rankIdList.Where(r => !result.ContainsKey(r)))
            result[rid] = fi.DefaultAmountOMR;

        return result;
    }

    public decimal ComputeSubtotal(
        decimal rate, bool isPerDay, bool isPerNominee,
        int courseDays, int extraBefore, int extraAfter, int nomineeCount)
    {
        var effectiveDays = isPerDay ? (courseDays + extraBefore + extraAfter) : 1;
        var effectiveCount = isPerNominee ? nomineeCount : 1;
        return rate * effectiveDays * effectiveCount;
    }
}
```

#### `Domain/Training/Managers/PlanItemRankBreakdownManager.cs`
```csharp
public class PlanItemRankBreakdownManager(
    IRepository<PlanItemFinancialItem, Guid> pifiRepo,
    IRepository<PlanItemFinancialItemRank, Guid> rankRepo,
    IRepository<FinancialItem, Guid> fiRepo,
    IRepository<TrainingPlanItem, Guid> planItemRepo,
    IRepository<Nomination, Guid> nominationRepo,
    EmployeeResolver employeeResolver,
    FinancialItemDefaultResolver defaultResolver)
    : DomainService
{
    public async Task InitializeAsync(Guid planItemFinancialItemId)
    {
        var pifi = await pifiRepo.GetAsync(planItemFinancialItemId);
        var fi = await fiRepo.GetAsync(pifi.FinancialItemId);
        if (!fi.IsPerNominee) return;

        var planItem = await planItemRepo.GetAsync(pifi.PlanItemId);
        var rankCounts = await GetRankCountsAsync(pifi.PlanItemId);

        foreach (var (rankId, count) in rankCounts)
        {
            var rate = await defaultResolver.ResolveRateAsync(fi.Id, rankId);
            var subtotal = defaultResolver.ComputeSubtotal(
                rate, fi.IsPerDay, fi.IsPerNominee,
                planItem.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, count);

            await rankRepo.InsertAsync(
                new PlanItemFinancialItemRank(
                    GuidGenerator.Create(), pifi.Id, rankId, count, rate, subtotal),
                autoSave: true);
        }

        await RefreshPifiTotalAsync(pifi);
    }

    public async Task RefreshForDaysChangeAsync(Guid planItemId)
    {
        var planItem = await planItemRepo.GetAsync(planItemId);
        var pifiQ = await pifiRepo.GetQueryableAsync();
        var pifis = await AsyncExecuter.ToListAsync(
            pifiQ.Where(x => x.PlanItemId == planItemId));

        foreach (var pifi in pifis)
        {
            var fi = await fiRepo.GetAsync(pifi.FinancialItemId);
            if (!fi.IsPerDay) continue;

            var rankQ = await rankRepo.GetQueryableAsync();
            var ranks = await AsyncExecuter.ToListAsync(
                rankQ.Where(x => x.PlanItemFinancialItemId == pifi.Id));

            foreach (var r in ranks)
            {
                r.SubtotalOMR = defaultResolver.ComputeSubtotal(
                    r.RatePerUnitOMR, fi.IsPerDay, fi.IsPerNominee,
                    planItem.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, r.NomineeCount);
                await rankRepo.UpdateAsync(r);
            }

            await RefreshPifiTotalAsync(pifi);
        }
    }

    public async Task RefreshForNomineeChangeAsync(Guid planItemId)
    {
        var planItem = await planItemRepo.GetAsync(planItemId);
        var rankCounts = await GetRankCountsAsync(planItemId);

        var pifiQ = await pifiRepo.GetQueryableAsync();
        var pifis = await AsyncExecuter.ToListAsync(
            pifiQ.Where(x => x.PlanItemId == planItemId));

        foreach (var pifi in pifis)
        {
            var fi = await fiRepo.GetAsync(pifi.FinancialItemId);
            if (!fi.IsPerNominee) continue;

            var rankQ = await rankRepo.GetQueryableAsync();
            var existing = (await AsyncExecuter.ToListAsync(
                rankQ.Where(x => x.PlanItemFinancialItemId == pifi.Id)))
                .ToDictionary(x => x.RankId);

            foreach (var (rankId, count) in rankCounts)
            {
                if (existing.TryGetValue(rankId, out var row))
                {
                    row.NomineeCount = count;
                    row.SubtotalOMR = defaultResolver.ComputeSubtotal(
                        row.RatePerUnitOMR, fi.IsPerDay, fi.IsPerNominee,
                        planItem.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, count);
                    await rankRepo.UpdateAsync(row);
                }
                else
                {
                    var rate = await defaultResolver.ResolveRateAsync(fi.Id, rankId);
                    var subtotal = defaultResolver.ComputeSubtotal(
                        rate, fi.IsPerDay, fi.IsPerNominee,
                        planItem.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, count);
                    await rankRepo.InsertAsync(
                        new PlanItemFinancialItemRank(
                            GuidGenerator.Create(), pifi.Id, rankId, count, rate, subtotal));
                }
            }

            // Remove rows for ranks no longer present
            foreach (var (rankId, row) in existing.Where(e => !rankCounts.ContainsKey(e.Key)))
                await rankRepo.DeleteAsync(row);

            await RefreshPifiTotalAsync(pifi);
        }
    }

    public async Task UpdateRateAsync(Guid rankRowId, decimal newRate)
    {
        var row = await rankRepo.GetAsync(rankRowId);
        var pifi = await pifiRepo.GetAsync(row.PlanItemFinancialItemId);
        var fi = await fiRepo.GetAsync(pifi.FinancialItemId);
        var planItem = await planItemRepo.GetAsync(pifi.PlanItemId);

        row.RatePerUnitOMR = newRate;
        row.SubtotalOMR = defaultResolver.ComputeSubtotal(
            newRate, fi.IsPerDay, fi.IsPerNominee,
            planItem.DurationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, row.NomineeCount);
        await rankRepo.UpdateAsync(row);

        await RefreshPifiTotalAsync(pifi);
    }

    private async Task<Dictionary<Guid, int>> GetRankCountsAsync(Guid planItemId)
    {
        var nomQ = await nominationRepo.GetQueryableAsync();
        var employeeIds = await AsyncExecuter.ToListAsync(
            nomQ.Where(x => x.PlanItemId == planItemId && !x.IsReturned)
                .Select(x => x.EmployeeId));

        var employees = await employeeResolver.BatchResolveByIdsAsync(employeeIds);
        return employees.Values
            .Where(e => e.RankId.HasValue)
            .GroupBy(e => e.RankId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task RefreshPifiTotalAsync(PlanItemFinancialItem pifi)
    {
        var rankQ = await rankRepo.GetQueryableAsync();
        var sum = await AsyncExecuter.SumAsync(
            rankQ.Where(x => x.PlanItemFinancialItemId == pifi.Id),
            x => x.SubtotalOMR);

        if (sum > 0)
        {
            pifi.EstimatedAmountOMR = sum;
            await pifiRepo.UpdateAsync(pifi);
        }
    }
}
```

### 3.3 New AppServices (3)

#### `FinancialItemRankAmountAppService` + `IFinancialItemRankAmountAppService`
Standard `CrudAppService<FinancialItemRankAmount, FinancialItemRankAmountDto, ...>` with extra `GetByFinancialItemAsync(Guid)`.

#### `PlanItemFinancialItemRankAppService` + `IPlanItemFinancialItemRankAppService`
Two endpoints:
- `GetListByPifiAsync(Guid pifiId)` — list rows + rank names
- `UpdateRateAsync(Guid rankRowId, decimal newRate)` → calls `PlanItemRankBreakdownManager.UpdateRateAsync`

#### `PlanNoteAppService` + `IPlanNoteAppService`
- `CreateAsync(CreatePlanNoteDto input)` — append-only, infers `AuthorRole` from current user roles
- `GetListAsync(EntityType, EntityId)` — chronological

### 3.4 New DTOs (~10)

```
Application.Contracts/Training/
  Finance/Dtos/
    FinancialItemRankAmountDto.cs
    CreateUpdateFinancialItemRankAmountDto.cs
    FinancialItemRankAmountGetListInput.cs
  Plans/Dtos/
    PlanItemFinancialItemRankDto.cs
    UpdateRateDto.cs
    PlanNoteDto.cs
    CreatePlanNoteDto.cs
    PlanNoteGetListInput.cs
    ReturnReasonDto.cs              ← shared { Reason: string, required, max 2000 }
    ReplaceNominationDto.cs         ← { NewEmployeeId: Guid }
  Nominations/Dtos/
    (extend NominationDto: PlanItemId, IsReturned, LastReturnNote)
```

### 3.5 New Mapperly Mappers (~6)

Each in its own file under `Application/Training/Mapper/`:
- `FinancialItemRankAmountToDtoMapper`
- `CreateUpdateFinancialItemRankAmountToEntityMapper`
- `PlanItemFinancialItemRankToDtoMapper`
- `PlanNoteToDtoMapper`
- (plus updated `NominationToDtoMapper` for new fields — auto-handled)

### 3.6 Update DbContext config — add 3 entity configurations

Add to `TrainingDbContextModelCreatingExtensions.cs` inside `ConfigureFinancePhase3` (or new `ConfigurePhase3Changes`):

```csharp
builder.Entity<FinancialItemRankAmount>(b =>
{
    b.ToTable(TrainingConsts.DbTablePrefix + "FinancialItemRankAmounts", TrainingConsts.DbSchema);
    b.ConfigureByConvention();
    b.Property(x => x.FinancialItemId).IsRequired();
    b.Property(x => x.RankId).IsRequired();
    b.Property(x => x.AmountOMR).IsRequired().HasColumnType("decimal(18,3)");
    b.HasOne<FinancialItem>().WithMany().HasForeignKey(x => x.FinancialItemId).OnDelete(DeleteBehavior.Cascade);
    b.HasIndex(x => new { x.TenantId, x.FinancialItemId, x.RankId }).IsUnique();
});

builder.Entity<PlanItemFinancialItemRank>(b =>
{
    b.ToTable(TrainingConsts.DbTablePrefix + "PlanItemFinancialItemRanks", TrainingConsts.DbSchema);
    b.ConfigureByConvention();
    b.Property(x => x.PlanItemFinancialItemId).IsRequired();
    b.Property(x => x.RankId).IsRequired();
    b.Property(x => x.NomineeCount).IsRequired();
    b.Property(x => x.RatePerUnitOMR).IsRequired().HasColumnType("decimal(18,3)");
    b.Property(x => x.SubtotalOMR).IsRequired().HasColumnType("decimal(18,3)");
    b.HasOne<PlanItemFinancialItem>().WithMany().HasForeignKey(x => x.PlanItemFinancialItemId).OnDelete(DeleteBehavior.Cascade);
    b.HasIndex(x => new { x.TenantId, x.PlanItemFinancialItemId, x.RankId }).IsUnique();
});

builder.Entity<PlanNote>(b =>
{
    b.ToTable(TrainingConsts.DbTablePrefix + "PlanNotes", TrainingConsts.DbSchema);
    b.ConfigureByConvention();
    b.Property(x => x.EntityType).IsRequired();
    b.Property(x => x.EntityId).IsRequired();
    b.Property(x => x.Note).IsRequired().HasMaxLength(2000);
    b.Property(x => x.AuthorRole).IsRequired();
    b.HasIndex(x => new { x.TenantId, x.EntityType, x.EntityId, x.CreationTime });
});
```

Add three `DbSet<>` properties in `TrainingDbContext.cs`.

### 3.7 New Migration: `Phase3ChangesV44`

One single migration is cleaner than two separate ones.

```
Up():
  - Alter table AppFinancialItems: add 5 columns
  - Alter table AppTrainingPlanItems:
      drop columns OfficersCount, EnlistedCount, Capacity
      add columns IsReturned, LastReturnNoteId
  - Alter table AppNominations:
      alter SessionId to nullable
      add columns PlanItemId (nullable initially), IsReturned, LastReturnNoteId
      drop unique index (SessionId, EmployeeId)
      add index PlanItemId
      add unique index (PlanItemId, EmployeeId)
      // PlanItemId stays nullable in DB schema even though entity says required
      // (until production data backfilled — see Risk #1)
  - Create table AppFinancialItemRankAmounts (+ unique index, FK to AppFinancialItems)
  - Create table AppPlanItemFinancialItemRanks (+ unique index, FK to AppPlanItemFinancialItems)
  - Create table AppPlanNotes (+ index)

Down():
  Reverse all of the above (be careful — dropped columns lose data)
```

Naming: `20260422120000_Phase3ChangesV44.cs`

---

## 4. Files to OBSOLETE / NEUTRALIZE

| File | Action |
|---|---|
| `NominationApproval.cs` | Keep entity + table for history. EF config kept. No new writes. Add XML doc comment marking deprecated for new flows. |
| `NominationAppService.CreateBatchAsync` | Delete — moved into `TrainingPlanItemAppService.CreateAsync` |
| `NominationAppService.ApproveAsync/RejectAsync/GetApprovalChainAsync` | Delete (or `[Obsolete]` for one release) — UGM/TD chain dropped |
| Permissions: `Nominations.ApproveUGM` / `ApproveTD` (older group) | Keep constants but stop using; mark `[Obsolete]` in code comments |
| `NominationStatus.UTMApproved/UGMApproved/TDApproved` | Keep enum values; comment as historical-only |

---

## 5. Migration Plan — Two-Phase Deployment for Production Safety

### Phase A — Schema migration (this session)
Run `Phase3ChangesV44` migration. New `Nomination.PlanItemId` column starts **nullable** to accommodate existing rows.

### Phase B — Data backfill (one-time script, separate)

```sql
-- For each existing nomination, derive PlanItemId from the session→course→plan-item chain
-- (only relevant if there are non-empty Nomination rows in production from v4.3)
-- Safe to skip in dev — fresh seeder generates correct data
UPDATE n SET n.PlanItemId = pi.Id
FROM AppNominations n
JOIN AppCourseSessions s ON s.Id = n.SessionId
JOIN AppCourses c ON c.Id = s.CourseId
JOIN AppTrainingPlanItems pi ON pi.TenantCourseId = c.TenantCourseId
                              AND pi.PlanId = (
                                  SELECT TOP 1 PlanId FROM AppTrainingPlanItems
                                  WHERE TenantCourseId = c.TenantCourseId
                                  ORDER BY CreationTime DESC)
WHERE n.PlanItemId IS NULL;
```

### Phase C — Tighten constraint (next migration)
After verifying no nulls remain, add a follow-up migration `Phase3ChangesV44Cleanup` that makes `Nomination.PlanItemId` non-nullable.

For dev, skip Phase B + C — drop the seeder DB and reseed cleanly.

---

## 6. Risk Flags ⚠️

| # | Risk | Mitigation |
|---|---|---|
| 1 | **`Nomination.PlanItemId` non-null in entity, nullable in DB initially** | Phase B backfill script + Phase C migration. Document in README. |
| 2 | **Dropping `OfficersCount`/`EnlistedCount`/`Capacity` is destructive** | These are derived from nominations now. Document loss in migration comment. Backfill not possible from existing data. |
| 3 | **Table prefix mismatch** `Trn` (consts) vs `App` (actual) | Continue using `App` — established. Address in separate refactor PR if needed. |
| 4 | **`AutoFillFromDefaults` becomes additive** instead of destructive | If user re-runs autofill, existing assignments are kept. New behavior may surprise — document in localization. |
| 5 | **`NominationConditionValidator` refactor** | Pull per-condition logic into private helper to avoid duplicating ~250 lines |
| 6 | **DTO breaking changes** ripple to frontend immediately | Frontend will be updated in next session — backend stays consistent. Bumps Angular proxy regeneration. |
| 7 | **Seeder updates touch 3 tenant scenarios** | Each scenario must be revalidated after seeder changes. Run all 3 fresh. |
| 8 | **Cascade timing on `AutoFillFromDefaults`** | Need to ensure rank breakdown initialization is wrapped in same UoW; otherwise partial state on failure. Add `[UnitOfWork]` attribute. |

---

## 7. Implementation Sequence (Backend-only)

Recommended order to minimize compile errors:

1. **Enums** — `PlanStatus`, `NominationStatus` updates + new `PlanNoteEntityType`, `PlanNoteAuthorRole`
2. **Entities** — `FinancialItem` (add fields), `TrainingPlanItem` (modify), `Nomination` (modify), 3 new entities
3. **EF Configurations** — update existing, add 3 new, add `DbSet`s
4. **Generate Migration** — `dotnet ef migrations add Phase3ChangesV44`
5. **Domain Services** — `FinancialItemDefaultResolver`, `PlanItemRankBreakdownManager`, update `NominationConditionValidator`
6. **DTOs** — all updates + new files
7. **Mapperly Mappers** — new + verify existing still compile
8. **Application.Contracts interfaces** — update + new
9. **AppServices** — update existing (TrainingPlan, TrainingPlanItem, Nomination, PlanItemFinancialItem, FinancialItem) + add 3 new
10. **Permissions** — add new + register in `TrainingPermissionDefinitionProvider`
11. **Seeder** — `TenantDataSeeder` updates + add `FinancialItemRankAmount` seed
12. **Compile + smoke test** — `dotnet build`, run migrator, run seeder
13. **Localization** — add Arabic/English keys (~30 new keys for error messages, statuses, button labels)

Estimated total backend file touches: **15 modified + 26 added = 41 files**

---

## 8. File Counts vs Original Estimate

| Category | Original Estimate | Actual after Reconnaissance |
|---|---|---|
| Backend modify | 14 | 15 (+TrainingDbContextModelCreatingExtensions explicitly) |
| Backend add | 25 | 26 (+ReplaceNominationDto, +ReturnReasonDto shared) |
| Backend total | ~45 | ~41 |
| Migrations | 2 | 1 (Phase A only) + optional Phase C cleanup |

---

## 9. What Goes To Claude Code

This document + Implementation Prompt + Design v2 + Mockup v2 → upload to project files → start Claude Code session against the repo with task: "Execute Section 7 sequence from `GTMS-Phase3-Changes-Code-Delta-Report.md`."

Claude Code will:
- Make edits in the repo
- Generate the migration via `dotnet ef`
- Run `dotnet build` to verify compilation
- Commit per logical step (entities, EF, migration, domain, app, seeder)

This planning chat stays focused on design + decisions; the repo work happens in Claude Code.

---

## 10. Final Confirmation Checklist

Before starting Claude Code session, confirm:

- [ ] Section 0 default decisions are acceptable (table prefix, enum kept, nullable SessionId)
- [ ] Risk #2 is acceptable (drop 3 columns, no backfill possible — derived from nominations)
- [ ] Risk #1 is acceptable (two-phase migration for production, single-phase for dev)
- [ ] You've taken a backup / branch of the repo before changes start
- [ ] Frontend will be addressed in a separate session after backend ships

---

*End of Code Delta Report — v1.0 — April 22, 2026*
