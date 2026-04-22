# GTMS Phase 3 Changes — Implementation Prompt

**Version:** v1.1 (added Section 12A — UX & Design Standards)
**Date:** April 20, 2026
**Base:** GTMS v4.3 (Phase 3 complete)
**Target:** v4.4
**Companion docs:**
- `GTMS-Phase3-Changes-v2-Design.md` — full design spec
- `GTMS-Phase3-Changes-v2-Mockup.html` — **visual style anchor** (match this feel)
- `GTMS-Project-Checkpoint-v4_3.md` — project context

---

## 1. Scope Summary

Seven coordinated changes (CHG-01 → CHG-07) reshape the Phase 3 workflow:

| # | Change | Nature |
|---|---|---|
| CHG-01 | Nominations mandatory at plan item creation | Workflow reshape |
| CHG-02 | `DefaultAmountOMR` on FinancialItem | Schema add |
| CHG-03 | Per-rank amounts + **editable on plan item level** | New table |
| CHG-04 | Review pre-fills defaults per nomination × rank | Domain service |
| CHG-05 | Notes + Return at Plan / Item / Nomination | Schema + state |
| CHG-06 | Creator edits while returned | AppService guards |
| CHG-07 | `IsPerDay` + `IsPerNominee` flags + cascade | Calculation engine |

---

## 2. Locked Decisions

**Round 1 — Design questions:**
1. ✅ **Drop UGM/TD nomination approvals.** `NominationApproval` table kept for history; no new writes.
2. ✅ **Per-rank editable.** Staff edits rate per-rank per-plan-item via new table `TrnPlanItemFinancialItemRanks`.
3. ✅ **Ship item + nomination-level return in v1.** Full granularity.
4. ✅ **Notes visible to all.** UTM sees every Staff/TD/TH note on their plan.

**Round 2 — Final sanity checks:**
1. ✅ Rate edits on plan item stay isolated — never mutate `FinancialItemRankAmounts`.
2. ✅ `Days` is always locked to `PlanItem.DurationDays`. Staff cannot edit independently. **BUT** when `DurationDays` changes → cascade recalc all per-day subtotals.
3. ✅ New nominee added → auto-create rank row with `DefaultAmountOMR` (or `FinancialItemRankAmount` if one exists).
4. ✅ Approval blocked while ANY `PlanItem.IsReturned == true` OR ANY `Nomination.IsReturned == true`. Red banner listing unresolved returns.

---

## 3. Critical Rule: DurationDays Cascade + Travel Days

**3.1 `DurationDays` cascade:** when `TrainingPlanItem.DurationDays` changes (by UTM in Open / ReturnedToCreator states):

```
BEFORE commit:
  1. Validate new DurationDays >= 1
  2. For each PlanItemFinancialItem where FinancialItem.IsPerDay = true:
     For each child PlanItemFinancialItemRank:
       effectiveDays = newDurationDays + FinancialItem.ExtraDaysBefore + FinancialItem.ExtraDaysAfter
       SubtotalOMR   = RatePerUnitOMR
                     * effectiveDays
                     * (IsPerNominee ? NomineeCount : 1)
     Recompute parent PlanItemFinancialItem.EstimatedAmountOMR = SUM(children.SubtotalOMR)
  3. Save transaction
```

**3.2 Travel days formula:**

```
effectiveDays = IsPerDay ? (DurationDays + ExtraDaysBefore + ExtraDaysAfter) : 1
effectiveCount = IsPerNominee ? NomineeCount : 1
subtotal = rate * effectiveDays * effectiveCount
```

**Example (Travel Allowance, Colonel rank, 5-day international course):**
- rate = 100 OMR/day, IsPerDay = true, IsPerNominee = true
- ExtraDaysBefore = 1 (travel out), ExtraDaysAfter = 1 (travel back)
- effectiveDays = 5 + 1 + 1 = **7**
- nomineeCount = 1
- **subtotal = 100 × 7 × 1 = 700 OMR**

All encapsulated in `PlanItemRankBreakdownManager.RefreshForDaysChangeAsync(planItemId)` — also triggered on `FinancialItem.ExtraDays*` change (admin action).

---

## 4. Schema Changes (exact)

### 4.1 New Tables (3)

#### 4.1.1 `TrnFinancialItemRankAmounts`

```csharp
public class FinancialItemRankAmount : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid FinancialItemId { get; set; }
    public Guid RankId { get; set; }
    public decimal AmountOMR { get; set; }
}
```

