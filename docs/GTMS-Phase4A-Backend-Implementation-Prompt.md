# GTMS Phase 4A — Backend Implementation Prompt (Claude Code)

**Version:** v1.0
**Date:** April 23, 2026
**Repo:** local working tree at `D:\Training\MOD.Training` (branch `phase3-changes-v44`)
**Scope:** Backend only — `Domain.Shared` / `Domain` / `Application.Contracts` / `Application` / `EntityFrameworkCore`
**Parent docs (load into context before executing):**
- `docs/GTMS-Phase4A-Implementation-Prompt.md` (v1.2 — locked scope + 8 Round-1 decisions)
- `docs/GTMS-Phase4A-API-List.md` (Gate 2 — the 25 endpoints)
- `docs/GTMS-Phase4A-Mockup.html` (Gate 3 — for role/flow sanity only; UI is out of scope)
- `docs/GTMS-Phase3-Changes-Code-Delta-Report.md` (delivery pattern to mimic)
- `docs/GTMS-Phase3-Changes-Implementation-Prompt.md` §12 (non-negotiable patterns)

---

## 0. Defaulted Decisions

| # | Question | Decision | Rationale |
|---|---|---|---|
| 0.1 | Physical table prefix | **Keep `App*`** for new tables (matches all 11 existing migrations) | Spec docs say `Trn*` but the live schema has always used `App*`; changing now would require destructive renames. `TrainingConsts.DbTablePrefix = "Trn"` stays documentation-only. |
| 0.2 | Existing enum stubs | **Rewrite in place.** `Domain.Shared/Training/Enums/CasualCourseStatus.cs` and `FundingScenario.cs` already exist from an earlier exploratory commit but hold the wrong values. Overwrite with the v4.5 spec values. | Avoids ambiguous "v2" file names; enum is DB-stored as int so values must be authoritative. |
| 0.3 | `PlanNote` polymorphism | **Reuse existing `AppPlanNotes` table.** Add two values to `PlanNoteEntityType` (`CasualCourse = 3`, `CasualCourseNomination = 4`). No new notes table. | Matches Round-1 decision #1; no schema alter — enum stored as int. |
| 0.4 | `ResubmitAsync` target status | **Store `ReturnedFromStatus` on `CasualCourse`.** On return, capture the status we returned from; on resubmit, restore it (not a hardcoded `UnderReview`). Reference: Phase 3 Changes `ResubmitAsync` was fixed to the same pattern (commit `46b0027`). | Per-prompt tweak #2 in the API list; prevents the Phase 3 regression from reappearing. |
| 0.5 | `SelectedPriceQuoteId` FK | **Declare the column nullable without a real FK constraint in Phase 4A.** Phase 4B will add the FK to `AppPriceQuotes` when the polymorphic migration lands. | Keeps migration stable; avoids a second alter-table in 4B (Implementation Prompt §1). |
| 0.6 | Unique index on `AppCasualCourses` | **Skip** `(TenantId, TenantCourseId, EstimatedDateFrom)`. It was listed as optional in §4.1 of the parent prompt, and blocking duplicate requests is a business rule that may legitimately want to be violated (urgent reruns). | Avoid premature constraints. Can add in a follow-up migration. |

---

## 1. Files to DELETE — none

No physical deletions. Some values on existing enums are overwritten (0.2).

---

## 2. Files to MODIFY (11)

### 2.1 `Domain.Shared/Training/Enums/CasualCourseStatus.cs`
**Change:** replace body with full 9-value enum (existing file has 5 placeholder values).

```csharp
namespace MOD.Training.Training.Enums;

public enum CasualCourseStatus
{
    Draft             = 0,
    Submitted         = 1,
    UGMApproved       = 2,
    UnderReview       = 3,
    StaffReviewed     = 4,
    TDApproved        = 5,
    THApproved        = 6,
    ReturnedToCreator = 7,
    Rejected          = 8
}
```

### 2.2 `Domain.Shared/Training/Enums/FundingScenario.cs`
**Change:** replace body to match the three named scenarios from Implementation Prompt §4.4. Start values at `1` so `0` never reads as "FundingCoversAll" on a default-initialized row.

```csharp
namespace MOD.Training.Training.Enums;

public enum FundingScenario
{
    FundingSourceCoversAll    = 1, // No reallocation (no travel items)
    FundingSourceCoversCourse = 2, // Travel items via reallocation
    FinancialItemsCoverAll    = 3  // Full reallocation
}
```

### 2.3 `Domain.Shared/Training/Enums/PlanNoteEntityType.cs`
**Change:** add two values.

```csharp
namespace MOD.Training.Training.Enums;

public enum PlanNoteEntityType
{
    Plan                   = 0,
    PlanItem               = 1,
    Nomination             = 2,
    CasualCourse           = 3, // NEW — Phase 4A
    CasualCourseNomination = 4  // NEW — Phase 4A
}
```

### 2.4 `Application.Contracts/Training/Permissions/TrainingPermissions.cs`
**Change:** expand the existing `CasualCourses` class (lines 58–66) to cover every lifecycle action the 25-endpoint list references. Align naming with the Phase 3 `TrainingPlan` style (`Submit`, `Return`, `Reject`). Keep backward-compatible constants — don't delete `ApproveUGM`/`Review`/`ApproveTD`/`ApproveTH` even if the new names differ.

```csharp
public static class CasualCourses
{
    public const string Default     = GroupName + ".CasualCourses";
    public const string Create      = Default + ".Create";
    public const string Edit        = Default + ".Edit";
    public const string Delete      = Default + ".Delete";
    public const string Submit      = Default + ".Submit";
    public const string Approve     = Default + ".Approve";     // UGM approve
    public const string Review      = Default + ".Review";      // Staff (start-review + assign-scenario + financials)
    public const string TDApprove   = Default + ".TDApprove";
    public const string HeadApprove = Default + ".HeadApprove";
    public const string Return      = Default + ".Return";
    public const string Reject      = Default + ".Reject";

    // [Obsolete — kept for older bindings] — alias constants from pre-v4.5 scaffolding
    public const string ApproveUGM  = Approve;
    public const string ApproveTD   = TDApprove;
    public const string ApproveTH   = HeadApprove;
}
```

