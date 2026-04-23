# GTMS — Phase 4A Implementation Prompt
## Casual Courses (Foundation)

**Version:** v1.2 (adds default cost preview for UTM + UGM — §3.6, endpoint #2)
**Date:** April 23, 2026
**Base:** GTMS v4.4 (Phase 3 + Phase 3 Changes complete)
**Target:** v4.5
**Companion docs:**
- `GTMS-Project-Checkpoint-v4_4.md` — project state recovery
- `GTMS-BRD-v4_0.md` §17.2, §19A — casual course workflow + reallocation rules
- `GTMS-ERD-v4_0.html` — CasualCourses / CasualCourseFinancials / CasualCourseNominations schemas
- `GTMS-API-Docs-v4_0.html` §6 — casual course endpoints
- `GTMS-Workflows-v4_0.html` — Workflow 3 (Casual, 6→5 steps after MOD-17)
- `GTMS-New-Screens-v4_0.html` — Casual Course Request mockup
- `GTMS-MOD-17-UTM-Replaces-DM.md` — 5-step workflow after DM removal
- `GTMS-Phase3-Changes-Implementation-Prompt.md` — nomination/return/rank patterns to reuse

---

## 1. Scope Summary

Phase 4A delivers the casual course lifecycle — one-off training requests submitted outside the annual plan. Scope is **foundational only** (no payments, no reallocation, no shared center requests — those land in 4B and 4C).

| Area | Count |
|---|---|
| New tables | 3 (`TrnCasualCourses`, `TrnCasualCourseFinancials`, `TrnCasualCourseNominations`) |
| New enums | 2 (`CasualCourseStatus`, `FundingScenario`) |
| Modified enums | 1 (`NoteEntityType` — adds `CasualCourse`, `CasualCourseNomination`) |
| New AppServices | 3 (`CasualCourseAppService`, `CasualCourseFinancialAppService`, `CasualCourseNominationAppService`) |
| Modified domain services | 2 (`NominationConditionValidator` + 1 sibling; `CourseNameResolver` — small) |
| New domain services | 1 (`CasualCourseValidator`) |
| Reused domain services (key) | `FinancialItemDefaultResolver` — drives both Staff auto-fill and the new default-cost preview endpoint |
| New API endpoints | **25** |
| New frontend pages | 4 (List, Request Form, Review, Approval) |
| Reused shared components | 3 (Notes drawer, Return modal, Nomination picker) |

**Out of scope (Phase 4B / 4C):**
- `TravelAllowancePayments`, `CoursePayments` — Phase 4B
- **`PriceQuotes` for casual courses — Phase 4B.** Executed as the post-approval procurement track once a casual course reaches `THApproved`. Implementation makes `PriceQuotes.SessionId` nullable and adds `PriceQuotes.CasualCourseId?` FK with a check constraint that exactly one is set (polymorphic reuse of the Phase 3 entity — no new quote table).
- `BudgetReallocations` auto-generation — Phase 4B (triggered by payment confirmation)
- `SharedCenterCourseRequests` — Phase 4C (standalone, cross-entity)

**Phase 4A → Phase 4B handoff column:** `CasualCourses.SelectedPriceQuoteId?` is declared in the Phase 4A schema but stays null throughout Phase 4A. Phase 4B writes it when Staff picks the winning quote post-approval. Declaring it here keeps the Phase 4A migration stable and avoids a second alter-table in 4B.

---

## 2. Locked Decisions

**Round 1 — confirm before implementation:**

1. ✅ **Return loop parity with Phase 3 Changes.** Extend `AppPlanNotes.EntityType` to include `CasualCourse` and `CasualCourseNomination`. Reuse `NotesDrawerComponent`, `ReturnModalComponent`, `NominationPickerComponent`. Add `IsReturned` + `LastReturnNoteId` on both `CasualCourse` and `CasualCourseNomination`.
2. ✅ **Item-level financial breakdown only** — no per-rank rows for casual courses. CHG-07 formula still applied (per-day × per-nominee × extra days). If per-rank becomes needed, added as a follow-up.
3. ✅ **Nebras balance display is placeholder** — read-only card, no external call, no integration work.
4. ✅ **Cost gate on both TD and TH approval** — same pattern as `TrainingPlanAppService.FinalApproveAsync`. Staff's `AssignScenarioAsync` writes `EstimatedTotalCost`.
5. ✅ **Unit-scope filter via `EmployeeResolver.IsCurrentUserUnitScopedAsync`** — UTM/UGM see only their unit's casual courses.
6. ✅ **Condition validation at creation** via new sibling `NominationConditionValidator.ValidateByTenantCourseAsync(tenantCourseId, employeeId)` — reads `TenantCourseConditions` (not `PlanItemConditions`). Diff-based nominee updates in `Draft` / `ReturnedToCreator`.
7. ✅ **No reallocation records in Phase 4A** — `FundingScenario` is stored on the casual course, reallocation generation hooks off Finance Officer payment confirmation in Phase 4B.
8. ✅ **`CasualCourseStatus` enum:** `Draft (0)`, `Submitted (1)`, `UGMApproved (2)`, `UnderReview (3)`, `StaffReviewed (4)`, `TDApproved (5)`, `THApproved (6)`, `ReturnedToCreator (7)`, `Rejected (8)`. `UnderReview` lives between `UGMApproved` and `StaffReviewed` so Staff has an explicit "Start Review" transition.

---

## 3. Critical Rules

### 3.1 FundingSource requirement

- **Required when `CourseType ∈ { ExternalInternational, ExternalLocal }`.** Validated in `CasualCourseValidator.ValidateForSubmitAsync`.
- **Optional for `Internal`** (internal casual courses don't consume external budget).
- Free-text field — no format validation. Example: `wew-232-343-2` (Nebras project code).

### 3.2 Cost gate

```
At TDApproveAsync and HeadApproveAsync:
  If CasualCourse.EstimatedTotalCost IS NULL or <= 0
     THROW BusinessException("Training:CasualCourse:CostRequired")
```

Staff MUST call `AssignScenarioAsync` (which writes `EstimatedTotalCost`) before Staff can move status to `StaffReviewed`.

### 3.3 CHG-07 formula applied to `CasualCourseFinancial.EstimatedAmount`

```
effectiveDays   = IsPerDay     ? (DurationDays + ExtraDaysBefore + ExtraDaysAfter) : 1
effectiveCount  = IsPerNominee ? NomineeCount                                       : 1
EstimatedAmount = RatePerUnit * effectiveDays * effectiveCount
```

Where `RatePerUnit` resolves via `FinancialItemDefaultResolver.ResolveRateAsync(itemId, rankId: null)` — falls straight to `DefaultAmountOMR` since there's no rank resolution for casual (item-level only).

Staff can override `EstimatedAmount` directly per line — same DX as plan-item PIFI but flatter (no rank grid).

### 3.4 Status transitions (state machine)

```
                       ┌─────────────────────────────────────────┐
                       │                                         │
                       │   Return at any Staff/TD/TH step        │
                       ▼                                         │
  Draft ──Submit──▶ Submitted ──UGMApprove──▶ UGMApproved ──StartReview──▶ UnderReview
   ▲                                                                             │
   │                                                                             │ AssignScenario
   │                                                                             │ + items
   └─────────── (UTM edits) ◀── ReturnedToCreator ◀───────Return─────────────────┤
                                        │                                        │
                                   Resubmit                                      ▼
                                        │                                   StaffReviewed
                                        ▼                                        │
                                  UnderReview                              TDApprove (cost gate)
                                                                                 ▼
                                                                             TDApproved
                                                                                 │
                                                                            HeadApprove (cost gate)
                                                                                 ▼
                                                                             THApproved ✅
```

**Rejected** is a terminal stop at any approval stage (distinct from `ReturnedToCreator` which expects a fix-and-resubmit loop).

**After `THApproved` — Phase 4B post-approval tracks (for reference, not implemented in 4A):**
- **Track A — Price Quotes:** Staff collects N provider quotes via polymorphic `PriceQuotes` (FK to `CasualCourseId`), picks winner, writes `SelectedPriceQuoteId` back to the casual course. Winner's price may update `EstimatedTotalCost` or trigger a variance flag.
- **Track B — Travel Instructions:** Nebras handoff for external courses (manual/future).
- **Track C — Course Payment:** Finance Officer confirms invoice; triggers `BudgetReallocation` generation when `FundingScenario ∈ {2, 3}`.

Phase 4A never invokes these. The only anticipatory wiring is the nullable `SelectedPriceQuoteId` column.

### 3.5 Unit scope

Same helper as Phase 3 Changes (`EmployeeResolver.IsCurrentUserUnitScopedAsync`):
- UTM + UGM + DM → `CasualCourse.UnitId == currentUser.MainUnitId`
- Staff + TD + TH + Finance → all tenant rows

Applied on every `GetListAsync` / `GetAsync` / `UpdateAsync` / `DeleteAsync` in `CasualCourseAppService`.

### 3.6 Default cost preview (UTM + UGM visibility)

**Problem:** at UTM form time and UGM approval time, no financial items have been assigned yet — Staff's `AssignScenarioAsync` runs at step 3. Both roles need a reasonable cost projection to make a decision.

**Solution:** a pure calculator endpoint `POST /api/app/casual-courses/estimate-preview` that accepts `(tenantCourseId, courseType, durationDays, nomineeCount)` and returns the default item breakdown + total by:

1. Reading `CourseTypeFinancialItemDefaults` for the given `CourseType` and tenant.
2. For each default `FinancialItem`, computing the subtotal via `FinancialItemDefaultResolver.ComputeSubtotal(rate, isPerDay, isPerNominee, courseDays, extraBefore, extraAfter, nomineeCount)` — same formula as the real auto-fill.
3. Summing to a grand total.

**No DB writes. No permission beyond authenticated user.** Reusable across phases (could be generalized to annual plans later — not in scope for 4A).

**Frontend contract:**
- PAGE 4.2 (UTM form) calls `estimate-preview` on every change to `CourseType`, `DurationDays`, or nominee count (debounced 400ms). Renders result in Section 6.5.
- PAGE 4.4 UGM variant calls it once on page load. Renders result read-only.
- PAGE 4.3 (Staff review) does **not** use it — Staff sees actual `CasualCourseFinancials` rows instead.
- PAGE 4.4 TD / TH variants do **not** use it — they see `EstimatedTotalCost` (the gate value written by Staff).

**Messaging rule (non-negotiable):** every render of the preview must include the caption:
> "This is a projection from default items. The authoritative cost comes from Staff's financial-item assignment in step 3."

This prevents anyone treating the preview as a commitment. The cost gate at TD + TH checks `EstimatedTotalCost`, never the preview result.

---

## 4. Schema Changes

### 4.1 New Table — `TrnCasualCourses`

| Field | Type | Required | Notes |
|---|---|---|---|
| `Id` | `Guid` PK | yes | |
| `TenantId` | `Guid` FK | yes | `IMultiTenant` |
| `TenantCourseId` | `Guid` FK → `TenantCourses` | yes | Replaces pre-v4 `CatalogCourseId` |
| `UnitId` | `Guid` FK → `AbpOrganizationUnit` | yes | Resolved from `Employee.MainUnitId` of requester |
| `RequestedById` | `Guid` FK → `AbpUsers` | yes | UTM user (MOD-17) |
| `CourseType` | enum | yes | `ExternalInternational` / `ExternalLocal` / `Internal` |
| `Priority` | `int` | yes | 1 (highest) – 5 |
| `Justification` | `string(500)` | yes | |
| `DescriptionAr` | `string(1000)` | no | Defaults from `TenantCourse.DescriptionAr` — editable |
| `ObjectivesAr` | `string(1000)` | no | Defaults from `TenantCourse.ObjectivesAr` — editable |
| `DurationYears` | `int` | no | Default 0 |
| `DurationMonths` | `int` | no | Default 0 |
| `DurationDays` | `int` | yes | ≥ 1 |
| `EstimatedDateFrom` | `DateTime` | yes | |
| `EstimatedDateTo` | `DateTime` | yes | |
| `FundingSource` | `string(200)` | conditional | Required when `CourseType != Internal` |
| `EstimatedTotalCost` | `decimal(18,3)?` | no | Set by Staff in `AssignScenarioAsync`; cost gate on TD/TH |
| `FundingScenario` | enum? | no | 1 / 2 / 3 — set by Staff |
| `SelectedPriceQuoteId` | `Guid?` FK → `PriceQuotes` | no | **Declared in Phase 4A, written in Phase 4B.** Null until Staff picks the winning quote post-approval. |
| `Status` | enum | yes | `CasualCourseStatus` (default `Draft`) |
| `IsReturned` | `bool` | yes | Default false |
| `LastReturnNoteId` | `Guid?` FK → `AppPlanNotes` | no | Null when not returned |
| `RejectedReason` | `string(500)?` | no | Set when status `Rejected` |

**Unique index:** `(TenantId, TenantCourseId, EstimatedDateFrom)` — optional, prevents duplicate concurrent requests for the same course/date.

**Navs:**
- `TenantCourse` (many-to-one)
- `Unit` (many-to-one, through `AbpOrganizationUnit`)
- `Financials` (one-to-many → `CasualCourseFinancials`)
- `Nominations` (one-to-many → `CasualCourseNominations`)

### 4.2 New Table — `TrnCasualCourseFinancials`

| Field | Type | Required | Notes |
|---|---|---|---|
| `Id` | `Guid` PK | yes | |
| `TenantId` | `Guid` FK | yes | |
| `CasualCourseId` | `Guid` FK | yes | Cascade delete |
| `FinancialItemId` | `Guid` FK → `TrnFinancialItems` | yes | Sub-item (leaf) |
| `EstimatedAmountOMR` | `decimal(18,3)` | yes | Computed via CHG-07 formula at auto-fill; editable by Staff |
| `ActualAmountOMR` | `decimal(18,3)?` | no | Populated later by Finance Officer (Phase 4B) |
| `Source` | enum | yes | `FundingSource` / `FinancialItem` — driven by `FundingScenario` |
| `Notes` | `string(500)?` | no | |

**Unique index:** `(CasualCourseId, FinancialItemId)`.

**Auto-fill rule:** when `AssignScenarioAsync` runs, Staff's `financialItemAssignments[]` populates this table. If Staff asks for auto-fill, the `CourseTypeFinancialItemDefaults` table (MOD-15, already seeded) drives which items appear; `FinancialItemDefaultResolver.ComputeSubtotal` drives the starting amount. Auto-fill is **additive** (never destructive on existing rows) — same rule as PAGE 3.3.

### 4.3 New Table — `TrnCasualCourseNominations`

| Field | Type | Required | Notes |
|---|---|---|---|
| `Id` | `Guid` PK | yes | |
| `TenantId` | `Guid` FK | yes | |
| `CasualCourseId` | `Guid` FK | yes | Cascade delete |
| `EmployeeId` | `Guid` FK → HR | yes | Read-only FK; condition-validated at add |
| `IsReturned` | `bool` | yes | Default false |
| `LastReturnNoteId` | `Guid?` FK → `AppPlanNotes` | no | |
| `ConditionSnapshotJson` | `string(max)?` | no | Cached condition-check result at nomination time (for audit) |

**Unique index:** `(CasualCourseId, EmployeeId)` — one nomination per employee per course.

### 4.4 Enum additions

**`CasualCourseStatus` (new, `Domain.Shared/Training/Enums/`):**
```csharp
Draft            = 0,
Submitted        = 1,
UGMApproved      = 2,
UnderReview      = 3,
StaffReviewed    = 4,
TDApproved       = 5,
THApproved       = 6,
ReturnedToCreator = 7,
Rejected         = 8
```

**`FundingScenario` (new):**
```csharp
FundingSourceCoversAll    = 1, // No reallocation
FundingSourceCoversCourse = 2, // Travel items via reallocation
FinancialItemsCoverAll    = 3  // Full reallocation
```

**`FinancialAmountSource` (new — used by `CasualCourseFinancial.Source`):**
```csharp
FundingSource = 0,
FinancialItem = 1
```

**`NoteEntityType` (modify existing in Phase 3 Changes):**
```csharp
Plan              = 0,
PlanItem          = 1,
Nomination        = 2,
CasualCourse      = 3, // NEW
CasualCourseNomination = 4 // NEW
```

### 4.5 Migration

Single migration: `v4_5_Phase4A_CasualCourses`
- Creates 3 tables with FKs and indexes above
- Adds 2 new enum values to `NoteEntityType` (no schema change — enum stored as int)
- No destructive changes to existing tables

---

## 5. Domain Services

### 5.1 NEW — `CasualCourseValidator`

Responsibilities:
- `ValidateForSubmitAsync(casualCourseId)` — called on `Submit`:
  - Status must be `Draft` or `ReturnedToCreator`
  - `DurationDays >= 1`
  - `EstimatedDateFrom <= EstimatedDateTo`
  - `FundingSource` required when external course type
  - At least one nomination
  - All nominations pass `NominationConditionValidator.ValidateByTenantCourseAsync`
- `ValidateForTDApprovalAsync(casualCourseId)` — called on `TDApprove`:
  - Status must be `StaffReviewed`
  - `EstimatedTotalCost > 0`
  - `FundingScenario` set
  - No `IsReturned` on course or any nomination
- `ValidateForHeadApprovalAsync(casualCourseId)` — called on `HeadApprove`:
  - Status must be `TDApproved`
  - `EstimatedTotalCost > 0` (double-check)

### 5.2 UPDATED — `NominationConditionValidator`

Add sibling method:
```csharp
Task<ValidationResult> ValidateByTenantCourseAsync(Guid tenantCourseId, Guid employeeId);
```

Reads `TenantCourseConditions` (not `PlanItemConditions` / `SessionConditions`). Returns same structured result (9 real condition types).

### 5.3 UNCHANGED — reused as-is

- `EmployeeResolver` — use `.IsCurrentUserUnitScopedAsync()` + `.ResolveUnitIdAsync(userId)`
- `CourseNameResolver` — used by frontend for display in lists
- `FinancialItemDefaultResolver` — used by Staff auto-fill of `CasualCourseFinancials`
- `BudgetRecalculator` — NOT invoked in Phase 4A (casual does not alter `TrainingBudget.TotalAmount` per MOD-23; reallocation in Phase 4B is separate)

---

## 6. AppServices

### 6.1 NEW — `CasualCourseAppService` (main lifecycle)

Extends `CrudAppService<CasualCourse, CasualCourseDto, Guid, CasualCourseGetListInput, CreateUpdateCasualCourseDto>`.

| Endpoint | Route | Role | Notes |
|---|---|---|---|
| `CreateAsync(CreateUpdateCasualCourseDto)` | `POST /api/app/casual-courses` | UTM | Requires ≥ 1 nomination; runs condition validator on each |
| `GetEstimatePreviewAsync(EstimatePreviewInput)` | `POST /api/app/casual-courses/estimate-preview` | all (authenticated) | **Pure calculator — no DB write.** Reads `CourseTypeFinancialItemDefaults` for `CourseType`, applies CHG-07 via `FinancialItemDefaultResolver.ComputeSubtotal`, returns per-item breakdown + total. Used live on PAGE 4.2 (UTM form) and statically on UGM approval page. |
| `GetListAsync(CasualCourseGetListInput)` | `GET /api/app/casual-courses` | all | Unit-scoped filter applied |
| `GetAsync(id)` | `GET /api/app/casual-courses/{id}` | all | WithDetails: TenantCourse + Financials + Nominations |
| `UpdateAsync(id, CreateUpdateCasualCourseDto)` | `PUT /api/app/casual-courses/{id}` | UTM | Only in `Draft` / `ReturnedToCreator`; diff-based nominees |
| `DeleteAsync(id)` | `DELETE /api/app/casual-courses/{id}` | UTM | Only in `Draft` — cascade to children |
| `SubmitAsync(id)` | `POST /api/app/casual-courses/{id}/submit` | UTM | `Draft → Submitted` (or `ReturnedToCreator → UnderReview` on resubmit) |
| `UGMApproveAsync(id, NoteDto?)` | `POST /api/app/casual-courses/{id}/ugm-approve` | UGM | `Submitted → UGMApproved` |
| `StartReviewAsync(id)` | `POST /api/app/casual-courses/{id}/start-review` | Staff | `UGMApproved → UnderReview` |
| `AssignScenarioAsync(id, AssignScenarioDto)` | `PUT /api/app/casual-courses/{id}/assign-scenario` | Staff | Writes `FundingScenario` + `EstimatedTotalCost` + upserts financial assignments; sets `Source` per scenario; `UnderReview → StaffReviewed` |
| `TDApproveAsync(id, NoteDto?)` | `POST /api/app/casual-courses/{id}/td-approve` | TD | Cost gate; `StaffReviewed → TDApproved` |
| `HeadApproveAsync(id, NoteDto?)` | `POST /api/app/casual-courses/{id}/head-approve` | TH | Cost gate; `TDApproved → THApproved` |
| `ReturnAsync(id, ReturnDto)` | `POST /api/app/casual-courses/{id}/return` | UGM/Staff/TD/TH | Status → `ReturnedToCreator`; writes note; sets `IsReturned` |
| `ResubmitAsync(id)` | `POST /api/app/casual-courses/{id}/resubmit` | UTM | Only when `ReturnedToCreator`; clears `IsReturned` on course + nominations; status resumes at the stage that returned |
| `RejectAsync(id, RejectDto)` | `POST /api/app/casual-courses/{id}/reject` | UGM/Staff/TD/TH | Terminal; writes reason + status → `Rejected` |

**DTO naming:** `CasualCourseGetListInput`, `CreateUpdateCasualCourseDto`, `AssignScenarioDto`, `ReturnDto`, `RejectDto`, `NoteDto` (reuse from Phase 3 Changes where possible).

**Permissions:**
```csharp
public static class CasualCourses
{
    public const string Default     = GroupName + ".CasualCourses";
    public const string Create      = Default + ".Create";
    public const string Edit        = Default + ".Edit";
    public const string Delete      = Default + ".Delete";
    public const string Submit      = Default + ".Submit";
    public const string Approve     = Default + ".Approve";    // UGM
    public const string Review      = Default + ".Review";     // Staff
    public const string TDApprove   = Default + ".TDApprove";
    public const string HeadApprove = Default + ".HeadApprove";
    public const string Return      = Default + ".Return";
    public const string Reject      = Default + ".Reject";
}
```

### 6.2 NEW — `CasualCourseFinancialAppService`

| Endpoint | Route | Role | Notes |
|---|---|---|---|
| `GetListByCasualCourseAsync(casualCourseId)` | `GET /api/app/casual-course-financials` | Staff/TD/TH/UTM (read) | Ordered by `FinancialItem.SortOrder` |
| `AutoFillFromDefaultsAsync(casualCourseId)` | `POST /api/app/casual-course-financials/auto-fill` | Staff | Additive; reads `CourseTypeFinancialItemDefaults`; applies CHG-07 |
| `UpdateAmountAsync(id, UpdateAmountDto)` | `PUT /api/app/casual-course-financials/{id}/amount` | Staff | Inline edit of `EstimatedAmountOMR` |
| `AddItemAsync(casualCourseId, AddItemDto)` | `POST /api/app/casual-course-financials` | Staff | Add custom item outside defaults |
| `DeleteItemAsync(id)` | `DELETE /api/app/casual-course-financials/{id}` | Staff | Only while `UnderReview` |

Note: `EstimatedAmountOMR` is recomputed by `AssignScenarioAsync` if Staff sends bulk; individual `UpdateAmountAsync` is optimistic-UI from the Review page.

### 6.3 NEW — `CasualCourseNominationAppService`

| Endpoint | Route | Role | Notes |
|---|---|---|---|
| `GetListByCasualCourseAsync(casualCourseId)` | `GET /api/app/casual-course-nominations` | all | WithDetails: Employee, Rank, Position |
| `AddAsync(casualCourseId, employeeId)` | `POST /api/app/casual-course-nominations` | UTM | Runs condition validator; blocked if course not in `Draft` / `ReturnedToCreator` |
| `RemoveAsync(id)` | `DELETE /api/app/casual-course-nominations/{id}` | UTM | Same state guard |
| `ReturnAsync(id, ReturnDto)` | `POST /api/app/casual-course-nominations/{id}/return` | Staff | Nomination-level return |
| `ReplaceAsync(id, employeeId)` | `PUT /api/app/casual-course-nominations/{id}/replace` | UTM | Replace one nominee with another |

Diff-based updates invoked from `CasualCourseAppService.UpdateAsync` when `CreateUpdateCasualCourseDto.NomineeEmployeeIds` changes — same algorithm as `TrainingPlanItemAppService.UpdateAsync` (preserve untouched, remove absent, add new with condition validation).

### 6.4 MODIFIED — `PlanNoteAppService`

No behavioral change — the polymorphic table already supports it. Add two new enum values (`CasualCourse`, `CasualCourseNomination`) to the `EntityType` input validator.

---

## 7. Frontend — Pages

### 7.1 PAGE 4.1 — Casual Courses List (`/training/casual-courses`)

- **Layout:** same pattern as PAGE 3.1 (Annual Plans List) — card-per-row, hierarchical status chip, role-aware action column.
- **Filters:** Year, Status (multi-select), Unit (for non-scoped users), My Requests toggle.
- **Columns:** Course (from `TenantCourse.CatalogCourse.NameAr`), FundingSource, Unit, Requester, NomineesCount, EstimatedTotalCost (OMR), Status badge, Actions.
- **Status-to-Action mapping (explicit `@switch` — no `@default` fallback, per Phase 3 Changes lesson):**

| Status | Who acts | Action button |
|---|---|---|
| Draft | UTM | Edit / Submit / Delete |
| Submitted | UGM | Approve / Return / Reject |
| UGMApproved | Staff | Start Review |
| UnderReview | Staff | Open Review page |
| StaffReviewed | TD | Open Approval page |
| TDApproved | TH | Open Approval page |
| ReturnedToCreator | UTM | Edit (amber row highlight + banner) |
| THApproved | — | Read-only |
| Rejected | — | Read-only with reason tooltip |

- **Unit accordions** when Staff/TD/TH views it — reuse `showAccordions()` signal pattern.

### 7.2 PAGE 4.2 — Casual Course Request Form (`/training/casual-courses/new` + `/:id/edit`) — UTM

Matches the `GTMS-New-Screens-v4_0.html` "casual-updated" mockup layout. Multi-section:

**Section A — Course Details**
- TenantCourseId (select — active only) — on change, defaults DescriptionAr/ObjectivesAr
- CourseType (select)
- Priority (select 1–5)
- Justification (required textarea)
- DescriptionAr (editable, pre-filled)
- ObjectivesAr (editable, pre-filled)

**Section B — Duration & Dates**
- DurationYears / DurationMonths / DurationDays (number inputs)
- EstimatedDateFrom / EstimatedDateTo (date-box)
- Auto-validation: date range matches duration (warning, not hard block)

**Section C — FundingSource (conditional — external only)**
- FundingSource (text, required when external)
- Nebras balance placeholder card (static "رصيد FundingSource (من نبراس): يُعرض هنا عند ربط نبراس")

**Section D — Nominations (mandatory)**
- Reuse `NominationPickerComponent` from Phase 3 Changes
- Same condition-validation UX (pass/fail per employee)
- Counts auto-update: OfficersCount, EnlistedCount, Capacity (read-only)

**Section E — Actions**
- Save Draft (status stays `Draft`)
- Submit (calls `SubmitAsync`; runs `CasualCourseValidator`)
- Cancel

**Returned-state banner** at top when `status == ReturnedToCreator` — amber, shows latest return reason via `NotesDrawerComponent`.

### 7.3 PAGE 4.3 — Casual Course Review (`/training/casual-courses/:id/review`) — Staff

Matches PAGE 3.3 (Plan Review) pattern — flat (no rank grid).

**Panel 1 — Course Summary** (read-only — TenantCourse details, requester, dates, duration, nominations count).

**Panel 2 — Nominations** (table with condition-check status, nomination-level return action).

**Panel 3 — Funding Scenario** (radio group — 3 options, each with a one-line explanation and a reallocation-preview badge):
- Scenario 1 — FundingSource covers all → No reallocation
- Scenario 2 — FundingSource covers course / Financial items for travel → Auto-reallocation
- Scenario 3 — Financial items cover all → Auto-reallocation

**Panel 4 — Financial Items (flat table — no rank rows)**
- Auto-fill button (reads `CourseTypeFinancialItemDefaults`)
- Columns: Financial Item, Source (auto-set by scenario), Rate (read-only), Days (read-only, from `DurationDays + extras`), Nominee Count (read-only), Estimated Amount (editable), Notes
- Total row at bottom — writes `EstimatedTotalCost` on save
- Add / Remove item row controls (only while `UnderReview`)

**Panel 5 — Notes Drawer** (reuse).

**Actions:** Save Assignments (status stays `UnderReview`), Submit to TD (`UnderReview → StaffReviewed`), Return to Creator, Reject.

### 7.4 PAGE 4.4 — Casual Course Approval (`/training/casual-courses/:id/approve`) — TD / TH

Single page, role-aware:
- TD sees it when status `StaffReviewed`
- TH sees it when status `TDApproved`

Read-only everywhere except the action row. Reuses Notes drawer + Return modal.

**Cost gate:** Approve button disabled (greyed + tooltip) when `EstimatedTotalCost` is null or ≤ 0. Matches PAGE 3.4 pattern.

**Actions:** Approve, Return to Creator, Reject.

### 7.5 Shared components — reused (no changes)

- `NotesDrawerComponent` — left-slide drawer
- `ReturnModalComponent` — required reason, amber theme
- `NominationPickerComponent` — employee search + unit filter

Only change: the drawer's `scope` tabs gain two extra values — `CasualCourse` and `CasualCourseNomination`.

---

## 8. State Machine (Backend Source of Truth)

```
CreateAsync       : (none)               → Draft
SubmitAsync       : Draft                → Submitted
                  : ReturnedToCreator    → UnderReview (resubmit path)
UGMApproveAsync   : Submitted            → UGMApproved
StartReviewAsync  : UGMApproved          → UnderReview
AssignScenarioAsync: UnderReview         → StaffReviewed
TDApproveAsync    : StaffReviewed        → TDApproved    (cost gate)
HeadApproveAsync  : TDApproved           → THApproved    (cost gate)
ReturnAsync       : Submitted | UGMApproved | UnderReview | StaffReviewed | TDApproved
                                         → ReturnedToCreator
ResubmitAsync     : ReturnedToCreator    → <stage that returned>
RejectAsync       : Submitted | UGMApproved | UnderReview | StaffReviewed | TDApproved
                                         → Rejected      (terminal)
DeleteAsync       : Draft only           (cascade children)
```

Every transition is guarded by an explicit status check that throws `BusinessException("Training:CasualCourse:InvalidStatusTransition")` on mismatch.

---

## 9. Implementation Order

### 9.1 Backend (`Phase-4A-Backend` session)

1. Enums — `CasualCourseStatus`, `FundingScenario`, `FinancialAmountSource`; extend `NoteEntityType`
2. Entities — `CasualCourse`, `CasualCourseFinancial`, `CasualCourseNomination`
3. EF Configuration — `CasualCourseConfiguration.cs` + add DbSets
4. Permissions — `TrainingPermissions.CasualCourses`
5. Migration + seeder (3 casual courses per tenant, one at each major stage: Draft, UnderReview, THApproved)
6. DTOs + mappers (MapperBase pattern, class-based)
7. Domain service — `CasualCourseValidator`
8. `NominationConditionValidator.ValidateByTenantCourseAsync` sibling
9. AppService — `CasualCourseAppService` (all 13 endpoints)
10. AppService — `CasualCourseFinancialAppService`
11. AppService — `CasualCourseNominationAppService`
12. Extend `PlanNoteAppService` to accept new `EntityType` values
13. Unit tests — status transitions, cost gate, scenario write, diff-based nominees, unit-scope filter

### 9.2 Frontend (`Phase-4A-Frontend` session)

1. Generate proxies — `abp generate-proxy -t ng` (picks up all new endpoints + DTOs)
2. Routes + menu — add `Casual Courses` entry under Training menu
3. Feature module folder `src/app/training/casual-courses/`
4. PAGE 4.1 — list component (standalone, reuses card + status chip components)
5. PAGE 4.2 — request form component (multi-section; reuse NominationPicker)
6. PAGE 4.3 — review component (scenario radio + financials table — mirror PAGE 3.3 structure)
7. PAGE 4.4 — approval component (mirror PAGE 3.4)
8. Localization — `en.json` + `ar.json` entries for every new key
9. Add casual-course-note scopes to NotesDrawer
10. Smoke test against seeded data in Ground Forces / Air Forces / Naval Forces tenants

### 9.3 Validation Gate

- Zero inline styles in templates
- Zero getter arrays
- Zero `.bind(this)`
- All localization keys use `::Training.KeyName` in TS/HTML; dot-notation (no `::`) in JSON
- All `@switch` on status have explicit cases for all 9 enum values
- Every AppService method has explicit `authorize` permission check
- Every state transition throws `BusinessException` with a localizable error code
- Every AppService list method applies unit-scope filter

---

## 10. File Inventory

### Backend (~32 files)

```
Domain.Shared/Training/Enums/
  + CasualCourseStatus.cs
  + FundingScenario.cs
  + FinancialAmountSource.cs
  ~ NoteEntityType.cs (extend)

Domain.Shared/Training/Permissions/
  ~ TrainingPermissions.cs (+ CasualCourses class)
  ~ TrainingPermissionDefinitionProvider.cs (grant roles)

Domain/Training/CasualCourses/
  + CasualCourse.cs
  + CasualCourseFinancial.cs
  + CasualCourseNomination.cs

Domain/Training/Managers/
  + CasualCourseValidator.cs
  ~ NominationConditionValidator.cs (+ ValidateByTenantCourseAsync)

EntityFrameworkCore/Training/Configurations/
  + CasualCourseConfiguration.cs
  ~ TrainingDbContext.cs (+ 3 DbSets)

EntityFrameworkCore/Migrations/
  + v4_5_Phase4A_CasualCourses.cs

Application.Contracts/Training/CasualCourses/
  + Dtos/CasualCourseDto.cs
  + Dtos/CreateUpdateCasualCourseDto.cs
  + Dtos/CasualCourseGetListInput.cs
  + Dtos/AssignScenarioDto.cs
  + Dtos/CasualCourseFinancialDto.cs
  + Dtos/CreateUpdateCasualCourseFinancialDto.cs
  + Dtos/CasualCourseNominationDto.cs
  + ICasualCourseAppService.cs
  + ICasualCourseFinancialAppService.cs
  + ICasualCourseNominationAppService.cs

Application/Training/CasualCourses/
  + CasualCourseAppService.cs
  + CasualCourseFinancialAppService.cs
  + CasualCourseNominationAppService.cs
  + Mappers/CasualCourseToDtoMapper.cs
  + Mappers/CreateUpdateCasualCourseToEntityMapper.cs
  + Mappers/CasualCourseFinancialToDtoMapper.cs
  + Mappers/CreateUpdateCasualCourseFinancialToEntityMapper.cs
  + Mappers/CasualCourseNominationToDtoMapper.cs

Domain/Data/
  ~ GtmsDataSeeder.cs (+ 3 casual courses per tenant)

Localization/Training/
  ~ en.json (+ ~60 keys)
  ~ ar.json (+ ~60 keys)
```

### Frontend (~18 files)

```
src/app/training/casual-courses/
  + casual-courses.routes.ts
  + list/casual-courses-list.component.ts + .html + .scss
  + request/casual-course-request.component.ts + .html + .scss
  + review/casual-course-review.component.ts + .html + .scss
  + approval/casual-course-approval.component.ts + .html + .scss
  + services/casual-course.service.ts (wrapper around proxy)
  + models/casual-course-status.ts (enum map for badges)

src/app/training/shared/
  ~ notes-drawer/notes-drawer.component.ts (+ 2 scope tabs)

src/app/proxy/training/
  (auto-regenerated by abp generate-proxy)

src/app/app.routes.ts  (~ add loadChildren)
src/app/layout/side-menu.component.ts  (~ add menu item)
```

---

## 11. Delivery Protocol (Gate-by-Gate)

Follow the Phase 2B / Phase 3 / Phase 3 Changes rhythm:

1. **OmanAI approves this prompt** (Round-1 decisions locked, scope confirmed).
2. **API list gate** — before code, produce a table of all 25 endpoints (method, route, role, request DTO, response DTO). OmanAI approves.
3. **Page design gate** — produce interactive HTML mockups for PAGE 4.1–4.4 (match GTMS-Complete-UI-v2.html + v4.4 look). OmanAI approves.
4. **Backend implementation** (Claude Code against `MOD.TrainingLive` repo).
5. **Frontend implementation** (Claude Code).
6. **Smoke test** — run against seeded data; capture any bug list for a v4.5 follow-up pass.
7. **Checkpoint v4.5** — deliver `GTMS-Project-Checkpoint-v4_5.md`.

---

## 12. Non-negotiable Patterns (Carry Forward)

- **Mapperly class-based** `MapperBase<TSource, TTarget>` — NO static extension mappers, NO AutoMapper.
- **No HttpApi controllers** — ABP auto-generates from `CrudAppService`.
- **DTO naming**: `XxxGetListInput`, not `GetXxxListDto`.
- **No wrapper proxy services** — inject the generated proxy directly, call via `firstValueFrom()`.
- **Localization keys** follow `::Training.KeyName` in TS/HTML, dot-notation in JSON, single colons in error codes.
- **No inline styles, no getter arrays, no `.bind(this)`** in Angular templates.
- **Standalone components**, Signals, `@if` / `@for`, `input()` / `output()`, `inject()`, `firstValueFrom`.
- **Explicit `@switch` cases** for every status value — no `@default` fallback that hides flows.
- **Status guards on every AppService transition** — throw with localizable error code.
- **Unit-scope filter via helper** — no per-service `IsInRolesAsync([...])` drift.
- **Auto-fill additive, never destructive** — skip existing rows.
- **Full CHG-07 formula** at every computation point — never hardcode 0 or partial formula.

---

## 13. UX & Design Standards (Inherited from Phase 3 Changes §12A)

- Card-based layouts, 16px radius, subtle shadow, Tajawal font.
- Status badges follow colour code: amber = needs action, green = approved, red = rejected/returned, grey = draft.
- Cost gate visible in UI (disabled button + tooltip), not hidden behind an error toast.
- ReturnedToCreator rows have amber left accent + banner.
- Notes drawer badge pulses when unread.
- Financial items table supports optimistic rate edits (apply locally, rollback on error toast).
- Empty states use emoji illustration + headline + one CTA.
- RTL polish: thousands separators for OMR, DD/MM/YYYY dates, `ر.ع` suffix.
- WCAG AA contrast, visible focus rings, semantic HTML.

---

## 14. Ready-to-Execute Checklist

Before starting the code session, confirm:

- [ ] All 8 Round-1 decisions approved (§2)
- [ ] State machine diagram (§3.4) approved
- [ ] 25 endpoints list (delivery gate 2) approved
- [ ] 4 page mockups (delivery gate 3) approved
- [ ] v4.4 codebase backed up
- [ ] Migration test strategy agreed (reset DB or apply forward-only?)

---

*End of Phase 4A Implementation Prompt — v1.2 — April 23, 2026*