- EF config: table `TrnFinancialItemRankAmounts`, prefix `Trn`
- Unique index: `(TenantId, FinancialItemId, RankId)`
- FK to `TrnFinancialItems.Id`
- No FK to `HrRanks` — soft reference (HR read-only)

#### 4.1.2 `TrnPlanItemFinancialItemRanks`

```csharp
public class PlanItemFinancialItemRank : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid PlanItemFinancialItemId { get; set; }
    public Guid RankId { get; set; }
    public int NomineeCount { get; set; }
    public decimal RatePerUnitOMR { get; set; }
    public decimal SubtotalOMR { get; set; }
}
```

- Table `TrnPlanItemFinancialItemRanks`
- Unique index: `(TenantId, PlanItemFinancialItemId, RankId)`
- FK to `TrnPlanItemFinancialItems.Id` with cascade delete
- `SubtotalOMR` is stored (not computed) — updated by domain manager on every change

#### 4.1.3 `TrnPlanNotes`

```csharp
public enum PlanNoteEntityType { Plan = 0, PlanItem = 1, Nomination = 2 }
public enum PlanNoteAuthorRole { UTM = 0, Staff = 1, TD = 2, TH = 3, UGM = 4 }

public class PlanNote : CreationAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public PlanNoteEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string Note { get; set; }  // max 2000
    public PlanNoteAuthorRole AuthorRole { get; set; }
    public bool IsReturnReason { get; set; }
}
```

- Table `TrnPlanNotes`
- Index: `(TenantId, EntityType, EntityId, CreationTime DESC)`
- Append-only — no Update/Delete in repository layer

### 4.2 Modified Entities

#### 4.2.1 `FinancialItem`

```csharp
// ADD 5 fields
public decimal DefaultAmountOMR { get; set; }  // default 0
public bool IsPerDay { get; set; }             // default false
public bool IsPerNominee { get; set; }         // default false
public int ExtraDaysBefore { get; set; }       // default 0 — travel day(s) before course
public int ExtraDaysAfter { get; set; }        // default 0 — travel day(s) after course
```

`ExtraDaysBefore` / `ExtraDaysAfter` are only applied when `IsPerDay = true`. Typical values:
- Travel Allowance: before = 1, after = 1 (full travel-to-and-from coverage)
- Accommodation: before = 0, after = 0 (or 1+1 if business requires — configurable)
- Venue Rental: 0 / 0
- Non-per-day items: 0 / 0 (ignored)

#### 4.2.2 `TrainingPlan`

```csharp
public enum PlanStatus {
    Draft = 0,
    Open = 1,
    Submitted = 2,
    UnderReview = 3,
    ReturnedToCreator = 4,   // NEW
    TDApproved = 5,
    THApproved = 6,
    Rejected = 7
}
```

#### 4.2.3 `TrainingPlanItem`

```csharp
// REMOVE
public int NomineesCount { get; set; }  // becomes derived in DTO

// ADD
public bool IsReturned { get; set; }          // default false
public Guid? LastReturnNoteId { get; set; }   // → PlanNotes.Id
```

#### 4.2.4 `Nomination`

```csharp
// ADD
public bool IsReturned { get; set; }
public Guid? LastReturnNoteId { get; set; }
```

### 4.3 Deprecated

- `NominationApproval` — keep table + existing rows for history. Remove writes from `NominationAppService`. No DTO, no Create/Update endpoints.

### 4.4 Migrations

Two EF migrations (separate for clarity):

```
Migration 1: AddPhase3Changes_SchemaDeltas
  - FinancialItem +3 columns
  - TrainingPlan.Status enum value added (no DDL — enum is int)
  - TrainingPlanItem: DROP NomineesCount, ADD IsReturned, LastReturnNoteId
  - Nomination: ADD IsReturned, LastReturnNoteId

Migration 2: AddPhase3Changes_NewTables
  - TrnFinancialItemRankAmounts
  - TrnPlanItemFinancialItemRanks
  - TrnPlanNotes
```

---

## 5. Domain Services

### 5.1 NEW: `FinancialItemDefaultResolver`

```csharp
public class FinancialItemDefaultResolver : DomainService
{
    // Single rank lookup
    public async Task<decimal> ResolveRateAsync(Guid financialItemId, Guid rankId);
    // Returns: FinancialItemRankAmount if exists, else FinancialItem.DefaultAmountOMR

    // Batch — for pre-fill on Staff first-open
    public async Task<Dictionary<Guid, decimal>> ResolveRatesAsync(
        Guid financialItemId, IEnumerable<Guid> rankIds);

    // Compute subtotal with per-day + per-nominee + extra-days rules
    public decimal ComputeSubtotal(decimal rate, bool isPerDay, bool isPerNominee,
                                     int courseDays, int extraBefore, int extraAfter,
                                     int nomineeCount)
    {
        var effectiveDays = isPerDay ? (courseDays + extraBefore + extraAfter) : 1;
        var effectiveCount = isPerNominee ? nomineeCount : 1;
        return rate * effectiveDays * effectiveCount;
    }
}
```

