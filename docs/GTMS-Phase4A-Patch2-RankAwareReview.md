# GTMS Phase 4A — Patch Prompt (Rank-Aware Review — PAGE 4.3)

**Target version:** v4.5.1 → v4.5.2
**Scope:** Reverse Round-1 decision #2 ("item-level only, no per-rank breakdown") for casual courses. Upgrade `CasualCourseFinancials` to support per-rank sub-rows, mirroring the annual-plan shape (`PlanItemFinancialItemRanks`). Rebuild PAGE 4.3 (Staff review) to match PAGE 3.3 structure.
**Base code:** Phase 4A v4.5.1 (rank-aware preview patch shipped).
**Companion docs:** `docs/GTMS-Phase4A-Implementation-Prompt.md` v1.2, `docs/GTMS-Phase3-Changes-v2-Mockup.html` (plan-review screen as structural reference), `docs/GTMS-Phase3-Changes-Code-Delta-Report.md` (for the `PlanItemFinancialItemRank` precedent pattern).

---

## 1. Why this patch exists

The original Round-1 decision #2 locked in at Phase 4A planning was:
> "Item-level financial breakdown only — no per-rank rows for casual courses. Casual courses are low-volume, short-duration, typically 1–3 nominees."

**This was wrong for the same reason the preview was wrong** (fixed in v4.5.1): per-nominee items have rank-variable rates, and flattening to a single row per item destroys the ability to assign different rates to different ranks within the same financial item. A mixed-rank casual course (e.g., 1 Captain + 2 Sergeants on one external training) computes `EstimatedTotalCost` incorrectly because Staff can only enter one rate per item, not one rate per rank.

The annual plan solved this in Phase 3 Changes (CHG-04) with `PlanItemFinancialItemRanks`. This patch applies the **exact same shape** to casual courses so review UX, data model, and resolver calls are identical across the two paths. Staff working on a mixed-rank casual course should feel like they're working on a plan item — same accordion expand, same rank rows, same editable rate input per rank.

**Explicit reversal recorded:** Round-1 decision #2 is overturned. Casual courses now have per-rank financial breakdown.

---

## 2. Schema changes

### 2.1 New table — `TrnCasualCourseFinancialItemRanks`

Direct mirror of `TrnPlanItemFinancialItemRanks`. Naming convention matches: parent is `CasualCourseFinancial`, child is `CasualCourseFinancialItemRank`.

| Field | Type | Required | Notes |
|---|---|---|---|
| `Id` | `Guid` PK | yes | |
| `TenantId` | `Guid` FK | yes | `IMultiTenant` |
| `CasualCourseFinancialId` | `Guid` FK → `TrnCasualCourseFinancials` | yes | Cascade delete |
| `RankId` | `Guid` FK → HR Rank | yes | Read-only HR reference |
| `NomineeCount` | `int` | yes | How many nominees of this rank are on this course |
| `RatePerUnitOMR` | `decimal(18,3)` | yes | Editable by Staff; defaulted from `FinancialItemDefaultResolver.ResolveRateAsync` |
| `SubtotalOMR` | `decimal(18,3)` | yes | Computed: `RatePerUnitOMR × EffectiveDays × NomineeCount`. Written by Staff ops (not a computed column — easier to query) |
| `RateSource` | `string(20)` | yes | `"RankOverride"` or `"DefaultAmount"` at autofill time (audit aid) |

**Unique index:** `(CasualCourseFinancialId, RankId)` — one row per (item × rank).

**Rule:** no row exists if `NomineeCount == 0` for that rank. If a rank is removed from the nominee list, the matching rank row is removed via cascade from `AssignScenarioAsync`.

### 2.2 Modified table — `TrnCasualCourseFinancials`

Small cleanup — the parent table keeps header fields, total is now a sum of children:

| Field | Change | Notes |
|---|---|---|
| `EstimatedAmountOMR` | **rename** → `TotalAmountOMR` | Denormalized sum of child `SubtotalOMR`; written by `AssignScenarioAsync` after rank rows are upserted. Kept (not computed column) so reporting queries stay flat. |
| `ActualAmountOMR` | unchanged | Still populated in Phase 4B by Finance Officer |
| `Source` | unchanged | `FundingSource` / `FinancialItem` — driven by scenario |
| `Notes` | unchanged | Item-level Staff notes (parent-level, not per-rank) |

