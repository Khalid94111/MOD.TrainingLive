# GTMS Phase 3 Changes v2 — Design

**Date:** April 20, 2026
**Session:** Phase 3.1 — Workflow Reshape + Defaults + Return Loop
**Base:** GTMS v4.3
**Status:** Design review v2 — decisions locked, awaiting final signoff before implementation

---

## ✅ Decisions Locked (Round 1)

| # | Decision | Impact |
|---|---|---|
| Q1 | **Drop UGM/TD nomination approvals** | PAGE 3.5 becomes read-only overview. `NominationApproval` writes stop. |
| Q2 | **Per-rank editable rates** | New table `TrnPlanItemFinancialItemRanks` — staff edits rate per-rank on each plan item |
| Q3 | **Ship item + nomination-level return in v1** | `IsReturned` flags + return notes on PlanItem and Nomination |
| Q4 | **Notes visible to all** | Full thread visible to UTM, Staff, TD, TH |

## 📋 Change Summary

| # | Change | Complexity |
|---|---|---|
| CHG-01 | Nominations mandatory at plan item creation (move from post-approval) | **High** |
| CHG-02 | `DefaultAmount` on FinancialItem | Low |
| CHG-03 | Rank-based amount overrides + **per-rank editable on plan items** | **High** (raised from Medium) |
| CHG-04 | Review pre-fills defaults per nomination × rank | Medium |
| CHG-05 | Notes + Return at Plan / **Item / Nomination** levels (full granularity) | **High** |
| CHG-06 | Creator can modify until approved (editable while returned) | Medium |
| **CHG-07** | **Per-day + per-nominee rate flags** (NEW) | Medium |

---

## 🗄️ Schema Changes

### 1. `FinancialItem` — ADD 5 fields (CHG-02 + CHG-07)

```csharp
public decimal DefaultAmountOMR { get; set; }    // default: 0 — fallback when no rank override
public bool IsPerDay { get; set; }               // default: false — multiply by (duration + extra days)
public bool IsPerNominee { get; set; }           // default: false — multiply by nominee count
public int ExtraDaysBefore { get; set; }         // default: 0 — travel-out day(s) before course
public int ExtraDaysAfter { get; set; }          // default: 0 — travel-back day(s) after course
```

**Calculation matrix (CHG-07):**

| `IsPerDay` | `IsPerNominee` | Example | Auto-calculated subtotal |
|---|---|---|---|
| false | false | Course Fees (flat) | `amount` |
| false | true | Clothing Allowance | `amount × nomineeCount` |
| true | false | Venue/Room Rental | `amount × (days + extraBefore + extraAfter)` |
| true | true | Travel Allowance, Accommodation | `amount × (days + extraBefore + extraAfter) × nomineeCount` |

**Travel days example:** Travel Allowance (IsPerDay, IsPerNominee, ExtraBefore=1, ExtraAfter=1) for a 5-day course at 50 OMR/day, 1 nominee = `50 × (5+1+1) × 1` = **350 OMR**.

Migration: add 5 columns, defaults false/0, backfill via seeder:
- Travel Allowance: `IsPerDay=true, IsPerNominee=true, ExtraDaysBefore=1, ExtraDaysAfter=1`
- Accommodation: `IsPerDay=true, IsPerNominee=true, ExtraDaysBefore=0, ExtraDaysAfter=0`
- Course Fees: all defaults (0/false)

---

### 2. NEW TABLE: `TrnFinancialItemRankAmounts` (CHG-03)

General-purpose rank overrides. Populated only for rank-varying items (travel allowances, etc.).

| Field | Type | Required | Description |
|---|---|---|---|
| Id | Guid PK | Yes | |
| TenantId | Guid FK | Yes | IMultiTenant |
| FinancialItemId | Guid FK | Yes | → FinancialItem |
| RankId | Guid FK | Yes | → HrRank (read-only ref) |
| AmountOMR | decimal | Yes | Rank-specific amount |

**Unique constraint:** `(FinancialItemId, RankId)` per tenant.