### 5.2 NEW: `PlanItemRankBreakdownManager`

Encapsulates all breakdown lifecycle.

```csharp
public class PlanItemRankBreakdownManager : DomainService
{
    // Called by PlanItemFinancialItemAppService.AutoFillAsync
    public async Task InitializeAsync(Guid planItemFinancialItemId);
    // Creates rank rows for each distinct rank in the plan item's nominees

    // Called on nomination add/remove (via domain event or direct AppService call)
    public async Task RefreshForNomineeChangeAsync(Guid planItemId);
    // Recomputes NomineeCount per rank, adds missing rank rows, recomputes SubtotalOMR

    // Called by TrainingPlanItemAppService.UpdateAsync when DurationDays changes
    public async Task RefreshForDaysChangeAsync(Guid planItemId);
    // Recomputes SubtotalOMR for all per-day financial items in this plan item

    // Called by PlanItemFinancialItemAppService.UpdateRankRateAsync
    public async Task UpdateRateAsync(Guid rankBreakdownId, decimal newRate);
    // Updates row + recomputes parent PlanItemFinancialItem.EstimatedAmountOMR
}
```

### 5.3 UPDATED: `NominationConditionValidator`

Now called at plan item creation (CHG-01), not post-approval. Same 9 checks, same Arabic errors. Signature unchanged.

### 5.4 UNCHANGED: `PlanItemCostCalculator`, `CourseNameResolver`, `EmployeeResolver`, `BudgetRecalculator`

- `PlanItemCostCalculator` still SUMs `PlanItemFinancialItems.EstimatedAmountOMR` — values now reflect sum of rank subtotals
- `BudgetRecalculator` still runs on `FinalApproveAsync`

---

## 6. AppService Changes

### 6.1 `TrainingPlanAppService`

**NEW endpoints:**

```csharp
// POST /api/training-plans/{id}/return-to-creator
public async Task ReturnToCreatorAsync(Guid id, ReturnReasonDto input);
//   input: { Reason: string, required, max 2000 }
//   guards: status IN (UnderReview, TDApproved)
//   effect: status = ReturnedToCreator; creates PlanNote (IsReturnReason=true)

// POST /api/training-plans/{id}/resubmit
public async Task ResubmitAsync(Guid id);
//   guards: status == ReturnedToCreator + no item/nomination IsReturned
//   effect: status = UnderReview
```

**NEW guard on existing approval endpoints:**

```csharp
// StaffApproveAsync, TDApproveAsync, FinalApproveAsync
// Block if EXISTS (PlanItem where PlanId = id AND IsReturned) OR
//         EXISTS (Nomination where Plan.Id = id AND IsReturned)
// Return Arabic error: "لا يمكن الاعتماد — يوجد بنود/ترشيحات مُعادة يجب حلها أولاً"
```

### 6.2 `TrainingPlanItemAppService` (CHG-01 major)

**`CreateAsync` signature change:**

```csharp
public class CreatePlanItemDto {
    public Guid TenantCourseId { get; set; }
    public CourseType Type { get; set; }
    public Priority Priority { get; set; }
    public string Justification { get; set; }
    public string Description { get; set; }
    public string Objectives { get; set; }
    public int DurationDays { get; set; }
    public DateTime EstimatedDateFrom { get; set; }
    public DateTime EstimatedDateTo { get; set; }
    // ... (other existing fields)

    // NEW — REQUIRED
    public List<Guid> NomineeEmployeeIds { get; set; }  // min 1
}
```

Transactional flow:
1. Create `TrainingPlanItem` (without NomineesCount)
2. Copy conditions from `TenantCourse` → `PlanItemConditions`
3. For each nominee: run `NominationConditionValidator`, create `Nomination`
4. If any validation fails → throw aggregated Arabic exception, rollback
5. Return DTO with `NomineesCount` derived

**NEW endpoints:**