**Unique index unchanged:** `(CasualCourseId, FinancialItemId)` still holds.

### 2.3 Migration — `v4_5_2_CasualCourseFinancialItemRanks`

Migration does three things:

1. **Create new table** `TrnCasualCourseFinancialItemRanks` with FK + unique index
2. **Rename column** `CasualCourseFinancials.EstimatedAmountOMR` → `TotalAmountOMR`
3. **Data migration** — for every existing `CasualCourseFinancial` row where `IsPerNominee == true` on the referenced `FinancialItem`, insert rank rows by:
   - Joining the casual course's nominations to their employees' ranks
   - Grouping by rank
   - Creating one rank row per group with `RatePerUnitOMR = existing EstimatedAmountOMR / NomineeCount` (best-effort backfill — Staff can correct after migration)
   - `RateSource = "DefaultAmount"` on all backfilled rows (because the old schema couldn't distinguish)

Run migration in a single transaction. If the backfill fails on any tenant, rollback cleanly.

**Non-per-nominee items** (flat rate items like "Course Cost") get a single rank row with `RankId = Guid.Empty` (sentinel), `NomineeCount = 1`, `RatePerUnitOMR = existing amount`, `SubtotalOMR = existing amount`. This keeps the query shape uniform.

---

## 3. Domain changes

### 3.1 New entity — `CasualCourseFinancialItemRank`

**File:** `src/YourApp.Domain/Training/CasualCourses/CasualCourseFinancialItemRank.cs`

Direct copy of `PlanItemFinancialItemRank` from Phase 3 Changes. Same properties, same multi-tenant attribute, same `FullAuditedEntity` base (not sure — check what `PlanItemFinancialItemRank` uses and match exactly).

### 3.2 Updated entity — `CasualCourseFinancial`

Add navigation:
```csharp
public virtual ICollection<CasualCourseFinancialItemRank> Ranks { get; set; } = new List<CasualCourseFinancialItemRank>();
```

Remove `EstimatedAmountOMR` property. Add `TotalAmountOMR` in its place.

### 3.3 EF Configuration

**File:** `src/YourApp.EntityFrameworkCore/Training/Configurations/CasualCourseFinancialItemRankConfiguration.cs`

Copy `PlanItemFinancialItemRankConfiguration` as template. Change:
- Table name → `TrnCasualCourseFinancialItemRanks`
- Parent FK → `CasualCourseFinancialId`

Add `DbSet<CasualCourseFinancialItemRank>` to `TrainingDbContext`.

### 3.4 Domain service — `CasualCourseFinancialCalculator`

**New file:** `src/YourApp.Domain/Training/Managers/CasualCourseFinancialCalculator.cs`

Mirrors the annual-plan equivalent (if one exists; if not, create both and share):

```csharp
public class CasualCourseFinancialCalculator : IDomainService
{
    // Computes SubtotalOMR for a single rank row
    public decimal ComputeRankSubtotal(
        FinancialItem item,
        decimal ratePerUnit,
        int courseDurationDays,
        int nomineeCount)
    {
        var effectiveDays = item.IsPerDay
            ? courseDurationDays + item.ExtraDaysBefore + item.ExtraDaysAfter
            : 1;
        var effectiveCount = item.IsPerNominee ? nomineeCount : 1;
        return ratePerUnit * effectiveDays * effectiveCount;
    }

    // Recomputes TotalAmountOMR on parent from child rank rows
    public decimal ComputeParentTotal(CasualCourseFinancial parent)
    {
        return parent.Ranks.Sum(r => r.SubtotalOMR);
    }
}
```

Same as `PlanItemFinancialCalculator` if one exists — DRY both calls through a shared helper if practical, otherwise keep them parallel.

---

## 4. AppService changes

### 4.1 `CasualCourseFinancialAppService.AutoFillFromDefaultsAsync` — rewrite

**Old behavior:** creates one `CasualCourseFinancial` row per default, sets `EstimatedAmountOMR = DefaultAmountOMR × effectiveDays × totalNomineeCount`.

**New behavior:** creates one `CasualCourseFinancial` row per default + one `CasualCourseFinancialItemRank` row per (item × rank) combination:

```csharp
public async Task<ListResultDto<CasualCourseFinancialDto>> AutoFillFromDefaultsAsync(Guid casualCourseId)
{
    var course = await _casualCourseRepo.WithDetailsAsync(x => x.Nominations);  // needs nominees for rank grouping
    // Status guard...
    
    var nominees = await _employeeResolver.GetEmployeesWithRanksAsync(
        course.Nominations.Select(n => n.EmployeeId).ToList());
    var nomineesByRank = nominees.GroupBy(n => n.RankId).ToList();
    
    var defaults = await _defaultRepo.GetListAsync(d =>
        d.TenantId == CurrentTenant.Id && d.CourseType == course.CourseType);
    
    foreach (var def in defaults.OrderBy(d => d.SortOrder))
    {
        // Skip if parent row already exists (additive)
        var existingParent = await _financialRepo.FirstOrDefaultAsync(
            f => f.CasualCourseId == casualCourseId && f.FinancialItemId == def.FinancialItemId);
        if (existingParent != null) continue;
        
        var financialItem = await _financialItemRepo.GetAsync(def.FinancialItemId);
        var parent = new CasualCourseFinancial {
            Id = GuidGenerator.Create(),
            TenantId = CurrentTenant.Id,
            CasualCourseId = casualCourseId,
            FinancialItemId = financialItem.Id,
            Source = DeriveSourceFromScenario(course.FundingScenario, financialItem),
            TotalAmountOMR = 0  // computed below
        };
        
        if (!financialItem.IsPerNominee)
        {
            // Flat item — single synthetic rank row
            var (rate, source) = await _rateResolver.ResolveRateAsync(financialItem.Id, rankId: null);
            var subtotal = _calculator.ComputeRankSubtotal(financialItem, rate, course.DurationDays, 1);
            parent.Ranks.Add(new CasualCourseFinancialItemRank {
                Id = GuidGenerator.Create(),
                TenantId = CurrentTenant.Id,
                CasualCourseFinancialId = parent.Id,
                RankId = Guid.Empty,
                NomineeCount = 1,
                RatePerUnitOMR = rate,
                SubtotalOMR = subtotal,
                RateSource = source
            });
            parent.TotalAmountOMR = subtotal;
        }
        else
        {
            // Per-nominee — one rank row per rank group
            foreach (var rankGroup in nomineesByRank)
            {
                var (rate, source) = await _rateResolver.ResolveRateAsync(financialItem.Id, rankGroup.Key);
                var subtotal = _calculator.ComputeRankSubtotal(financialItem, rate, course.DurationDays, rankGroup.Count());
                parent.Ranks.Add(new CasualCourseFinancialItemRank {
                    Id = GuidGenerator.Create(),
                    TenantId = CurrentTenant.Id,
                    CasualCourseFinancialId = parent.Id,
                    RankId = rankGroup.Key,
                    NomineeCount = rankGroup.Count(),
                    RatePerUnitOMR = rate,
                    SubtotalOMR = subtotal,
                    RateSource = source
                });
                parent.TotalAmountOMR += subtotal;
            }
        }
        
        await _financialRepo.InsertAsync(parent);
    }
    
    return new ListResultDto<CasualCourseFinancialDto>(/* mapped list */);
}
```

**Key rules preserved from the original:**
- Additive — skips rows where parent already exists
- Status guard — only runs when course is `UnderReview`
- `Source` derived from `FundingScenario` (unchanged logic)

### 4.2 `CasualCourseAppService.AssignScenarioAsync` — rewrite body

Receives a revised DTO shape that supports per-rank amounts:

```csharp
public class AssignScenarioDto
{
    public FundingScenario FundingScenario { get; set; }
    public List<AssignmentLineDto> FinancialItems { get; set; } = new();
    public bool Commit { get; set; }
}

public class AssignmentLineDto
{
    public Guid? Id { get; set; }   // null = new parent
    public Guid FinancialItemId { get; set; }
    public string? Notes { get; set; }
    public List<AssignmentRankLineDto> Ranks { get; set; } = new();  // ✅ NEW
}

public class AssignmentRankLineDto  // NEW
{
    public Guid? Id { get; set; }   // null = new rank row
    public Guid RankId { get; set; }  // Guid.Empty for flat items
    public int NomineeCount { get; set; }
    public decimal RatePerUnitOMR { get; set; }
    // SubtotalOMR computed server-side; never trust client
}
```

Algorithm:
1. For each `AssignmentLineDto`, upsert parent `CasualCourseFinancial`.
2. For each `AssignmentRankLineDto` under it, upsert child row; compute `SubtotalOMR` from `_calculator.ComputeRankSubtotal`.
3. Compute parent `TotalAmountOMR = SUM(children.SubtotalOMR)`.
4. Course-level `EstimatedTotalCost = SUM(all parents.TotalAmountOMR)`.
5. If `Commit == true`, transition `UnderReview → StaffReviewed` and enforce `EstimatedTotalCost > 0`.

### 4.3 New endpoint — `UpdateRankRateAsync`

Adds a single-cell update for the inline rate edits on PAGE 4.3:

| Method | Route | Purpose |
|---|---|---|
| `PUT` | `/api/app/casual-course-financial-item-ranks/{id}/rate` | Inline edit of one rank row's `RatePerUnitOMR`. Recomputes that row's `SubtotalOMR`, parent's `TotalAmountOMR`, and course's `EstimatedTotalCost`. Returns the updated parent for UI hydration. |

DTO:
```csharp
public class UpdateRankRateDto
{
    public decimal RatePerUnitOMR { get; set; }
}
```

Permission: `CasualCourses.Review`. State guard: parent course must be `UnderReview`.

**Why a dedicated endpoint:** Staff's UX is rapid inline editing (like Excel). Routing each cell change through `AssignScenarioAsync` sends a whole assignment DTO for every keystroke debounce — slow and chatty. Phase 3 Changes used the same inline-edit endpoint pattern for plan item ranks.

### 4.4 Deprecated endpoint — `UpdateAmountAsync`

The old `PUT /api/app/casual-course-financials/{id}/amount` (which edited the flat `EstimatedAmountOMR`) no longer makes sense. Two choices:

**Option X:** Delete it outright. Cleaner, but breaks any direct API consumers.
**Option Y:** Keep it as a convenience — interpret the amount as "divide equally across existing rank rows." Backward compatible but confusing.

**Go with Option X.** No external API consumers exist (internal-only system), and leaving a confusingly-named endpoint is worse than a clean break.

### 4.5 Extend `CasualCourseFinancialDto`

```csharp
public class CasualCourseFinancialDto : FullAuditedEntityDto<Guid>
{
    public Guid CasualCourseId { get; set; }
    public Guid FinancialItemId { get; set; }
    public string FinancialItemNameAr { get; set; }
    public bool IsPerDay { get; set; }
    public bool IsPerNominee { get; set; }
    public int EffectiveDays { get; set; }
    public decimal TotalAmountOMR { get; set; }    // ✅ renamed
    public decimal? ActualAmountOMR { get; set; }
    public FinancialAmountSource Source { get; set; }
    public string? Notes { get; set; }
    public List<CasualCourseFinancialItemRankDto> Ranks { get; set; } = new();  // ✅ new
}

public class CasualCourseFinancialItemRankDto : EntityDto<Guid>
{
    public Guid RankId { get; set; }
    public string RankNameAr { get; set; }  // joined from HR for display
    public int NomineeCount { get; set; }
    public decimal RatePerUnitOMR { get; set; }
    public decimal SubtotalOMR { get; set; }
    public string RateSource { get; set; }
}
```

Update Mapperly mapper classes accordingly — add `CasualCourseFinancialItemRankToDtoMapper` and update the parent mapper to include children.

---

## 5. Frontend changes — PAGE 4.3 rewrite

### 5.1 Structural rewrite

Existing `casual-course-review.component` has a flat financial table. Replace with the Phase 3 pattern: accordion per financial item, with expandable rank rows beneath.

**Reference:** open `docs/GTMS-Phase3-Changes-v2-Mockup.html` and view the `plan-review` screen (line 691 onward). Use its structure as the template. The casual course version is simpler in some ways (no unit accordion — only one course per page) but identical in financial panel structure.

### 5.2 Layout shape

```
PAGE 4.3 (NEW)
├── Header (unchanged)
├── Panel 1 — Course summary (unchanged, read-only)
├── Panel 2 — Nominations (unchanged, with condition pills)
├── Panel 3 — Funding Scenario cards (unchanged)
├── Panel 4 — Progress stats strip (NEW)
│   ├── Items total: N
│   ├── Items complete: X / N + progress bar
│   ├── Items missing cost: Y (with "jump to next" button)
│   └── Grand total: SUM(TotalAmountOMR) OMR
├── Panel 5 — Financial Items (REWRITTEN)
│   ├── "Auto-fill from defaults" button (page-level)
│   └── Per item:
│       ├── Parent row — item name, IsPerDay/IsPerNominee badges, effective-days chip, TotalAmountOMR, expand toggle, item-level ">> Return" button, item-level Delete
│       └── Expanded panel (when open):
│           ├── Effective-days explainer ("14 + 1 + 1 = 16")
│           ├── Rank table — Rank / NomineeCount (read-only, from course) / Rate (editable) / Effective days (read-only) / Subtotal
│           ├── Item Notes (editable)
│           └── "Return this item only" button
├── Panel 6 — Notes drawer trigger (unchanged)
└── Action bar — Save Review Draft / Finalize Review / Return to UTM / Reject (unchanged)
```

### 5.3 Per-rank rate inline edit

Each rate input fires `UpdateRankRateAsync` on debounce (400ms). On success:
- Update local signal for that rank row's `subtotalOMR`
- Update parent's `totalAmountOMR` signal
- Update grand-total signal
- No full reload needed — backend returns the updated parent; frontend maps into signals

On error (e.g., rate < 0, or state transition during the call):
- Rollback the cell to previous value
- Show error toast with localized message
- Don't let the edit proceed

### 5.4 Defensive rendering

- Financial item with `ranks.length === 1 && ranks[0].rankId === Guid.Empty` → render as plain flat row (no accordion affordance — it's a flat item)
- Financial item with `ranks.length > 1` → render with accordion header + rank table
- Rank row with `nomineeCount === 0` → shouldn't exist, but filter defensively if it does

### 5.5 Files touched

```
src/app/training/casual-courses/review/casual-course-review.component.ts     (rewrite)
src/app/training/casual-courses/review/casual-course-review.component.html   (rewrite)
src/app/training/casual-courses/review/casual-course-review.component.scss   (extend)
src/app/training/casual-courses/services/casual-course-review.service.ts     (add updateRankRate method)
```

---

## 6. Seeder updates

**File:** `src/YourApp.Domain/Data/TenantDataSeeder.cs` (or wherever casual courses are seeded).

The demo casual courses at `UnderReview` stage currently have flat financials. Update to:
- Populate rank rows for each financial item
- Use realistic mixed ranks (e.g., 1 Captain + 2 Sergeants on the Cyber Security course)
- Distribute rates sensibly: Captain gets higher rate on travel allowance, Sergeant gets lower
- Include at least one `RateSource = "DefaultAmount"` row and one `RateSource = "RankOverride"` row for coverage

Also verify the new migration's data backfill produces sensible starting values for pre-existing seeded casual courses.

---

## 7. Verification checklist

**Backend**
- [ ] `TrnCasualCourseFinancialItemRanks` table exists with correct schema + FK + unique index
- [ ] `CasualCourseFinancials.EstimatedAmountOMR` renamed to `TotalAmountOMR`
- [ ] Migration backfill runs cleanly on all 3 demo tenants (no orphan rows, no NULL violations)
- [ ] `CasualCourseFinancial.Ranks` nav populates correctly
- [ ] `AutoFillFromDefaultsAsync` creates parent + rank rows atomically
- [ ] `AssignScenarioAsync` accepts new DTO shape with per-rank assignments
- [ ] `UpdateRankRateAsync` endpoint works, recomputes subtotal and parent total
- [ ] `UpdateAmountAsync` endpoint removed (HTTP 404 on old route)
- [ ] `EstimatedTotalCost` on course row = SUM of all rank subtotals (verify with SQL)
- [ ] TD/TH cost gate still blocks when `EstimatedTotalCost <= 0`
- [ ] `CasualCourseFinancialDto` includes `Ranks[]` array with rank names joined from HR
- [ ] Mapperly mappers added/updated for rank DTO

**Frontend**
- [ ] Proxy regenerated — `Ranks[]` visible in generated types
- [ ] PAGE 4.3 renders progress stats strip correctly
- [ ] Financial items accordion expand/collapse works
- [ ] Per-rank rate inline edit debounces 400ms, fires `UpdateRankRateAsync`, updates totals
- [ ] Subtotal recomputes correctly on rate change
- [ ] Parent total + grand total update on any child change
- [ ] Effective-days chip shows "X + Y + Z = N" formula breakdown
- [ ] Flat items (single synthetic rank row) render without accordion
- [ ] Item-level Return + Delete buttons render only in `UnderReview` state
- [ ] "Auto-fill from defaults" button runs and additively inserts missing items + ranks
- [ ] "Jump to next incomplete" button finds the first financial item with `totalAmountOMR === 0`

**Cross-cutting**
- [ ] UGM rank-aware preview (from v4.5.1) still works — no regression
- [ ] Annual plan review (PAGE 3.3) completely untouched and working
- [ ] Seeded casual courses show realistic rank breakdowns on PAGE 4.3
- [ ] `docs/GTMS-Project-Checkpoint-v4_5_2.md` written with this patch's entry

---

## 8. Non-regression

- **Do not touch `PlanItemFinancialItemRanks`, `PlanItemFinancialItemAppService`, or PAGE 3.3.** This patch is additive to the casual course path only. Annual plan continues to work identically.
- **Do not change `FinancialItemDefaultResolver`.** Its signature is stable; both preview and review use it.
- **Do not change Phase 4B's planned shape.** `CoursePayment`, `TravelAllowancePayment`, `BudgetReallocation` are downstream consumers — they'll read the new rank rows naturally. No pre-wiring needed in 4A.
- **Do not change the rank-aware preview endpoint** (`/estimate-preview`). That's a calculator; it remains DB-free.
- **`CasualCourse` enum values and state machine unchanged.**

---

## 9. Commit / checkpoint

Single commit message suggested:
```
feat(casual-courses): per-rank financial breakdown on PAGE 4.3 (reverses Round-1 decision #2)

- New table TrnCasualCourseFinancialItemRanks (mirrors PlanItemFinancialItemRanks)
- Renames CasualCourseFinancials.EstimatedAmountOMR → TotalAmountOMR
- Migration backfills rank rows from existing flat rows (best-effort, Staff can correct)
- AutoFillFromDefaultsAsync now creates parent + rank children atomically
- AssignScenarioAsync accepts per-rank DTO shape
- New UpdateRankRateAsync endpoint for inline rate edits
- PAGE 4.3 rebuilt to match PAGE 3.3 shape (accordion + rank table + inline rate edit)
- Seeder updates: mixed-rank demo data with RankOverride/DefaultAmount coverage

Closes #<issue>
```

Append to checkpoint (v4.5.1 → v4.5.2) under "Architectural Reversals":

| # | Decision | Original | New | Reason |
|---|---|---|---|---|
| R1 | Round-1 #2 — casual course financial breakdown | Item-level only, no per-rank rows | Per-rank rows via new `CasualCourseFinancialItemRanks` table (mirrors annual plan) | Mixed-rank casual courses couldn't compute `EstimatedTotalCost` correctly because one rate couldn't cover multiple rank rates. Same shape as the annual plan, same resolver, same UX. |

---

## 10. Questions to confirm before starting

If any unclear, stop and ask:

1. **Does `PlanItemFinancialCalculator` exist as a shared domain service?** If yes, consider refactoring it to accept either a `PlanItemFinancial` or `CasualCourseFinancial` via a shared interface (`IFinancialItemWithRanks`). If no, create `CasualCourseFinancialCalculator` in parallel shape.
2. **HR Rank join** — is the `Rank` entity accessible through `WithDetailsAsync` on `CasualCourseFinancialItemRank`? Confirm the EF config pattern used for `PlanItemFinancialItemRank → Rank` and copy it.
3. **Data migration strategy** — for existing casual courses already in `UnderReview` or further states, is it acceptable to backfill rank rows by dividing the old `EstimatedAmountOMR` evenly across ranks? Or should the migration set all rank rows to 0 and require Staff to re-enter?
4. **Auto-fill vs Staff manual entry** — when Staff opens a casual course for review and clicks "Auto-fill", should the new rank rows be generated from `CourseTypeFinancialItemDefaults` with per-rank resolution (as specified in §4.1), or from Staff's last known rates for similar courses? Go with the defaults resolution — consistent with the preview.

---

*End of Patch Prompt — v1.0*
*Estimated effort: ~3 hours Claude Code work. Migration is the riskiest step — test on all 3 demo tenants before merge.*
*Target file count: ~15 backend changes, ~5 frontend changes, 1 migration, 7+ unit tests.*