**Resolution rule:**
```
FinancialItemDefaultResolver(itemId, rankId, courseDays, nomineeCount):
  rate = RankAmounts[itemId, rankId] ?? DefaultAmountOMR
  effectiveDays = item.IsPerDay
      ? (courseDays + item.ExtraDaysBefore + item.ExtraDaysAfter)
      : 1
  effectiveCount = item.IsPerNominee ? nomineeCount : 1
  return rate * effectiveDays * effectiveCount
```

---

### 3. NEW TABLE: `TrnPlanItemFinancialItemRanks` (CHG-03 + CHG-07)

Per-rank breakdown stored on each plan item's financial item — so Staff can edit rates **for this plan item only** without affecting global `FinancialItemRankAmounts`.

| Field | Type | Required | Description |
|---|---|---|---|
| Id | Guid PK | Yes | |
| TenantId | Guid | Yes | IMultiTenant |
| PlanItemFinancialItemId | Guid FK | Yes | → PlanItemFinancialItem |
| RankId | Guid FK | Yes | → HrRank |
| NomineeCount | int | Yes | Count of plan item nominees with this rank (auto-maintained) |
| RatePerUnitOMR | decimal | Yes | Editable — starts from `FinancialItemRankAmounts[Item,Rank]` or `DefaultAmountOMR` |
| SubtotalOMR | decimal | Yes | Auto-computed: `Rate × (IsPerDay ? Days : 1) × (IsPerNominee ? Count : 1)` |

**Unique constraint:** `(PlanItemFinancialItemId, RankId)` per tenant.

**Lifecycle:**
- Rows auto-created when Staff expands a plan item for the first time in Review (`ComputePreFillAsync`)
- `NomineeCount` auto-maintained on nomination add/remove via domain event
- `SubtotalOMR` auto-recomputed on Rate edit or Days change
- `PlanItemFinancialItem.EstimatedAmountOMR` = SUM of all children `SubtotalOMR` (for per-nominee items); or the flat value on the parent row (for non-per-nominee items)

**For flat items (IsPerNominee=false):** no rank rows created — amount stays on `PlanItemFinancialItem.EstimatedAmountOMR` directly.

---

### 4. NEW TABLE: `TrnPlanNotes` (CHG-05)

Polymorphic, append-only notes log across Plan / Item / Nomination.

| Field | Type | Required | Description |
|---|---|---|---|
| Id | Guid PK | Yes | |
| TenantId | Guid FK | Yes | IMultiTenant |
| EntityType | enum | Yes | `Plan` / `PlanItem` / `Nomination` |
| EntityId | Guid | Yes | Polymorphic FK (no DB constraint) |
| Note | string(2000) | Yes | Arabic content |
| AuthorRole | enum | Yes | Staff / TD / TH / UTM |
| IsReturnReason | bool | Yes | True if written as a Return-to-Creator reason |
| CreatorId | Guid | Yes | ABP audit |
| CreationTime | DateTime | Yes | ABP audit |

**Indexes:** `(EntityType, EntityId, CreationTime DESC)`.

Append-only — never update/delete. UI shows chronological thread.

---

### 4. `TrainingPlan.Status` — ADD value (CHG-05)

```csharp
public enum PlanStatus {
    Draft = 0,
    Open = 1,
    Submitted = 2,
    UnderReview = 3,
    ReturnedToCreator = 4,   // NEW — whole plan returned
    TDApproved = 5,
    THApproved = 6,
    Rejected = 7
}
```

---

### 5. `TrainingPlanItem` — ADD 2 fields, REMOVE 1 (CHG-01 + CHG-05)

```csharp
// REMOVED
public int NomineesCount { get; set; }   // now derived: Nominations.Count(n => n.PlanItemId == this.Id)

// ADDED (CHG-05 — item-level return, v1 scope)
public bool IsReturned { get; set; }         // default: false — set true on item-level return
public Guid? LastReturnNoteId { get; set; }  // pointer to the most recent return reason in PlanNotes
```