```csharp
// PUT /api/training-plan-items/{id}  (modified — cascade on DurationDays change)
// If DurationDays changed → call PlanItemRankBreakdownManager.RefreshForDaysChangeAsync

// POST /api/training-plan-items/{id}/return
public async Task ReturnAsync(Guid id, ReturnReasonDto input);
//   guards: Plan.Status == UnderReview, caller role IN (Staff, TD, TH)
//   effect: PlanItem.IsReturned = true, creates PlanNote (EntityType=PlanItem)

// POST /api/training-plan-items/{id}/clear-return
// (automatic when UTM saves — but exposed for explicit confirmation)
```

**NEW guards on all edit endpoints:**

```csharp
// UpdateAsync / DeleteAsync / conditions add/remove
// Allow if Plan.Status IN (Open, ReturnedToCreator)
//        OR (Plan.Status == UnderReview AND this.IsReturned == true)
// Role check: creator (UTM of the unit)
```

### 6.3 `NominationAppService` (major simplification + CHG-05)

**DROP:**
- Approval chain endpoints (`UgmApproveAsync`, `TdApproveAsync`, etc.) — marked `[Obsolete]` until full removal in v4.5
- `CreateBatchAsync` public endpoint — called internally only by `TrainingPlanItemAppService`

**KEEP:**
- GetListAsync (now used for read-only overview)
- GetAsync

**NEW:**

```csharp
// POST /api/nominations/{id}/return
public async Task ReturnAsync(Guid id, ReturnReasonDto input);

// POST /api/nominations/{id}/replace
public async Task<NominationDto> ReplaceAsync(Guid id, ReplaceNominationDto input);
//   input: { NewEmployeeId: Guid }
//   guards: IsReturned == true, caller is UTM
//   effect: validates new employee against conditions, updates EmployeeId,
//           clears IsReturned, triggers PlanItemRankBreakdownManager.RefreshForNomineeChangeAsync
```

### 6.4 NEW: `FinancialItemRankAmountAppService`

Standard `CrudAppService<FinancialItemRankAmount, ...>`.

```
GET    /api/financial-item-rank-amounts
GET    /api/financial-item-rank-amounts/{id}
POST   /api/financial-item-rank-amounts
PUT    /api/financial-item-rank-amounts/{id}
DELETE /api/financial-item-rank-amounts/{id}
GET    /api/financial-item-rank-amounts/by-item/{financialItemId}
```

### 6.5 NEW: `PlanItemFinancialItemRankAppService`

```
GET  /api/plan-item-financial-item-ranks/by-pifi/{planItemFinancialItemId}
PUT  /api/plan-item-financial-item-ranks/{id}/rate
     body: { RatePerUnitOMR: decimal }
     effect: calls PlanItemRankBreakdownManager.UpdateRateAsync
```

No Create/Delete exposed — lifecycle is managed by the domain manager.

### 6.6 NEW: `PlanNoteAppService`

```csharp
// POST /api/plan-notes
public async Task<PlanNoteDto> CreateAsync(CreatePlanNoteDto input);
//   input: { EntityType, EntityId, Note }
//   AuthorRole is inferred from CurrentUser's role

// GET /api/plan-notes?EntityType=&EntityId=
public async Task<ListResultDto<PlanNoteDto>> GetListAsync(PlanNoteGetListInput input);
//   returns chronological (oldest first by default)
```

No update/delete endpoints. Notes are permanent.

### 6.7 `FinancialItemAppService`

Minor: add `DefaultAmountOMR`, `IsPerDay`, `IsPerNominee` to Create/Update/Get DTOs.

### 6.8 `PlanItemFinancialItemAppService` (extended for CHG-03 + CHG-07)

**Modified `AutoFillAsync`:**
- Creates `PlanItemFinancialItem` entries from `CourseTypeFinancialItemDefaults`
- For each newly created PIFI where `FinancialItem.IsPerNominee == true`:
  - Calls `PlanItemRankBreakdownManager.InitializeAsync(pifi.Id)` → creates rank rows

**Modified `GetAsync`:**
- Returns DTO including children rank breakdown rows (via `WithDetailsAsync` / explicit navigation load)

---

## 7. Frontend — Page Changes

### 7.1 PAGE 2.1 Financial Items v2 (CHG-02 + CHG-03 + CHG-07)

- Add columns: `🗓️ يومي`, `👤 لكل مرشح`, `💰 المبلغ الافتراضي`, `👥 مبالغ رتب`
- Dialog gains 3 fields: `IsPerDay` (switch), `IsPerNominee` (switch), `DefaultAmountOMR` (number)
- Inline expand on row → **"المبالغ حسب الرتبة"** panel
  - Table: Rank | AmountOMR | [Edit] [Delete]
  - "+ Add rank amount" button opens mini-dialog (Rank picker + amount)
  - Example-calculation card showing formula preview