Also update `Application.Contracts/Training/Permissions/TrainingPermissionDefinitionProvider.cs` (or wherever the permission tree is declared — grep for `"CasualCourses"`). Register all new permissions under the existing `CasualCourses` group; grant:
- UTM role: `Create`, `Edit`, `Delete`, `Submit`
- UGM role: `Approve`, `Return`, `Reject`
- Staff role: `Review`, `Return`, `Reject`
- TD role: `TDApprove`, `Return`, `Reject`
- TH role: `HeadApprove`, `Return`, `Reject`

(`Default` granted to all so reads don't 403.)

### 2.5 `Domain/Training/Managers/NominationConditionValidator.cs`
**Change:** add a sibling method `ValidateByTenantCourseAsync(Guid tenantCourseId, Guid employeeId)` that reads `TenantCourseConditions` (not `SessionConditions` / `PlanItemConditions`). Reuse the same per-condition logic currently inside `ValidateAsync`.

**Required refactor:** extract the per-condition switch into a private helper `EvaluateConditionAsync(ConditionEntity condition, Employee employee, List<Rank> allRanks)` returning a `ConditionResult`. Call it from all three public methods (`ValidateAsync`, `ValidateByPlanItemAsync`, the new `ValidateByTenantCourseAsync`). Don't copy-paste the ~9-case switch.

```csharp
public async Task<List<ConditionResult>> ValidateByTenantCourseAsync(
    Guid tenantCourseId, Guid employeeId)
{
    var results = new List<ConditionResult>();

    var employee = await employeeResolver.GetWithRankAsync(employeeId);
    if (employee == null)
    {
        results.Add(new ConditionResult(false, "الموظف", "الموظف غير موجود في النظام"));
        return results;
    }

    var allRanks = await employeeResolver.GetAllRanksAsync();

    var condQ = await tenantCourseConditionRepo.GetQueryableAsync();
    var conditions = await tenantCourseConditionRepo.AsyncExecuter.ToListAsync(
        condQ.Where(x => x.TenantCourseId == tenantCourseId));

    if (!conditions.Any()) return results; // permissive default — no conditions = pass

    foreach (var c in conditions)
        results.Add(await EvaluateConditionAsync(c, employee, allRanks));

    return results;
}
```

Constructor gains `IRepository<TenantCourseCondition, Guid> tenantCourseConditionRepo`. The exact type name lives in `Domain/Training/TenantCourses/` — grep `TenantCourseCondition` to confirm.

### 2.6 `Domain/Training/DataSeeder/TenantDataSeeder.cs`
**Change:** seed **3 casual courses per tenant** (Ground / Air / Naval) each at a distinct status so the frontend can smoke-test every role view.

**New private method** `SeedCasualCoursesAsync(Guid tenantId, ...)` called once per tenant from the existing orchestration method:

| # | Status | What to seed alongside |
|---|---|---|
| 1 | `Draft` | 0 financials, 2 nominations (no condition-check run — Draft is UTM scratchpad) |
| 2 | `UnderReview` | 0 financials (Staff hasn't assigned yet), 2 nominations, course was `Submitted → UGMApproved → StartReview` — write one `AppPlanNote` with `EntityType=CasualCourse` (UGM approval note: "موافقة — يُرفع للاعتماد المالي") |
| 3 | `THApproved` | 5 financials (Travel + Accommodation + Transport + Material + Misc) with `EstimatedTotalCost` = SUM of line amounts, `FundingScenario = FundingSourceCoversAll`, 3 nominations (one per rank). Use existing `FinancialItemDefaultResolver.ComputeSubtotal` to fill `EstimatedAmountOMR`. Write 3 `AppPlanNotes` tracing the approval chain (UGM → Staff → TD → TH). |

Reuse existing tenant-scoped helpers:
- `TenantCourseId` — pick the first 3 active tenant courses from the seeded catalog
- `UnitId` — use the seeded UTM's `MainUnitId`
- `RequestedById` — the tenant's UTM user (already resolved in seeder)
- `CourseType` — mix: one `Internal`, one `ExternalLocal`, one `ExternalInternational`
- `FundingSource` — seed a sample Nebras-style code `"NEBR-2026-{idx}"` for the external ones, null for Internal
- `DurationDays` — 5 / 7 / 10 days respectively
- `EstimatedDateFrom/To` — 60 days out from seed time, ranging

**Idempotency:** guard with `if (await casualCourseRepo.AnyAsync(x => x.TenantId == tenantId)) return;` at the top of the method (same pattern as existing seeds).

### 2.7 `EntityFrameworkCore/Training/TrainingDbContextModelCreatingExtensions.cs`
**Change:** register the three new entities. Add a new method `ConfigureCasualCoursesPhase4A(this ModelBuilder builder)` and call it from `ConfigureTraining(...)` right after `ConfigurePhase3Changes` (or the corresponding v4.4 method).

See §3.4 for the config body. Also add `b.Ignore(x => x.Financials)` / `b.HasMany(...)` navs per the spec.

### 2.8 `EntityFrameworkCore/EntityFrameworkCore/TrainingDbContext.cs`
**Change:** add 3 `DbSet<>` properties.

```csharp
public DbSet<CasualCourse>            CasualCourses            { get; set; }
public DbSet<CasualCourseFinancial>   CasualCourseFinancials   { get; set; }
public DbSet<CasualCourseNomination>  CasualCourseNominations  { get; set; }
```

### 2.9 `Application/Training/Mapper/TrainingAutoMapperProfile.cs`
**Change:** NO behavioral change. Mapperly class-based mappers are wired via DI, not AutoMapper — but verify the file compiles after new DTOs land (existing pattern registers nothing for Mapperly, so this file likely stays untouched). Only listed here as a reminder to grep for any AutoMapper `.CreateMap<...>` that might need removal if someone added one by mistake.

### 2.10 `Domain/MOD.Training.Domain.csproj`
**Change:** no edit expected. If the new entities need namespace references not present, the project file auto-resolves because everything is under `MOD.Training.*`. Listed as a verification step only.

### 2.11 `Application.Contracts/Training/Shared/Dtos/ReturnReasonDto.cs` *(verify exists)*
**Change:** none if the Phase 3 DTO is already there (it should be — see `Application.Contracts/Training/Plans/Dtos/ReturnReasonDto.cs` per the earlier `ls`). Casual courses reuse it verbatim — do NOT create a new `ReturnDto`. If the DTO is scoped to `Training.Plans.Dtos`, leave it — the namespace is fine for cross-entity reuse.

If the DTO does not carry a `min 10 chars` constraint, add it:
```csharp
[Required, MinLength(10), MaxLength(2000)] public string Reason { get; set; } = "";
```

---

## 3. Files to ADD (~40)

### 3.1 New enum — `Domain.Shared/Training/Enums/FinancialAmountSource.cs`

```csharp
namespace MOD.Training.Training.Enums;

public enum FinancialAmountSource
{
    FundingSource = 0,
    FinancialItem = 1
}
```

Used by `CasualCourseFinancial.Source` to record which bucket the line item draws from — server-side derivation from `FundingScenario` (not user-chosen).

### 3.2 New entities (3) — under `Domain/Training/CasualCourses/`

#### 3.2.1 `CasualCourse.cs`

```csharp
using MOD.Training.Training.Enums;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.CasualCourses;

public class CasualCourse : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid TenantCourseId { get; set; }
    public Guid UnitId { get; set; }
    public Guid RequestedById { get; set; }

    public CourseType CourseType { get; set; }
    public int Priority { get; set; }                 // 1 (highest) – 5
    public string Justification { get; set; } = "";   // max 500
    public string? DescriptionAr { get; set; }        // max 1000
    public string? ObjectivesAr { get; set; }         // max 1000

    public int DurationYears { get; set; }
    public int DurationMonths { get; set; }
    public int DurationDays { get; set; }

    public DateTime EstimatedDateFrom { get; set; }
    public DateTime EstimatedDateTo { get; set; }

    public string? FundingSource { get; set; }        // max 200; required when external

    public decimal? EstimatedTotalCost { get; set; }  // decimal(18,3); cost-gate value
    public FundingScenario? FundingScenario { get; set; }

    public Guid? SelectedPriceQuoteId { get; set; }   // Phase 4B writes this

    public CasualCourseStatus Status { get; set; } = CasualCourseStatus.Draft;
    public CasualCourseStatus? ReturnedFromStatus { get; set; } // see §0.4

    public bool IsReturned { get; set; }
    public Guid? LastReturnNoteId { get; set; }

    public string? RejectedReason { get; set; }       // max 500

    // Navigations
    public ICollection<CasualCourseFinancial>? Financials { get; set; }
    public ICollection<CasualCourseNomination>? Nominations { get; set; }

    protected CasualCourse() { }

    public CasualCourse(
        Guid id,
        Guid tenantCourseId,
        Guid unitId,
        Guid requestedById,
        CourseType courseType,
        int priority,
        string justification,
        int durationDays,
        DateTime from,
        DateTime to) : base(id)
    {
        TenantCourseId = tenantCourseId;
        UnitId = unitId;
        RequestedById = requestedById;
        CourseType = courseType;
        Priority = priority;
        Justification = justification;
        DurationDays = durationDays;
        EstimatedDateFrom = from;
        EstimatedDateTo = to;
    }
}
```

#### 3.2.2 `CasualCourseFinancial.cs`

```csharp
using MOD.Training.Training.Enums;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MOD.Training.Training.CasualCourses;

public class CasualCourseFinancial : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid CasualCourseId { get; set; }
    public Guid FinancialItemId { get; set; }

    public decimal EstimatedAmountOMR { get; set; } // decimal(18,3); CHG-07 at compute time
    public decimal? ActualAmountOMR   { get; set; } // decimal(18,3); Phase 4B populates

    public FinancialAmountSource Source { get; set; }
    public string? Notes { get; set; } // max 500

    protected CasualCourseFinancial() { }

    public CasualCourseFinancial(
        Guid id,
        Guid casualCourseId,
        Guid financialItemId,
        decimal estimatedAmount,
        FinancialAmountSource source) : base(id)
    {
        CasualCourseId  = casualCourseId;
        FinancialItemId = financialItemId;
        EstimatedAmountOMR = estimatedAmount;
        Source = source;
    }
}
```

#### 3.2.3 `CasualCourseNomination.cs`

```csharp
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

    public string? ConditionSnapshotJson { get; set; } // serialized List<ConditionResult> at add-time

    protected CasualCourseNomination() { }

    public CasualCourseNomination(Guid id, Guid casualCourseId, Guid employeeId) : base(id)
    {
        CasualCourseId = casualCourseId;
        EmployeeId = employeeId;
    }
}
```

### 3.3 New domain service — `Domain/Training/Managers/CasualCourseValidator.cs`

```csharp
using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace MOD.Training.Training.Managers;

public class CasualCourseValidator(
    IRepository<CasualCourse, Guid> casualCourseRepo,
    IRepository<CasualCourseNomination, Guid> nominationRepo,
    NominationConditionValidator conditionValidator)
    : DomainService
{
    public async Task ValidateForSubmitAsync(Guid casualCourseId)
    {
        var cc = await casualCourseRepo.GetAsync(casualCourseId);

        if (cc.Status != CasualCourseStatus.Draft &&
            cc.Status != CasualCourseStatus.ReturnedToCreator)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        if (cc.DurationDays < 1)
            throw new BusinessException("Training:CasualCourse:DurationRequired");

        if (cc.EstimatedDateFrom > cc.EstimatedDateTo)
            throw new BusinessException("Training:CasualCourse:DateRangeInvalid");

        if (cc.CourseType != CourseType.Internal && string.IsNullOrWhiteSpace(cc.FundingSource))
            throw new BusinessException("Training:CasualCourse:FundingSourceRequired");

        var nomQ = await nominationRepo.GetQueryableAsync();
        var nominations = await AsyncExecuter.ToListAsync(
            nomQ.Where(x => x.CasualCourseId == casualCourseId));

        if (!nominations.Any())
            throw new BusinessException("Training:CasualCourse:NoNominations");

        var failures = new List<string>();
        foreach (var n in nominations)
        {
            var results = await conditionValidator.ValidateByTenantCourseAsync(
                cc.TenantCourseId, n.EmployeeId);
            var failed = results.Where(r => !r.Passed).ToList();
            if (failed.Any())
                failures.Add($"{n.EmployeeId}: {string.Join(", ", failed.Select(f => f.Details))}");
        }

        if (failures.Any())
            throw new BusinessException("Training:CasualCourse:ConditionsFailed")
                .WithData("Failures", string.Join(" | ", failures));
    }

    public async Task ValidateForTDApprovalAsync(Guid casualCourseId)
    {
        var cc = await casualCourseRepo.GetAsync(casualCourseId);

        if (cc.Status != CasualCourseStatus.StaffReviewed)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        if (!cc.EstimatedTotalCost.HasValue || cc.EstimatedTotalCost.Value <= 0)
            throw new BusinessException("Training:CasualCourse:CostRequired");

        if (!cc.FundingScenario.HasValue)
            throw new BusinessException("Training:CasualCourse:ScenarioRequired");

        if (cc.IsReturned)
            throw new BusinessException("Training:CasualCourse:UnresolvedReturn");

        var nomQ = await nominationRepo.GetQueryableAsync();
        var anyReturnedNom = await AsyncExecuter.AnyAsync(
            nomQ.Where(x => x.CasualCourseId == casualCourseId && x.IsReturned));
        if (anyReturnedNom)
            throw new BusinessException("Training:CasualCourse:UnresolvedReturnedNominations");
    }

    public async Task ValidateForHeadApprovalAsync(Guid casualCourseId)
    {
        var cc = await casualCourseRepo.GetAsync(casualCourseId);

        if (cc.Status != CasualCourseStatus.TDApproved)
            throw new BusinessException("Training:CasualCourse:InvalidStatusTransition");

        if (!cc.EstimatedTotalCost.HasValue || cc.EstimatedTotalCost.Value <= 0)
            throw new BusinessException("Training:CasualCourse:CostRequired");
    }
}
```

Registered as `ITransientDependency` automatically via `DomainService` base (ABP convention).

### 3.4 EF configuration — inside `TrainingDbContextModelCreatingExtensions.cs`

Add as a new private extension `ConfigureCasualCoursesPhase4A` and invoke from the main `ConfigureTraining`:

```csharp
private static void ConfigureCasualCoursesPhase4A(this ModelBuilder builder)
{
    builder.Entity<CasualCourse>(b =>
    {
        b.ToTable(TrainingConsts.DbTablePrefix + "CasualCourses", TrainingConsts.DbSchema);
        b.ConfigureByConvention();

        b.Property(x => x.TenantCourseId).IsRequired();
        b.Property(x => x.UnitId).IsRequired();
        b.Property(x => x.RequestedById).IsRequired();
        b.Property(x => x.CourseType).IsRequired();
        b.Property(x => x.Priority).IsRequired();
        b.Property(x => x.Justification).IsRequired().HasMaxLength(500);
        b.Property(x => x.DescriptionAr).HasMaxLength(1000);
        b.Property(x => x.ObjectivesAr).HasMaxLength(1000);
        b.Property(x => x.DurationDays).IsRequired();
        b.Property(x => x.EstimatedDateFrom).IsRequired();
        b.Property(x => x.EstimatedDateTo).IsRequired();
        b.Property(x => x.FundingSource).HasMaxLength(200);
        b.Property(x => x.EstimatedTotalCost).HasColumnType("decimal(18,3)");
        b.Property(x => x.RejectedReason).HasMaxLength(500);
        b.Property(x => x.Status).IsRequired();

        b.HasMany(x => x.Financials).WithOne()
            .HasForeignKey(x => x.CasualCourseId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Nominations).WithOne()
            .HasForeignKey(x => x.CasualCourseId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.TenantId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.UnitId });
    });

    builder.Entity<CasualCourseFinancial>(b =>
    {
        b.ToTable(TrainingConsts.DbTablePrefix + "CasualCourseFinancials", TrainingConsts.DbSchema);
        b.ConfigureByConvention();

        b.Property(x => x.CasualCourseId).IsRequired();
        b.Property(x => x.FinancialItemId).IsRequired();
        b.Property(x => x.EstimatedAmountOMR).IsRequired().HasColumnType("decimal(18,3)");
        b.Property(x => x.ActualAmountOMR).HasColumnType("decimal(18,3)");
        b.Property(x => x.Source).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(500);

        b.HasIndex(x => new { x.CasualCourseId, x.FinancialItemId }).IsUnique();
    });

    builder.Entity<CasualCourseNomination>(b =>
    {
        b.ToTable(TrainingConsts.DbTablePrefix + "CasualCourseNominations", TrainingConsts.DbSchema);
        b.ConfigureByConvention();

        b.Property(x => x.CasualCourseId).IsRequired();
        b.Property(x => x.EmployeeId).IsRequired();

        b.HasIndex(x => new { x.CasualCourseId, x.EmployeeId }).IsUnique();
    });
}
```

### 3.5 Migration — `EntityFrameworkCore/Migrations/<timestamp>_Phase4ACasualCourses.cs`

Generate via:
```
dotnet ef migrations add Phase4ACasualCourses -p src/MOD.Training.EntityFrameworkCore -s src/MOD.Training.DbMigrator
```

Expected operations (verify before committing):
- `CreateTable AppCasualCourses` (20 columns, PK Id, indexes on `(TenantId, Status)` + `(TenantId, UnitId)`)
- `CreateTable AppCasualCourseFinancials` (8 columns, PK Id, unique index `(CasualCourseId, FinancialItemId)`, FK cascade)
- `CreateTable AppCasualCourseNominations` (6 columns, PK Id, unique index `(CasualCourseId, EmployeeId)`, FK cascade)
- No changes to existing tables — enum additions (CasualCourse = 3 on `PlanNoteEntityType`) are int-stored, no DDL

**Verify** the migration does NOT contain drops. If it does (e.g., because the old placeholder enums confused the snapshot), abort and regenerate against a clean model.

### 3.6 New DTOs (~14) — under `Application.Contracts/Training/CasualCourses/Dtos/`

One class per file. All `[Required]` annotations match the entity's `IsRequired()` EF config.

| File | Purpose |
|---|---|
| `CasualCourseDto.cs` | Full entity shape + resolved display fields (`CourseNameAr`, `UnitName`, `RequesterName`). Returned by POST / PUT / workflow transitions. |
| `CasualCourseDetailDto.cs` | Extends `CasualCourseDto` with `Nominations: List<CasualCourseNominationDto>`, `Financials: List<CasualCourseFinancialDto>`, `LatestReturnNote: PlanNoteDto?`, `ConditionSummary: string?`. Returned by `GetAsync`. |
| `CasualCourseListItemDto.cs` | Flat grid row — `Id`, `CourseNameAr`, `FundingSource`, `UnitName`, `RequesterName`, `NomineesCount`, `EstimatedTotalCost`, `Status`, `IsReturned`, `LastReturnReason`. |
| `CreateUpdateCasualCourseDto.cs` | UTM create/update input. Includes `NomineeEmployeeIds: List<Guid>` (diff-based on update). `FundingSource` is conditionally required — enforce in validator, not attribute. |
| `CasualCourseGetListInput.cs` | Paged list input — `Year?`, `Status: List<CasualCourseStatus>?`, `UnitId?`, `OnlyMyRequests: bool`, `IsReturnedOnly: bool`, `Search?`. Extends `PagedAndSortedResultRequestDto`. |
| `AssignScenarioDto.cs` | `FundingScenario: FundingScenario`, `EstimatedTotalCost: decimal`, `FinancialItems: List<AssignmentLineDto>`, `Commit: bool`. |
| `AssignmentLineDto.cs` | `Id: Guid?` (null = new), `FinancialItemId: Guid`, `Amount: decimal`, `Notes: string?`. |
| `RejectDto.cs` | `Reason: string` with `[Required, MinLength(10), MaxLength(500)]`. |
| `EstimatePreviewInput.cs` | `TenantCourseId: Guid`, `CourseType: CourseType`, `DurationDays: int`, `NomineeCount: int`. |
| `EstimatePreviewDto.cs` | `Items: List<EstimatePreviewItemDto>`, `Total: decimal`, `CourseType: CourseType`, `ComputedAt: DateTime`. |
| `EstimatePreviewItemDto.cs` | `FinancialItemId`, `FinancialItemName`, `IsPerDay`, `IsPerNominee`, `Rate`, `EffectiveDays`, `EffectiveCount`, `ComputedAmount`. |
| `CasualCourseFinancialDto.cs` | Full CRUD DTO (`Id`, `CasualCourseId`, `FinancialItemId`, `FinancialItemName`, `IsPerDay`, `IsPerNominee`, `EstimatedAmountOMR`, `ActualAmountOMR`, `Source`, `Notes`). |
| `CreateCasualCourseFinancialDto.cs` | `FinancialItemId`, `EstimatedAmountOMR`, `Notes`. `Source` is derived server-side. |
| `UpdateAmountDto.cs` | `Amount: decimal`, `Notes: string?`. |
| `CasualCourseNominationDto.cs` | `Id`, `CasualCourseId`, `EmployeeId`, `EmployeeName`, `RankName`, `UnitName`, `IsReturned`, `LastReturnNote`, `ConditionPassed: bool`, `ConditionDetails: string?`. |

**DO NOT** create a new `ReturnDto` — the endpoints accept the existing `ReturnReasonDto` from `Application.Contracts/Training/Plans/Dtos/ReturnReasonDto.cs`.

**DO NOT** create a new `NoteDto` — the optional note on `UGMApproveAsync` / `TDApproveAsync` / `HeadApproveAsync` accepts the existing `CreatePlanNoteDto` (or a trimmed `NoteDto { Text, IsReturnReason = false }` living in `Shared/Dtos` — pick ONE, don't scatter).

### 3.7 New AppService interfaces — under `Application.Contracts/Training/CasualCourses/`

```
ICasualCourseAppService.cs
ICasualCourseFinancialAppService.cs
ICasualCourseNominationAppService.cs
```

Each interface mirrors its AppService (§3.8) 1:1. Follow the Phase 3 pattern — these are what the Angular `abp generate-proxy` consumes.

### 3.8 New AppServices (3) — under `Application/Training/CasualCourses/`

#### 3.8.1 `CasualCourseAppService.cs` — 15 endpoints

Extends `CrudAppService<CasualCourse, CasualCourseDto, Guid, CasualCourseGetListInput, CreateUpdateCasualCourseDto>`. Override `GetPolicyName` / `CreatePolicyName` / etc. to map to `TrainingPermissions.CasualCourses.*`.

Constructor signature (primary-constructor style, .NET 10):
```csharp
public class CasualCourseAppService(
    IRepository<CasualCourse, Guid> repository,
    IRepository<CasualCourseFinancial, Guid> financialRepo,
    IRepository<CasualCourseNomination, Guid> nominationRepo,
    IRepository<TenantCourse, Guid> tenantCourseRepo,
    IRepository<FinancialItem, Guid> financialItemRepo,
    IRepository<CourseTypeFinancialItemDefault, Guid> defaultsRepo,
    CasualCourseValidator validator,
    NominationConditionValidator conditionValidator,
    FinancialItemDefaultResolver defaultResolver,
    EmployeeResolver employeeResolver,
    IPlanNoteAppService planNoteAppService,
    CasualCourseToDtoMapper toDtoMapper,
    CreateUpdateCasualCourseToEntityMapper fromCreateMapper)
    : CrudAppService<CasualCourse, CasualCourseDto, Guid, CasualCourseGetListInput, CreateUpdateCasualCourseDto>(repository),
      ICasualCourseAppService
```

Endpoints — implementation notes only (not full bodies):

| # | Method | Status guards | Key logic |
|---|---|---|---|
| 1 | `CreateAsync(CreateUpdateCasualCourseDto)` | none (Draft) | Insert entity; copy nominations with `ConditionSnapshotJson` from `ValidateByTenantCourseAsync`; ignore `FundingSource`/`FinancialItems` for Draft. |
| 2 | `GetEstimatePreviewAsync(EstimatePreviewInput)` | authenticated only | Read `CourseTypeFinancialItemDefaults` for `CourseType`; for each, call `defaultResolver.ComputeSubtotal(rate, isPerDay, isPerNominee, durationDays, extraBefore, extraAfter, nomineeCount)` with `rate = fi.DefaultAmountOMR` (no rank lookup). Return summed total + per-item breakdown. **No DB writes.** |
| 3 | `GetListAsync(input)` | unit-scope filter via `employeeResolver.IsCurrentUserUnitScopedAsync()` → if true, `WHERE UnitId = currentUserUnitId` | Project to `CasualCourseListItemDto` inside the query for perf (`NomineesCount` from subquery, not nav). |
| 4 | `GetAsync(id)` | unit-scope enforced | `WithDetailsAsync(x => x.Nominations, x => x.Financials)`; hydrate `LatestReturnNote` via `planNoteAppService.GetListAsync`; hydrate resolved names. |
| 5 | `UpdateAsync(id, dto)` | `Status IN (Draft, ReturnedToCreator)` + unit-scope | Diff-based nominees: delete absent, keep unchanged, add new with condition validation (same algorithm as `TrainingPlanItemAppService.UpdateAsync`). Ignore any financials field on the DTO in this state. |
| 6 | `DeleteAsync(id)` | `Status == Draft` + unit-scope | Relies on cascade delete (EF config) for financials + nominations. |
| 7 | `SubmitAsync(id)` | `Status IN (Draft, ReturnedToCreator)` | Run `validator.ValidateForSubmitAsync`. On `Draft → Submitted`. On `ReturnedToCreator → ReturnedFromStatus` (resume at the returning stage). Clear `IsReturned` + `LastReturnNoteId` + `ReturnedFromStatus` on course; clear `IsReturned` on all nominations. |
| 8 | `UGMApproveAsync(id, NoteDto?)` | `Status == Submitted` | `→ UGMApproved`. If note text present, append as `PlanNote` (EntityType=CasualCourse, `IsReturnReason=false`). |
| 9 | `StartReviewAsync(id)` | `Status == UGMApproved` | `→ UnderReview`. |
| 10 | `AssignScenarioAsync(id, AssignScenarioDto)` | `Status == UnderReview` | Write `FundingScenario` + `EstimatedTotalCost`; upsert financials (diff by `Id` — null = insert, matched = update amount/notes, missing-from-input = delete); derive `Source` from scenario per rule: scenario 1 → `FundingSource`, scenario 2 → travel items = `FinancialItem` / non-travel = `FundingSource`, scenario 3 → all = `FinancialItem`. If `Commit=true`, transition `→ StaffReviewed`. |
| 11 | `TDApproveAsync(id, NoteDto?)` | via `validator.ValidateForTDApprovalAsync` | `→ TDApproved`. Append optional note. |
| 12 | `HeadApproveAsync(id, NoteDto?)` | via `validator.ValidateForHeadApprovalAsync` | `→ THApproved`. Terminal. |
| 13 | `ReturnAsync(id, ReturnReasonDto)` | `Status IN (Submitted, UGMApproved, UnderReview, StaffReviewed, TDApproved)` | Record `ReturnedFromStatus = current`; create `PlanNote` (EntityType=CasualCourse, IsReturnReason=true); set `IsReturned=true`, `LastReturnNoteId=note.Id`, `Status = ReturnedToCreator`. |
| 14 | `ResubmitAsync(id)` | `Status == ReturnedToCreator` | Alias → `SubmitAsync`. Validator already handles the path. |
| 15 | `RejectAsync(id, RejectDto)` | `Status != THApproved && Status != Rejected && Status != Draft` | Write `RejectedReason`; `Status = Rejected`. Append rejection as note (IsReturnReason=false — it's terminal, not a fix loop). |

**Authorization:** every transition uses `[Authorize(TrainingPermissions.CasualCourses.X)]` with `X` matching the permission table in §2.4.

**Unit-scope filter helper:** extract a private `ApplyUnitScopeAsync(IQueryable<CasualCourse> q)` so all query methods share one code path.

**Error codes (non-negotiable):** every `throw new BusinessException(...)` uses the `Training:CasualCourse:{Code}` format with a leading localization key match in `ar.json` / `en.json`. Enumerate:
```
Training:CasualCourse:InvalidStatusTransition
Training:CasualCourse:NoNominations
Training:CasualCourse:DurationRequired
Training:CasualCourse:DateRangeInvalid
Training:CasualCourse:FundingSourceRequired
Training:CasualCourse:CostRequired
Training:CasualCourse:ScenarioRequired
Training:CasualCourse:ConditionsFailed
Training:CasualCourse:UnresolvedReturn
Training:CasualCourse:UnresolvedReturnedNominations
Training:CasualCourse:NotFound
Training:CasualCourse:CannotDeleteInThisStatus
Training:CasualCourse:CannotEditInThisStatus
```

#### 3.8.2 `CasualCourseFinancialAppService.cs` — 5 endpoints

Constructor takes `IRepository<CasualCourseFinancial, Guid>`, `IRepository<CasualCourse, Guid>`, `IRepository<CourseTypeFinancialItemDefault, Guid>`, `IRepository<FinancialItem, Guid>`, `FinancialItemDefaultResolver`, mapper.

| # | Method | Status guard | Logic |
|---|---|---|---|
| 16 | `GetListByCasualCourseAsync(casualCourseId)` | any | Order by `FinancialItem.SortOrder`. Project `FinancialItemName`, `IsPerDay`, `IsPerNominee` via join. |
| 17 | `AutoFillFromDefaultsAsync(casualCourseId)` | parent `Status == UnderReview` | Additive — read `CourseTypeFinancialItemDefaults` for course's `CourseType`; for each default FI not already on the course, insert a `CasualCourseFinancial` with `EstimatedAmountOMR` = `defaultResolver.ComputeSubtotal(fi.DefaultAmountOMR, fi.IsPerDay, fi.IsPerNominee, durationDays, fi.ExtraDaysBefore, fi.ExtraDaysAfter, nomineeCount)`. `Source` = `FundingSource` by default (scenario not chosen yet). |
| 18 | `AddItemAsync(casualCourseId, CreateCasualCourseFinancialDto)` | parent `Status == UnderReview` | Direct insert. Recompute `EstimatedTotalCost` on parent. |
| 19 | `UpdateAmountAsync(id, UpdateAmountDto)` | parent `Status == UnderReview` | Inline edit. Recompute parent total. |
| 20 | `DeleteItemAsync(id)` | parent `Status == UnderReview` | Delete + recompute parent total. |

Extract `RecomputeTotalAsync(casualCourseId)` as a private helper.

#### 3.8.3 `CasualCourseNominationAppService.cs` — 5 endpoints

| # | Method | Guard | Logic |
|---|---|---|---|
| 21 | `GetListByCasualCourseAsync(casualCourseId)` | any | Join to `Employee` + `Rank` + `Unit` for display. Latest condition snapshot from `ConditionSnapshotJson`. |
| 22 | `AddAsync(casualCourseId, employeeId)` | parent `Status IN (Draft, ReturnedToCreator)` + unique check `(CasualCourseId, EmployeeId)` | Run `conditionValidator.ValidateByTenantCourseAsync`; if fails, still insert BUT store the failure snapshot (frontend renders the warning — matches v4.4 UX). Alternative: block on fail — pick block-on-fail per Round-1 decision #6 wording. |
| 23 | `RemoveAsync(id)` | parent state guard | Cascade safe — no per-nominee financial rows in 4A. |
| 24 | `ReturnAsync(id, ReturnReasonDto)` | parent `Status IN (UGMApproved, UnderReview, StaffReviewed, TDApproved)` | Create `PlanNote` (EntityType=CasualCourseNomination, IsReturnReason=true); set `IsReturned=true`, `LastReturnNoteId`. Also set parent `CasualCourse.IsReturned=true` if not already (parent-level flag aggregates). |
| 25 | `ReplaceAsync(id, newEmployeeId)` | parent state guard (matching `AddAsync`) | Revalidate new employee; update `EmployeeId`; rewrite `ConditionSnapshotJson`; clear `IsReturned` + `LastReturnNoteId`; preserve row Id for audit. |

### 3.9 New Mapperly mappers (5) — under `Application/Training/Mapper/`

Follow the existing class-based `MapperBase<TSource, TTarget>` pattern (see `TrainingPlanItemToDtoMapper.cs` as the exemplar). One class per file.

| File | Source → Target | Ignore list |
|---|---|---|
| `CasualCourseToDtoMapper.cs` | `CasualCourse → CasualCourseDto` | `CourseNameAr`, `UnitName`, `RequesterName`, `NomineesCount`, `FinancialsCount`, `LatestReturnReason` |
| `CreateUpdateCasualCourseToEntityMapper.cs` | `CreateUpdateCasualCourseDto → CasualCourse` | Don't map `NomineeEmployeeIds` (handled in AppService), `Status`, `IsReturned`, `LastReturnNoteId`, `ReturnedFromStatus`, `EstimatedTotalCost`, `FundingScenario`, `SelectedPriceQuoteId`, `RejectedReason` (all server-controlled) |
| `CasualCourseFinancialToDtoMapper.cs` | `CasualCourseFinancial → CasualCourseFinancialDto` | `FinancialItemName`, `IsPerDay`, `IsPerNominee` |
| `CreateUpdateCasualCourseFinancialToEntityMapper.cs` | `CreateCasualCourseFinancialDto → CasualCourseFinancial` | `CasualCourseId`, `Source` (both set server-side) |
| `CasualCourseNominationToDtoMapper.cs` | `CasualCourseNomination → CasualCourseNominationDto` | `EmployeeName`, `RankName`, `UnitName`, `ConditionPassed`, `ConditionDetails`, `LastReturnNote` |

**Template (copy-paste skeleton):**

```csharp
using MOD.Training.Training.Mapper.Core;
using Riok.Mapperly.Abstractions;

namespace MOD.Training.Training.Mapper;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CasualCourseToDtoMapper : MapperBase<CasualCourse, CasualCourseDto>
{
    [MapperIgnoreTarget(nameof(CasualCourseDto.CourseNameAr))]
    [MapperIgnoreTarget(nameof(CasualCourseDto.UnitName))]
    [MapperIgnoreTarget(nameof(CasualCourseDto.RequesterName))]
    [MapperIgnoreTarget(nameof(CasualCourseDto.NomineesCount))]
    [MapperIgnoreTarget(nameof(CasualCourseDto.FinancialsCount))]
    [MapperIgnoreTarget(nameof(CasualCourseDto.LatestReturnReason))]
    public override partial CasualCourseDto Map(CasualCourse source);
    public override partial void Map(CasualCourse source, CasualCourseDto destination);
}
```

### 3.10 Localization additions — `Localization/Training/en.json` + `ar.json`

Add keys for every error code enumerated in §3.8.1, plus display keys for status enum values (`CasualCourseStatus.Draft = "مسودة"`, etc.) and the three scenario labels. Match the existing JSON structure (dot notation, no `::` prefix).

Estimated ~60 new key pairs. Don't hand-curate Arabic — pull straight from the mockup file (`GTMS-Phase4A-Mockup.html` — grep for on-screen Arabic).

---

## 4. Migration Plan

### 4.1 Single forward migration
Generate `Phase4ACasualCourses` (see §3.5). Verify the generated C# ONLY contains `CreateTable` operations for the 3 new tables — no drops, no alters on existing tables.

### 4.2 No data backfill
New tables start empty. Seeder (§2.6) populates fresh data on dev reset.

### 4.3 Dev reset procedure
For local test against v4.5:
```
dotnet ef database drop -f -p src/MOD.Training.EntityFrameworkCore -s src/MOD.Training.DbMigrator
dotnet run --project src/MOD.Training.DbMigrator
```
This reapplies all migrations from `Initial` through `Phase4ACasualCourses` and runs the updated seeder (2.6) producing 3 casual courses × 3 tenants = 9 seeded casual courses.

### 4.4 No production migration concerns
Phase 4A is additive-only. No existing tables are touched. No FK to `AppPriceQuotes` until Phase 4B.

---

## 5. Risk Flags ⚠️

| # | Risk | Mitigation |
|---|---|---|
| 1 | **Enum overwrites (§2.1 + §2.2)** change meaning of existing values. If any dev DB has rows with old values (`CasualCourseStatus.UGMApproved = 1`), they now silently mean `Submitted`. | No casual-course data in any environment yet (feature launches in v4.5). Safe to overwrite. Include a one-line comment in the migration: `// No CasualCourse data exists at time of this migration — safe overwrite of enum values.` |
| 2 | **`NominationConditionValidator` refactor** (§2.5) touches a ~250-line method used by 2 existing call sites. Risk of regression in plan-item / session condition flows. | Extract via strict "behavior-preserving" refactor — each public method is reduced to `setup → call helper → return`. Run the existing v4.4 integration tests before committing. |
| 3 | **Condition validation at nominate-time vs submit-time** — AddAsync (§3.8.3, #22) runs the validator per nominee. If Staff-curated conditions change between nominate and submit, the `ConditionSnapshotJson` is stale. | `ValidateForSubmitAsync` re-runs the validator (§3.3). Submit is the authoritative gate. Snapshot is for UX only. |
| 4 | **Financial-items `Source` derivation** in `AssignScenarioAsync` (§3.8.1, #10) depends on knowing which items are "travel" for Scenario 2. | Treat any `FinancialItem` with `Code IN ('TRAVEL_ALLOWANCE', 'ACCOMMODATION', 'TRANSPORT')` as travel. Hardcode this set in the AppService with a clear comment; revisit when a `FinancialItem.IsTravel` flag lands. |
| 5 | **`EstimatedTotalCost` drift** — total can become stale if Staff deletes a financial row after `AssignScenarioAsync` committed. | `DeleteItemAsync` and `UpdateAmountAsync` both call the `RecomputeTotalAsync` helper. Also recompute on `AutoFillFromDefaultsAsync`. |
| 6 | **Seeder FinancialItem resolution** (§2.6) assumes the tenant has seeded financial items with `Code` values. | Guard each `ComputeSubtotal` call with a null check on the fetched `FinancialItem`. If any of Travel/Accom/etc. aren't seeded, skip the THApproved seed (log a warning — don't throw). |
| 7 | **Auto-generated migration regenerates the snapshot** and may include drifted columns from earlier uncommitted entity edits. | Before running `dotnet ef migrations add`, run `git status` — confirm no uncommitted entity changes outside Phase 4A files. |
| 8 | **`IPlanNoteAppService` cross-injection** — CasualCourse AppServices inject the plan note AppService. If it has a circular DI scope, container startup fails. | `PlanNoteAppService` has no dependency on casual courses. One-way dependency is safe. |

---

## 6. Implementation Sequence (Claude Code session)

Execute in this order to minimize intermediate compile errors:

1. **Enums** — §2.1, §2.2, §2.3, §3.1 (4 files)
2. **Entities** — §3.2 (3 files)
3. **EF config + DbSets** — §2.7, §2.8, §3.4
4. **Run `dotnet ef migrations add Phase4ACasualCourses`** — verify generated file matches §3.5
5. **Domain service — CasualCourseValidator** — §3.3
6. **Domain service — NominationConditionValidator refactor + new method** — §2.5
7. **DTOs** — §3.6 (14 files)
8. **AppService interfaces** — §3.7 (3 files)
9. **Mapperly mappers** — §3.9 (5 files)
10. **AppServices** — §3.8 (3 files, 25 endpoints total)
11. **Permissions** — §2.4
12. **Seeder** — §2.6
13. **Localization** — §3.10
14. **Build + reset DB + reseed** — `dotnet build`, then §4.3 commands
15. **Smoke test** — open Swagger at `https://localhost:44324/swagger`, walk the 8 workflow endpoints on one seeded casual course

After step 14 the build MUST be clean — zero errors, zero warnings from new code. If warnings exist from existing code, leave them.

### Commit granularity
Follow the Phase 3 Changes rhythm — one commit per logical step above. Prefix each commit message with `feat(phase4a-backend):`. Example:
```
feat(phase4a-backend): add CasualCourse entities + EF config
feat(phase4a-backend): add CasualCourseValidator + extend NominationConditionValidator
feat(phase4a-backend): generate Phase4ACasualCourses migration
feat(phase4a-backend): implement CasualCourseAppService (15 endpoints)
...
```

---

## 7. File Inventory Summary

| Category | Modified | Added | Total |
|---|---|---|---|
| Domain.Shared enums | 3 | 1 | 4 |
| Domain entities | 0 | 3 | 3 |
| Domain managers | 1 | 1 | 2 |
| Domain seeder | 1 | 0 | 1 |
| EF config / DbContext | 2 | 0 | 2 |
| Migration | 0 | 1 | 1 |
| Application.Contracts DTOs | 0 | 14 | 14 |
| Application.Contracts interfaces | 0 | 3 | 3 |
| Application.Contracts permissions | 1 | 0 | 1 |
| AppServices | 0 | 3 | 3 |
| Mapperly mappers | 0 | 5 | 5 |
| Localization | 2 | 0 | 2 |
| **Total** | **10** | **31** | **41** |

Approximately matches the Phase 3 Changes file count (41 vs. 41) — signals the scope is comparable.

---

## 8. Final Confirmation Checklist

Before starting the Claude Code session, confirm:

- [ ] The 6 defaulted decisions in §0 are acceptable (especially table prefix `App` and enum overwrites)
- [ ] Risk #1 accepted — the dev team has agreed no casual-course data exists in any environment
- [ ] Risk #4 accepted — the hardcoded travel-item code set is acceptable as a placeholder
- [ ] Backup branch taken before Claude Code session begins (branch from `phase3-changes-v44`)
- [ ] `dotnet ef` CLI installed and pointing at the correct EF tools version (`dotnet tool list -g`)
- [ ] SQL Server dev DB reachable at the connection string in `appsettings.Development.json`
- [ ] `GTMS-Phase4A-API-List.md` matches this prompt's endpoint counts (15 + 5 + 5 = 25)
- [ ] Frontend session deferred — this prompt is backend only; Angular regen happens in a separate session

---

## 9. What Goes To Claude Code

This document + Phase 4A Implementation Prompt (v1.2) + Phase 4A API List + Phase 3 Changes Code Delta Report (for pattern reference) → load into a fresh Claude Code session against this repo with the task:

> "Execute Section 6 sequence from `docs/GTMS-Phase4A-Backend-Implementation-Prompt.md`. Commit after each numbered step. Stop and ask before any destructive action (DB drop, force-push, file delete outside the listed inventory)."

Claude Code will:
- Make edits in the repo on a fresh branch `phase4a-backend` off `phase3-changes-v44`
- Generate the migration via `dotnet ef`
- Run `dotnet build` after each logical group
- Commit per logical step (enums → entities → EF → migration → domain → DTOs → mappers → AppServices → permissions → seeder → localization)

This planning chat stays focused on design + decisions; repo work happens in Claude Code.

---

*End of Phase 4A Backend Implementation Prompt — v1.0 — April 23, 2026*