When `IsReturned = true`:
- UTM can edit this item only (other items stay locked)
- Plan status stays `UnderReview`
- Staff cannot TD-approve the plan while any item has `IsReturned = true`
- UTM clears flag by saving the item (or pressing "Resolve & Resubmit item") → re-queues to Staff

---

### 6. `Nomination` — ADD 2 fields (CHG-05)

```csharp
public bool IsReturned { get; set; }         // default: false
public Guid? LastReturnNoteId { get; set; }  // pointer to return reason
```

Same semantics as PlanItem — UTM can replace/remove that specific nominee only. Plan and item approval blocked while any nomination is returned.

---

### 7. `Nomination.ApprovalStatus` — DROPPED (Q1)

`NominationApproval` table kept for historical records (existing v4.3 data). New nominations don't get an approval chain — they inherit their plan's approval status via the plan workflow only.

---

## 🔄 Workflow State Machine (CHG-05 + CHG-06)

```
                         ┌─────────────┐
                         │    Draft    │
                         │   (TD)      │
                         └──────┬──────┘
                                │ Open()
                                ▼
                         ┌─────────────┐
              ┌─────────►│    Open     │◄─────────┐
              │          │   (UTM)     │          │
              │          └──────┬──────┘          │
              │                 │ Submit()        │
              │                 ▼                 │
              │          ┌─────────────┐          │
              │          │  Submitted  │          │
              │          └──────┬──────┘          │
              │                 │ StartReview()   │
              │                 ▼                 │
              │          ┌─────────────┐          │ Resubmit()
              │   ┌─────►│ UnderReview │          │
              │   │      │  (Staff)    │──────────┤
              │   │      └──────┬──────┘          │
              │   │             │ ApproveByStaff()│
              │   │             ▼                 │
              │   │      ┌─────────────┐          │
              │   │      │ TDApproved  │──────────┤ ReturnToCreator()
              │   │      └──────┬──────┘          │
              │   │             │ ApproveByTH()   │
              │   │             ▼                 │
              │   │      ┌─────────────┐          │
              │   │      │ THApproved  │──────────┤
              │   │      │  (terminal) │          │
              │   │      └─────────────┘          │
              │   │                               │
              │   │      ┌─────────────────┐      │
              │   └──────┤ ReturnedToCreator│◄────┘
              └──────────┤      (UTM)       │ (editable again)
                         └─────────────────┘
```

**Rules:**
- Return allowed from `UnderReview`, `TDApproved`, or (before finalize) `THApproved` → must write note, status → `ReturnedToCreator`
- In `ReturnedToCreator`: UTM can edit plan, items, nominations
- Resubmit → status → `UnderReview` (re-walks chain from Staff)
- `THApproved` finalized → no return (BudgetRecalculator has run)

---

## 🛠️ Domain Services

### NEW: `FinancialItemDefaultResolver` (CHG-04)

```csharp
public class FinancialItemDefaultResolver : ITransientDependency
{
    // Resolve default for one (item, rank) pair
    Task<decimal> GetDefaultAmountAsync(Guid financialItemId, Guid? rankId);

    // Batch: compute total for a plan item's nominations across its assigned financial items
    Task<Dictionary<Guid, decimal>> ComputePreFillAsync(
        Guid planItemId,
        IEnumerable<Guid> financialItemIds,
        IEnumerable<Guid> rankIds);
}
```

Used by Plan Review to pre-fill `PlanItemFinancialItems.EstimatedAmountOMR`.

### UPDATED: `PlanItemCostCalculator` (no signature change)

Still SUMs `PlanItemFinancialItems.EstimatedAmountOMR` — but values now seeded from `FinancialItemDefaultResolver` on first Review open.

### UPDATED: `NominationConditionValidator`

Now runs **at plan item creation** (CHG-01), not at post-approval nomination step. Same 9 checks, same Arabic errors.

### UPDATED: `BudgetRecalculator`

No change — still runs on FinalApproveAsync.

---