### 7.2 PAGE 3.2 Plan Entry v3 (CHG-01 + CHG-06)

- **Add Item dialog — 2 sections:**
  - Section A: Course Info (15 existing fields)
  - Section B: Nominees (NEW, required, min 1)
    - Employee search box (filter by name + military number)
    - Unit filter dropdown
    - Results list with checkboxes — shows rank + unit
    - Selected nominees as chips with × to remove
    - Total count display
- On Save: POST with `NomineeEmployeeIds`. If validation fails → error banner with per-nominee Arabic messages
- **Returned state UX (CHG-06):**
  - When `Plan.Status == ReturnedToCreator`: amber banner at top with return reason note
  - Edit button on each item
  - If a specific item has `IsReturned == true`: highlight row + "⚠️ يحتاج تعديل" badge
  - Resubmit button disabled until every returned item/nomination is resolved
- Notes button in header opens drawer

### 7.3 PAGE 3.3 Plan Review v6 (CHG-04 + CHG-05 + CHG-07)

Builds on v5 inline-expand pattern.

**Expanded panel structure:**
```
┌─ Course Detail Card ─────────────┬─ Financial Items Panel ──────────┐
│ Submitter, Justification, etc.   │ [+ Add Financial Item]           │
│ Duration (editable via main row) │                                  │
│ Conditions (tags)                │  For each PIFI:                  │
│ [🔙 Return this item only]       │    Header: Name + IsPerDay badge │
│                                  │            + IsPerNominee badge  │
│                                  │            + SUM total OMR       │
│                                  │                                  │
│                                  │    Body (if IsPerNominee):       │
│                                  │      table: Rank | Count | Rate  │
│                                  │             | (Days) | Subtotal  │
│                                  │      — Rate is input             │
│                                  │      — Days column only if       │
│                                  │        IsPerDay                  │
│                                  │                                  │
│                                  │    Body (if !IsPerNominee):      │
│                                  │      single amount input         │
│                                  │                                  │
│                                  │    Notes field                   │
│                                  │                                  │
│                                  │  Grand Total (blue pill)         │
│                                  │  [📝 Note on item]               │
├──────────────────────────────────┴──────────────────────────────────┤
│ Nominees sub-table                                                   │
│   Name | Rank | Unit | Conditions | Notes | [🔙 Return]              │
└──────────────────────────────────────────────────────────────────────┘
```

- Rate edits call `PUT /plan-item-financial-item-ranks/{id}/rate` on blur
- Parent total refreshes on response (optimistic update OK)
- Item return button opens return modal → POST `/training-plan-items/{id}/return`
- Nominee return button opens return modal → POST `/nominations/{id}/return`
- Header has: Notes drawer button (count badge), Return-plan button, Staff-Approve button
- Staff-Approve button disabled if any IsReturned present (show tooltip listing unresolved)

### 7.4 PAGE 3.4 Plan Approval v3 (CHG-05)

- Header: Notes drawer button + Return-plan button + Reject + Approve
- Same read-only inline expand pattern as v2
- TD or TH returning plan → status = ReturnedToCreator → notification to UTM
- Same blocking rule: any IsReturned → approve disabled

### 7.5 PAGE 3.5 Nominations — Repurposed Read-Only

- Strip all create/submit/approve actions
- Keep list with filters: Plan, Item, Status, IsReturned, Rank, Unit
- Show as overview — clicking row navigates to PAGE 3.3 (Review) or 3.4 (Approval) based on plan status
- Add info banner: "الترشيحات الآن تُدار مع بنود الخطة في شاشة إدخال الخطة"

### 7.6 NEW Shared Components

#### `NotesDrawerComponent`
- Left-slide drawer, 440px wide
- Props: `entityType`, `entityId`
- Tabs: Plan / Item / Nomination (filter by EntityType for current plan)
- Chronological list of notes with role pill + timestamp + author
- Return-reason notes visually highlighted (amber border, special badge)
- Footer: add-note textarea + send button

#### `ReturnModalComponent`
- Centered modal
- Props: `entityType`, `entityId`, `title`
- Required textarea for reason
- Info banner: "سبب الإعادة سيُحفظ كملاحظة دائمة ويُرسل إشعار لـ UTM"
- Cancel + Confirm buttons — calls appropriate `return` endpoint

#### `NominationPickerComponent`
- Embedded section in Plan Entry dialog
- Employee search + unit filter
- Checkbox list + selected chips
- Outputs: `selectedEmployeeIds: Guid[]`

---

## 8. State Machine (final)

```
        ┌─────┐  open   ┌──────┐  submit  ┌───────────┐
        │Draft│────────▶│ Open │─────────▶│ Submitted │
        └─────┘         └──────┘          └─────┬─────┘
                           ▲                    │ startReview
                           │                    ▼
                           │             ┌────────────┐
                           │   resubmit  │UnderReview │
                           │   (plan)    └──────┬─────┘
                           │                    │ staffApprove
                           │                    ▼
                           │             ┌────────────┐
                           │             │ TDApproved │
                           │             └──────┬─────┘
                           │                    │ thApprove
                           │                    ▼
                           │             ┌────────────┐
                           │             │ THApproved │ (terminal)
                           │             └────────────┘
                           │
                     ┌─────┴──────────────┐
                     │ ReturnedToCreator  │◀── returnToCreator (from UnderReview|TDApproved)
                     └────────────────────┘    (reason required)
```

**Item/Nomination-level return runs in parallel with Plan state:**
- `PlanItem.IsReturned` / `Nomination.IsReturned` are boolean flags, not states
- Plan stays in `UnderReview` while sub-items are returned
- Clearing: UTM edits + saves the item / replaces the nominee
- Plan approval blocked while ANY is set

---

## 9. Implementation Order

### Backend (Phase-3-Changes-Backend)

1. **Schema + entities**
   - Create 3 new entity classes (one per file)
   - Update `FinancialItem`, `TrainingPlan`, `TrainingPlanItem`, `Nomination`
   - Update EF configurations
   - Generate Migration 1 (deltas), Migration 2 (new tables)

2. **Domain services**
   - `FinancialItemDefaultResolver`
   - `PlanItemRankBreakdownManager`
   - Update `NominationConditionValidator` call site

3. **DTOs + Mapperly mappers** (class-based `MapperBase<T,U>`, one per file)

4. **Application.Contracts** — service interfaces + permissions

5. **Application services**
   - `FinancialItemRankAmountAppService` (new)
   - `PlanItemFinancialItemRankAppService` (new)
   - `PlanNoteAppService` (new)
   - Update `TrainingPlanAppService`, `TrainingPlanItemAppService`, `NominationAppService`, `PlanItemFinancialItemAppService`, `FinancialItemAppService`

6. **Permissions** — add to `Application.Contracts` (not Domain.Shared)

7. **Seeder updates**
   - `CourseFieldDataSeeder` / `FinanceDataSeeder`: set `IsPerDay`, `IsPerNominee`, `DefaultAmountOMR` on seeded items
   - Seed sample `FinancialItemRankAmounts` for travel allowance + accommodation
   - Update existing plan seeder: ensure all plan items get nominees at creation

8. **Localization** — en.json + ar.json, keys for all new strings

### Frontend (Phase-3-Changes-Frontend)

9. **Shared components**
   - `NotesDrawerComponent`
   - `ReturnModalComponent`
   - `NominationPickerComponent`

10. **PAGE 2.1 v2** — Financial Items (add flags + default amount + rank panel)

11. **PAGE 3.2 v3** — Plan Entry (add Nominees section + returned-state UX)

12. **PAGE 3.3 v6** — Plan Review (editable per-rank rates + Days column + item/nominee return + notes drawer)

13. **PAGE 3.4 v3** — Plan Approval (notes + return)

14. **PAGE 3.5** — Nominations repurposed as read-only overview

15. **Proxy regeneration** — `abp generate-proxy -t ng`

16. **Localization** — en.json + ar.json keys

### Validation

17. **Test scenarios** (on seeded Ground Forces tenant):
    - Create plan item with 3 nominees of different ranks → verify 9 conditions run, rank breakdown auto-creates
    - Staff opens Review → verify pre-filled rates from defaults
    - Staff edits one rate → verify only that plan item changes, `FinancialItemRankAmounts` untouched
    - UTM edits DurationDays from 14 → 10 → verify all per-day subtotals recalc
    - Staff returns one item → verify plan stays UnderReview, UTM can edit item, other items locked
    - Staff tries TD-approve with one item returned → verify blocked with Arabic error
    - UTM resolves return → Staff approves → TD approves → TH final → verify `BudgetRecalculator` still works

---

## 10. File Inventory (estimate)

### Backend (~45 files)

| Category | Files |
|---|---|
| Entities | 3 new + 4 updated |
| EF Configurations | 3 new + 4 updated |
| Migrations | 2 files |
| Domain Services | 2 new + 1 updated |
| DTOs | ~12 new |
| Mapperly mappers | ~10 new |
| AppService interfaces | 3 new + 4 updated |
| AppServices | 3 new + 5 updated |
| Permissions | 1 updated |
| Seeders | 3 updated |
| Localization | 2 updated (en, ar) |