## 🌐 AppService Changes

### `TrainingPlanAppService`

**NEW endpoints:**
```
POST /training-plans/{id}/return-to-creator  → { reason: string }
POST /training-plans/{id}/resubmit
```

`FinalApproveAsync` now blocks if status != `TDApproved` (unchanged guard, confirmed logic).

**NEW guard on edit AppServices:** `TrainingPlanItemAppService.Update/Delete/Create` — allow if `Plan.Status in (Open, ReturnedToCreator)`, reject otherwise.

---

### `TrainingPlanItemAppService` (CHG-01 major)

`CreateAsync` signature changes:

```csharp
// OLD
Task<PlanItemDto> CreateAsync(CreatePlanItemDto input);

// NEW
Task<PlanItemDto> CreateAsync(CreatePlanItemDto input);
// input now includes: NomineeEmployeeIds: Guid[]  (min 1, required)
```

Transactional flow in `CreateAsync`:
1. Create plan item (without count)
2. Copy conditions from TenantCourse → PlanItemConditions
3. Validate each nominee against conditions via `NominationConditionValidator`
4. Create Nomination records (batch)
5. Return DTO with `NomineesCount` derived

If any nominee fails validation → transaction rolls back, return Arabic error listing failures.

---

### `FinancialItemAppService`

Add `DefaultAmountOMR` to Create/Update/Get DTOs. That's it.

---

### NEW: `FinancialItemRankAmountAppService`

Standard `CrudAppService<FinancialItemRankAmount, ...>` — 6 endpoints:
- Get / GetList / Create / Update / Delete / GetByFinancialItem

Page 2.1 loads rank amounts alongside the item detail.

---

### NEW: `PlanNoteAppService`

```csharp
Task<PlanNoteDto> CreateAsync(CreatePlanNoteDto input);  // append-only
Task<PagedResultDto<PlanNoteDto>> GetListAsync(EntityType, EntityId);
```

No update/delete endpoints exposed.

---

### `NominationAppService`

**DEPRECATED:** `CreateBatchAsync` standalone endpoint → now only called internally by `TrainingPlanItemAppService.CreateAsync`.

**KEEP:** UGM/TD approval endpoints (nomination-level approval still runs after plan approval for external coordination — confirm this is still needed, or fold into plan approval?).

> **Open question:** After CHG-01, are UGM/TD per-nomination approvals still needed? They used to run post-plan-approval. Options: (a) keep them as-is — nominations go through their own approval chain after plan is THApproved, (b) drop them — plan approval approves everything. **Need your call.**

---

## 🖥️ Page Changes

### PAGE 2.1 v2 — Financial Items (CHG-02 + CHG-03)

- Add **"المبلغ الافتراضي (DefaultAmountOMR)"** column to main table + dialog
- Inline expand on row → **"المبالغ حسب الرتبة"** panel
  - Table: Rank | AmountOMR | [Edit] [Delete]
  - Add button: "+ إضافة مبلغ لرتبة"
  - Message if empty: "لا توجد مبالغ خاصة بالرتب — سيتم استخدام المبلغ الافتراضي"

### PAGE 3.2 v3 — Plan Entry (CHG-01 + CHG-06)

- Add Item Dialog split into 2 sections (single modal, scrollable):
  - **Section A:** Course Info (15 fields — unchanged)
  - **Section B:** Nominees ⚠️ REQUIRED (min 1)
    - Employee multi-select (filtered by unit from EmployeeResolver)
    - Selected list with badges (name + rank)
    - Remove (×) per selected
    - Total count display
- Validation: min 1 nominee + all must pass condition checks → error banner if fails
- When Plan.Status = `ReturnedToCreator`, UTM sees "Edit" button on existing items + a big amber banner: *"الخطة مُعادة للتعديل — ملاحظة: [reason]"*
- Plan Notes thread accessible via "📝 الملاحظات" button in header

### PAGE 3.3 v6 — Plan Review (CHG-04 + CHG-05)