### Frontend (~25 files)

| Category | Files |
|---|---|
| Shared components | 3 new (Notes drawer, Return modal, Nomination picker) |
| PAGE 2.1 v2 | 3 (.ts, .html, .scss) |
| PAGE 3.2 v3 | 3 |
| PAGE 3.3 v6 | 3 |
| PAGE 3.4 v3 | 3 |
| PAGE 3.5 | 3 (simplified) |
| Proxies | auto-generated |
| Localization | 2 updated |

**Total: ~70 files**

---

## 11. Delivery Protocol

Following established Phase 3 session rhythm:

1. Backend first — entities → EF → migrations → domain services → AppServices → permissions → seeder
2. Package each logical layer as `.zip` for download (not individual files)
3. Frontend — shared components first, then pages in order 2.1 → 3.2 → 3.3 → 3.4 → 3.5
4. Each page packaged separately
5. Validation script + test data generator
6. Final checkpoint doc `GTMS-Project-Checkpoint-v4_4.md`

---

## 12. Non-negotiable Patterns (Carry Forward)

**Backend:**
- One class per file
- ABP repository methods only (`WithDetailsAsync`, `AnyAsync`, `CountAsync`, `GetListAsync`)
- Primary constructors (.NET 10)
- DTO naming: `XxxGetListInput` (ABP convention, not `GetXxxListDto`)
- Mapperly class-based `MapperBase<TSource, TTarget>`, never static extensions
- HR entities remain read-only via `ExcludeFromMigrations`
- No HttpApi controllers — ABP auto-generates
- Permissions live in `Application.Contracts`
- Table prefix `Trn`

**Frontend:**
- NO DevExtreme on any page in this phase (PAGE 2.1 rebuilt to match)
- Plain HTML/CSS using GTMS design system (`gtms-design.scss`)
- Signals throughout — individual signals per form field
- `input()` / `output()` functions for component I/O
- No arrow functions in templates — named methods only
- `@if` / `@for` control flow
- `firstValueFrom` — never `toPromise`
- `LocalizationPipe` (not `LocalizationModule`)
- ABP proxy services directly — no wrapper services
- Standalone components, no NgModules
- `$any($event.target).value` for native input binding
- Map<signal> keyed by ID for expand-row caching

**Localization:**
- HTML/TS: `'::Training.KeyName'`
- JSON: `"Training.KeyName"` (no `::` prefix)
- Error codes: `"Training:Entity:Error"` (single colons)
- Permissions: `"Permission:Entity"`
- **NEVER** `'Training::KeyName'` anywhere

---

## 12A. UX & Design Standards (NEW — design freedom with guardrails)

**Style anchor:** `GTMS-Phase3-Changes-v2-Mockup.html`. Every page in this phase should feel like it belongs in that mockup — same tokens, same rhythm, same polish.

### Design System (locked)

- **Font:** Tajawal (weights 300/400/500/700/800)
- **Color tokens** (Tailwind-compatible SCSS vars):
  - primary: `#eff6ff → #1e3a8a`
  - success: `#f0fdf4 → #15803d`
  - warning: `#fffbeb → #d97706`
  - danger: `#fef2f2 → #dc2626`
  - purple: `#faf5ff → #9333ea`
  - amber (return states): `#fffbeb → #b45309`
- **Border radius:** 10/12/14/16/18px scale — cards 16, buttons 10-12, modals 18
- **Shadows:** `0 4px 20px rgba(0,0,0,0.08)` default, `0 6px 20px rgba(0,0,0,0.1)` on hover
- **Gradients:** `linear-gradient(135deg, color-500, color-600)` on primary/success/warning/danger/amber buttons

### Component Library (required — extend as needed)

- **Cards** — white bg, 16px radius, soft shadow, `translateY(-2px)` on hover
- **Stat cards** — icon + big number + label + optional trend arrow
- **Buttons** — 5 gradient variants + ghost + icon-only; all with press feedback (`scale(0.98)`)
- **Status badges** — pill shape, bold, 12px, palette-matched to status meaning
- **Inline expand rows** — established v5 pattern (cache via `Map<signal>`, `stopPropagation` on inner controls)
- **Modals** — centered, 18px radius, fade + scale-in, ESC to close, click-outside to close
- **Drawers** — slide from start-side (right in RTL), 440px, with backdrop
- **Toolbar chips / filter pills** — rounded, bordered, active = filled
- **Progress bars** — thin (1.5-3px), animated fill
- **Approval step indicators** — circles with pulse on active, checkmark on done

### Interactions (mandatory)

- **Loading states:** skeleton shimmer, NOT centered spinners. Each stat card / row gets its own skeleton matching shape.
- **Inline editing feedback:** on blur → API → brief green highlight flash (500ms) on success; red shake + toast on failure.
- **Optimistic updates:** rate edits, note adds, item returns — apply immediately, roll back on API error with toast.
- **Toast notifications:** top-left in RTL. Success (green, auto-dismiss 3s). Error (red, dismissible, 5s). Info (blue, 4s). Use for all save/delete/return confirmations.
- **Hover states:** every clickable element has a hover cue — lift, bg shift, or color shift. 300ms transitions.
- **Disabled states:** 50% opacity + cursor-not-allowed + tooltip explaining why (`title` attr or custom tooltip).
- **Keyboard support:** `Esc` closes modals/drawers, `Enter` submits forms in modals, focus trap inside modals, restore focus to trigger on close.
- **Destructive actions:** always require confirmation (return-to-creator, delete item, remove nominee). Modal with typed-confirmation for irreversible ones.

### Animations (use liberally — keep fast)

- **Screen transitions:** 0.2-0.3s fade
- **Modal open:** fade + subtle scale (0.95 → 1) 0.2s
- **Drawer slide:** 0.25s ease-out
- **Accordion expand:** 0.2s with height animation
- **Stagger list entrance:** 40-60ms per row when loading fresh lists
- **Status changes:** flash color on badge, pulse on step indicators
- **Notification dots:** gentle pulse on unread note counts
- **Always respect** `prefers-reduced-motion` — disable non-essential animations

### Icons

- **Navigation:** emoji (locked from v4.3)
- **Actions/inline:** Lucide SVG icons allowed (`lucide-angular` already possible, or inline SVG)
- **Status:** colored pills, checkmarks, warning triangles
- **Avoid mixing** — each context picks one style

### Empty States

- Simple SVG illustration (or large emoji)
- Headline + supportive sentence + primary CTA
- Never a bare "No data" text

### Error States

- Inline field errors: red icon + red text below field
- Form-level: amber/red banner at top with summary
- API errors: toast + console log + retry option where reasonable

### RTL Polish

- `dir="rtl"` at root, all spacing respects logical properties
- Directional icons (arrows, carets) flip
- Number inputs show thousands separators for OMR amounts (12,450 not 12450)
- Dates shown in DD/MM/YYYY format
- Currency always suffix `ر.ع` or prefix `OMR` — pick one consistently (mockup uses suffix)

### Accessibility

- WCAG AA contrast minimum on all text
- Visible focus rings (2px primary-500)
- `aria-label` on icon-only buttons
- Semantic HTML — `<table>` for tables, `<nav>` for menus, `<button>` for actions (not `<div>`)
- Screen reader announcements for toast notifications

### Performance

- Lazy-load heavy page chunks via Angular route-level loading
- Virtualize lists > 100 rows (CDK virtual scroll or simple pagination — mockup uses pagination)
- Debounce search inputs 300ms
- Cache expanded row data in `Map<signal>` (established pattern)
- Preload next page on pagination hover (bonus polish)

### Signature Touches (make it feel alive)

- Plan progress bar that fills as items get reviewed
- Return reason banner with gentle amber gradient pulse until acknowledged by UTM
- Rate input that shows a small calculator icon on focus, tooltip with formula breakdown
- "Jump to next missing cost" button with a subtle arrow animation
- Notes drawer badge — pulse when new note arrives during session
- Nomination chip "remove" button fades in on hover
- Grand total row — subtle gradient shimmer when value changes

**Final rule:** if a decision isn't covered here, match the mockup. If the mockup doesn't cover it, pick the option that's more polished, more animated, more accessible — in that order.

---

## 13. Ready-to-Execute Checklist

Before starting code session, confirm:

- [ ] Design v2 reviewed (`GTMS-Phase3-Changes-v2-Design.md`)
- [ ] Mockup v2 reviewed (`GTMS-Phase3-Changes-v2-Mockup.html`)
- [ ] All 4 Round-1 decisions locked
- [ ] All 4 Round-2 sanity checks confirmed
- [ ] DurationDays cascade rule understood
- [ ] Backup of v4.3 code taken
- [ ] Seeder reset plan in place (for testing new migrations)

---

*End of Implementation Prompt — v1.0 — April 20, 2026*