Key change: Expanded panel shows **pre-filled defaults per-nominee-per-rank**.

**Financial Items panel (inline expand), new structure:**
```
┌─ 📋 البنود المالية ─────────────────────────── [+ إضافة بند] ─┐
│                                                                │
│  [▼] بدل سفر (Travel Allowance) — 1,250 ر.ع                    │
│      ┌──────────────────────────────────────────────────┐      │
│      │ الرتبة      │ العدد │ المبلغ الافتراضي │ المجموع │      │
│      │ عقيد        │  1   │     500          │   500   │      │
│      │ ملازم أول   │  2   │     250          │   500   │      │
│      │ جندي أول    │  1   │     250          │   250   │      │
│      └──────────────────────────────────────────────────┘      │
│      ملاحظات: [____________________________]                   │
│                                                                │
│  [▼] رسوم الدورة — 2,000 ر.ع  (flat × 4 nominees = 2,000)      │
│      ✏️ تعديل المجموع: [2000] ر.ع                               │
│      ملاحظات: [____________________________]                   │
└────────────────────────────────────────────────────────────────┘
```

Pre-fill happens **on first expand** — then cached in `PlanItemFinancialItems`. Staff can override totals.

**Notes panel:** Floating sidebar button "📝 ملاحظات (3)" → opens drawer showing thread for current entity (Plan | Item | Nomination tabs).

**Return button:** Red "🔙 إعادة للمُنشئ" in page header → opens dialog with required reason textarea → PATCH /return-to-creator.

### PAGE 3.4 v3 — Plan Approval (CHG-05)

- Add "🔙 إعادة للمُنشئ" button next to Approve/Reject
- Notes drawer (same as 3.3)
- TD and TH can both return

### PAGE 3.5 — Nominations — REPURPOSED

- Now **read-only overview** — lists all nominations across all plan items with status, rank, unit
- Filter by plan, item, status
- No create/submit actions (moved to 3.2)
- UGM/TD approval actions remain — pending your call on the open question above

---

## 🎨 New UI Elements

- `status-returned` badge (amber border + amber bg): `bg-amber-100 text-amber-800 border-amber-300`
- `notes-thread` panel — card list with role pills, timestamps
- `rank-breakdown-table` — compact nested table inside financial item row
- `return-dialog` — modal with required textarea + 2 buttons (Cancel / Confirm Return)

---

## 📦 Impact Estimate

| Metric | v4.3 → v4.4 |
|---|---|
| Tables | 72 → **75** (+RankAmounts, +PlanItemRankBreakdown, +PlanNotes) |
| AppServices | 8 → **11** (+RankAmount, +PlanItemRank, +PlanNote) |
| Endpoints | ~108 → **~135** (+27) |
| Domain Services | 5 → 6 (+Resolver with per-day/per-nominee logic) |
| Pages changed | 5 (2.1, 3.2, 3.3, 3.4, 3.5) |
| Migrations | 3 (add cols + 3 tables) |
| Seeder updates | Yes — `IsPerDay`/`IsPerNominee` flags + sample rank amounts for travel items |

---

## ❓ Final Sanity Checks Before Implementation

1. **Rank rate persistence:** When Staff edits a rate on a plan item's rank breakdown, it changes **only that plan item** (in `PlanItemFinancialItemRanks`). Global `FinancialItemRankAmounts` stays untouched. ✓ confirm?
2. **Days field:** Defaults from `PlanItem.DurationDays`. Should Staff be able to override days per-item on the financial breakdown (e.g., course is 14 days but travel applies only for 10)? Or always lock to `PlanItem.DurationDays`?
3. **Nomination added mid-review:** If UTM is returned an item and adds a new nominee with a rank not yet in the breakdown, system auto-adds a new rank row with Default rate — correct?
4. **Blocking rule:** Plan cannot be TD-approved if ANY `PlanItem.IsReturned == true` OR ANY `Nomination.IsReturned == true`. Staff sees a red banner listing unresolved returns. ✓ confirm?

---

*End of design v2.*
