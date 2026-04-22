# GTMS Project Checkpoint — v4.3

**Date:** April 20, 2026
**Session:** Phase 3 Implementation + Page Fixes
**Previous Version:** v4.2 (April 12, 2026)

---

## 📋 Session Summary

Phase 3 (Annual Plans & Nominations) fully implemented + 5 iterations of page fixes. 13 tables, 8 AppServices, ~50 endpoints, 7 pages. Key architectural improvements: HR integration (Rank + Employee), EstimatedCost auto-calculated, NominationConditionValidator with real 9-condition checks, CourseNameResolver, DevExtreme dropped entirely, BudgetRecalculator auto-updates budget totals on plan approval.

**Page fixes applied (5 iterations on Plan Review alone):**
- v1→v2: Bug fixes (inline editing, dropdown loading, field name, localization)
- v3: Unit accordion grouping, per-unit stats, progress bar, jump-to-next-missing
- v4: Per-unit pagination (10 items/page), batch auto-fill per unit
- v5: Inline expandable rows — course detail + financial items expand directly below clicked row
- Applied same inline expand pattern to Plan Approval (read-only) and Plan Entry (course detail only)
- BudgetRecalculator domain service wired into FinalApproveAsync
- Financial items dropdown uses `<optgroup>` grouping children under parent category

---

## 📁 Output Files

| File | Description |
|------|-------------|
| `GTMS-Phase3-Code.zip` | 107 files (75 backend + 32 frontend) — ORIGINAL with DevExtreme |
| `GTMS-Phase3-Fixes.zip` | 16 files — HR integration, domain services, updated AppServices |
| `GTMS-Phase3-Frontend-v2.zip` | 14 files — new frontend WITHOUT DevExtreme (GTMS design system) |
| `GTMS-Phase3-Missing-Pages.zip` | 11 files — Plan Review, Price Quotes, Providers + routes |
| `GTMS-Phase3-Pages-Mockup.html` | 7-tab HTML mockup (Tailwind + Tajawal + emoji) |
| `GTMS-Seeder-Refactored.zip` | 7 files — separated seeders with UoW per step |
| `training.routes.ts` | Master routes (all phases) |
| `training-route.provider.ts` | Menu hierarchy (all phases) |
| `Plan-Review-v5-InlineExpand.zip` | **LATEST** Plan Review — inline expand, pagination, batch auto-fill |
| `Plan-Approval-Entry-v2.zip` | **LATEST** Plan Approval (read-only expand) + Plan Entry (course detail expand) |
| `BudgetRecalculator.cs` | Domain service — auto-updates budget TotalAmount on plan approval |
| `FinalApproveAsync-Wire.cs` | Wire BudgetRecalculator into TrainingPlanAppService |
| Various fix files | OrgUnit Code reflection, tenant lookup, financial items per-tenant |

---

## ✅ Phase 3 Delivered Scope

### Tables (13 new)

| Table | Status | Notes |
|-------|--------|-------|
| TrnTrainingPlans | New | 6-step workflow (MOD-17), year-unique per tenant |
| TrnTrainingPlanItems | New | MOD-5 fields, EstimatedCost REMOVED (auto-calculated) |
| TrnPlanItemConditions | New | Copied from TenantCourseConditions |
| TrnCourses | New | Created from approved plan items |
| TrnCourseSessions | New | Shared: center sessions + annual plan sessions |
| TrnSessionConditions | New | Conditions on sessions |
| TrnPlanItemFinancialItems | New | MOD-6: Staff assigns, amounts in OMR + USD |
| TrnNominations | New | 3-level approval (MOD-17), post-course fields (v3.1) |
| TrnNominationApprovals | New | UTM(auto) → UGM → TD chain |
| TrnPriceQuotes | New | Auto-calc PerPerson ↔ Total |
| TrnTrainingProviders | New | Rating, approved status |
| HrRanks | Read-only | ExcludeFromMigrations, 14 military ranks |
| HrEmployees | Read-only | ExcludeFromMigrations, UserId + MainUnitId + RankId |

### AppServices (7 + 1 HR)

| Service | Endpoints | Key Logic |
|---------|-----------|-----------|
| TrainingPlanAppService | 12 | CRUD + 6-step workflow + cost gate + BudgetRecalculator on FinalApprove |
| TrainingPlanItemAppService | 7 | CRUD + conditions + names resolved + unit auto-detected |
| PlanItemFinancialItemAppService | 7 | CRUD + auto-fill + updateAmount (auto-calc USD) + updateNotes |
| CourseSessionAppService | 5 | Shared CRUD with course name join |
| NominationAppService | 6 | Batch create + 3-level approval + real condition validation |
| PriceQuoteAppService | 7 | CRUD + auto-calc + approve/reject |
| TrainingProviderAppService | 6 | CrudAppService + GetAllActive |
| HrLookupAppService | 4 | GetEmployeesByUnit, GetByServiceNumber, GetAllRanks, GetCurrentEmployee |

### Domain Services (5)

| Service | Purpose |
|---------|---------|
| EmployeeResolver | CurrentUser → Employee → MainUnitId/Rank; batch resolve |
| CourseNameResolver | TenantCourseId → CourseCatalog names (Ar/En); batch resolve |
| PlanItemCostCalculator | SUM(PlanItemFinancialItem.EstimatedAmountOMR); cost gate helper |
| NominationConditionValidator | Validates all 9 conditions against Employee HR data with Arabic error messages |
| BudgetRecalculator | Runs on FinalApproveAsync — SUMs plan item financials by parent FinancialItem, updates TrainingBudget.TotalAmount |

### Frontend Pages (7) — NO DevExtreme — LATEST versions

| Page | Route | Role | Components |
|------|-------|------|------------|
| 3.1 Annual Plans List | /training/plans | All | Table + create dialog + workflow steps |
| 3.2 Plan Entry **v2** | /training/plans/:id/entry | UTM | Items grid + inline expandable course detail + 15-field dialog + conditions |
| 3.3 Plan Review **v5** | /training/plans/:id/review | Staff | Unit accordion + per-unit pagination (10/page) + inline expandable course detail + editable financial items + batch auto-fill + progress bar + jump-to-next-missing |
| 3.4 Plan Approval **v2** | /training/plans/:id/approve | TD/TH | Stat cards + cost gate warning + inline expandable course detail + read-only financial items |
| 3.5 Nominations | /training/nominations | UTM/UGM/TD | Expandable approval chain + employee multi-select + condition validation |
| 3.6 Price Quotes | /training/price-quotes | Staff | Grid + auto-calc dialog + approve/reject |
| Providers | /training/providers | Staff | CRUD + rating + toggle switch |

### Data Seeder (refactored — 7 files)

| Seeder | Scope |
|--------|-------|
| GtmsDataSeeder | Orchestrator — calls sub-seeders in order with UoW |
| RankDataSeeder | 14 military ranks (supra-tenant) |
| CourseFieldDataSeeder | 6 fields (supra-tenant) |
| CourseCatalogDataSeeder | 10 courses + 14 conditions (supra-tenant) |
| ProviderDataSeeder | 4 providers (supra-tenant) |
| TenantDataSeeder | Everything per-tenant: OrgUnits, Users, Employees, TenantCourses, Finance, Centers, Plans, Nominations, Quotes |
| SeedIds | Fixed GUIDs for Ranks, Fields, Catalogs, Providers |

### 3 Tenant Scenarios

| Tenant | Scenario | Plan Status | Tests |
|--------|----------|-------------|-------|
| Ground Forces | FullApproved | THApproved | Sessions, nominations at all 3 levels, quotes, post-approval |
| Air Forces | MidWorkflow | UnderReview | 2 items missing cost → cost gate blocks |
| Naval Forces | EarlyStage | Open | UTMs submitting, no financials |

### Per Tenant Data

- 11 OrgUnits (HQ → TrainingDir → HR → 2 Brigades → 3 Battalions → Engineering → 2 Centers)
- 25 Users with Arabic names + military ranks
- 21 Employee records linked to Users with RankId, MainUnitId
- 8 TenantCourses with auto-copied conditions
- 12 Financial Items (3 parents + 9 children) — unique GUIDs per tenant
- 3 Exchange Rates, 6 Budgets
- 2 Training Centers with TCO/TCM role assignments
- 1 Center Plan (approved) with items
- 1 Annual Plan with 4-6 items at various workflow stages
- 6 Nominations at different approval levels + 1 rejected
- 3 Price Quotes (approved/rejected/pending)
- 2 Course Proposals (pending/approved)

---

## 🔧 Key Fixes Applied During Phase 3

### 1. EstimatedCost auto-calculated (not stored)
**Decision:** Remove `EstimatedCost` from TrainingPlanItem entity. Compute as SUM(PlanItemFinancialItem.EstimatedAmountOMR) at query time via PlanItemCostCalculator.
**Impact:** UpdateEstimatedCostDto removed, cost gate uses calculator.

### 2. TenantCourseName resolved (TODO eliminated)
**Fix:** CourseNameResolver batch-loads TenantCourse → CourseCatalog names. All AppServices now return actual Arabic/English course names.

### 3. UnitId auto-detected
**Fix:** EmployeeResolver.GetCurrentUserUnitIdAsync() → Employee.MainUnitId. TrainingPlanItemAppService.CreateAsync auto-sets UnitId.

### 4. Nomination condition validation (real implementation)
**Fix:** NominationConditionValidator checks all 9 conditions against Employee HR data with Arabic error messages. Blocks nomination if any condition fails.

### 5. OrgUnit Code via reflection
**Fix:** ABP OrganizationUnit.Code has internal setter. Set via `PropertyInfo.SetValue()`. Codes hardcoded in ABP format ("00001.00003.00001").

### 6. Tenant creation removed from seeder
**Fix:** ABP Commercial (Volo.Saas) has strict tenant validation. Seeder now looks up existing tenants by name instead of creating them.

### 7. Financial items unique per tenant
**Fix:** Replaced fixed FinancialItemIds GUIDs with per-tenant `guidGenerator.Create()` stored in `_fiIds` dictionary.

### 8. DevExtreme dropped from frontend
**Decision:** All Phase 3 pages use plain HTML tables, native inputs, CSS modals — styled with GTMS design system (Tailwind-inspired SCSS with gradients, badges, cards, animations). No DevExtreme imports.

### 9. PAGE 3.3 Plan Review — iterated through 5 versions
- **v1→v2:** Bug fixes — inline editing inputs for OMR amounts, `availableFinancialItems` loaded from `FinancialItemService`, `estimatedCost` field name corrected, 30 localization keys added
- **v3:** Unit accordion grouping, per-unit stats (item count + missing cost + total OMR), jump-to-next-missing button, progress bar, expand/collapse all
- **v4:** Per-unit pagination (10 items/page), per-unit cost filter dropdown, batch auto-fill per unit with progress indicator
- **v5 (LOCKED):** Inline expandable rows — course detail card + editable financial items panel expand directly below the clicked row (not at page bottom). One expanded at a time. Data cached via `Map<signal>` keyed by item ID.
- **Financial items dropdown:** Uses native `<optgroup>` elements grouping children under parent category

### 10. PAGE 3.4 Plan Approval v2 — inline expand (read-only)
**Fix:** Same inline expand pattern as Plan Review v5, but everything read-only. Financial items displayed as plain text (no inputs). Missing financials show red alert. Total shown as blue badge in panel header.

### 11. PAGE 3.2 Plan Entry v2 — inline expand (course detail only)
**Fix:** Same inline expand pattern, but shows only course detail (submitter, justification, objectives, duration, conditions). No financial items panel — UTM doesn't see or manage financials.

### 12. BudgetRecalculator domain service
**Fix:** `TrainingBudget.TotalAmount` was always 0 after plan approval. New `BudgetRecalculator` domain service runs inside `FinalApproveAsync`:
1. Gets all `PlanItemFinancialItems` for the approved plan
2. Resolves child → parent FinancialItem
3. SUMs amounts grouped by parent
4. Updates `TrainingBudget.TotalAmount` for (Year, ParentFinancialItemId)
Creates budget record if it doesn't exist yet.

### 13. `EstimatedCost` removed as stored field
**Decision:** Removed from `TrainingPlanItem` entity. Auto-calculated via `PlanItemCostCalculator` as SUM(PlanItemFinancialItem.EstimatedAmountOMR). Same pattern as MOD-23 budgets.

---

## 📐 Patterns Updated (Carry Forward)

### Frontend — NEW pattern (Phase 3+)
- **NO DevExtreme** — use `gtms-design.scss` shared styles
- **HTML tables** with `.gtms-table` / `.gtms-table-wrap` classes
- **Native inputs** with `.form-input` class
- **CSS modals** with `.modal-backdrop` / `.modal-content` / `.modal-sm|md|lg`
- **Status badges** with `.badge-draft` through `.badge-th-approved`
- **Stat cards** with `.stat-card` / `.card-blue|green|purple`
- **Approval chain** with `.approval-step` / `.step-circle` / `.step-done|active|pending`
- **Conditions panel** with `.conditions-panel` / `.condition-tag`
- **Financial panel** editable: `.financial-panel-inline` / read-only: `.financial-panel-readonly`
- **Inline expandable row pattern (LOCKED):** click row → detail expands below (`.detail-row` + `.inline-detail-wrap`). One expanded at a time. Cache loaded data in `Map<signal>` keyed by item ID to avoid re-fetching. `$event.stopPropagation()` on all inputs/buttons inside expanded area.
- **Course detail card** with `.course-detail-card` / `.detail-grid` / `.detail-mini` / `.conditions-inline`
- Signals for all state — individual signals per form field
- `$any($event.target).value` for native input binding
- **No arrow functions in templates** — extract to named methods

### Backend — Domain Services (5)
- **EmployeeResolver** — inject for user/employee/rank/unit resolution (batch-capable)
- **CourseNameResolver** — inject for TenantCourseId → catalog name lookup (batch-capable)
- **PlanItemCostCalculator** — inject for auto-calculated costs (replaces stored field)
- **NominationConditionValidator** — inject for pre-nomination validation (9 conditions, Arabic errors)
- **BudgetRecalculator** — inject in TrainingPlanAppService, called on FinalApproveAsync

### Seeder Pattern
- One `IDataSeedContributor` orchestrator
- Sub-seeders implement `ITransientDependency` + internal interface
- Each step in its own `UnitOfWork` with `requiresNew: true`
- `autoSave: true` on every insert
- Tenants pre-created (not by seeder), looked up by name
- Per-tenant data uses `guidGenerator.Create()` (not fixed GUIDs, except for supra-tenant Ranks/Catalogs)
- OrgUnit Code set via reflection (`PropertyInfo.SetValue`) — ABP has internal setter

---

## 📊 Cumulative State After Phase 3 + Fixes

| Metric | v4.2 | v4.3 | Delta |
|--------|------|------|-------|
| Total Tables | 59 | 72 | +13 (Phase 3 + HR read-only) |
| Total API Endpoints | 58 | ~108 | +50 |
| Implemented Pages | 10 | 17 | +7 |
| Domain Services | 0 | 5 | +5 |
| Backend Files | ~140 | ~215 | +75 |
| Frontend Files | ~60 | ~92 | +32 |
| Seeder Files | 1 | 7 | Refactored |
| Completion | ~40% | ~73% | +33% |

### Development Phases

| Phase | Scope | Status |
|-------|-------|--------|
| Phase 1 | Catalog, Fields, Proposals, TenantCourses | ✅ Complete |
| Phase 2A | Finance: Items, Defaults, Exchange Rates, Budgets | ✅ Complete |
| Phase 2B | Centers: Centers, Plans, Windows, Roles | ✅ Complete |
| Phase 3 | Annual Plans, Nominations, Sessions, Quotes, Providers | ✅ Complete |
| Phase 4 | Casual Courses, Payments, Reallocations, Shared Requests | ⬜ Next |
| Phase 5 | Post-Course, Reports, Evaluations | ⬜ Queued |

### Remaining (Phase 4 + 5)

**Phase 4 — 7 tables, 4 pages:**
- CasualCourse, CasualCourseFinancial, CasualCourseNomination
- TravelAllowancePayment, CoursePayment, BudgetReallocation
- SharedCenterCourseRequest

**Phase 5 — 4 entities (extend existing), 8 pages:**
- Certificate, CourseEvaluation, ProviderEvaluation (add to Nomination)
- SessionCompletionChecker (domain service)
- Results Entry, Certificate Mgmt, Course/Provider Evaluation
- Financial Items Report, Reallocation Report
- Plan Progress Dashboard + Table + Gantt

### Deferred
- IMET, Global International, Academic Studies streams
- Batch documentation update v4 → v5

---

## 📌 Continuation Prompt

```
# GTMS — Phase 4 / More Fixes Session

## Context
GTMS v4.3 — Phase 3 complete + all page fixes done. Ready for Phase 4 or more fixes.

## Completed
- Phase 1: Catalog, Fields, Proposals, TenantCourses ✅
- Phase 2A: Finance ✅
- Phase 2B: Centers ✅
- Phase 3: Annual Plans, Nominations, Sessions, Quotes, Providers ✅
- Page fixes: Plan Review v5, Plan Approval v2, Plan Entry v2 ✅
- BudgetRecalculator wired into FinalApproveAsync ✅

## Key Patterns (LOCKED)
- Frontend: NO DevExtreme — pure HTML/CSS with gtms-design.scss
- Inline expandable rows: click row → detail expands below (not page bottom)
  - Entry: course detail only
  - Review: course detail + editable financial items (inline inputs)
  - Approval: course detail + read-only financial items
- Plan Review: unit accordion + per-unit pagination (10/page) + batch auto-fill + progress bar + jump-to-next-missing
- Financial items dropdown: <optgroup> grouped by parent category
- Inline editing: (blur) → updateAmount/updateNotes endpoints → auto-calc USD
- BudgetRecalculator: runs on FinalApproveAsync, SUMs by parent FinancialItem
- Data cached via Map<signal> keyed by item ID

## Domain Services (5)
- EmployeeResolver, CourseNameResolver, PlanItemCostCalculator, NominationConditionValidator, BudgetRecalculator

## Project Files
Read GTMS-Project-Checkpoint-v4_3.md first for full context recovery.
```

---

## 🔧 Post-Phase 3 Page Fixes (Same Session)

### PAGE 3.3 — Plan Review: 5 iterations (v1 → v5)

**v1-v2:** Bug fixes — inline editable OMR inputs, populated financial items dropdown, correct field name, localization keys added.

**v3:** Unit accordion grouping — per-unit stats, jump-to-next-missing button.

**v4:** Per-unit pagination (10 items/page), batch auto-fill per unit, per-unit cost filter.

**v5 (FINAL):** Inline expandable rows — click a row → course detail card + financial items panel expand directly below the clicked row (not at page bottom). Only one item expanded at a time. Data cached via `Map<signal>` keyed by item ID to avoid re-fetching on re-expand.

### PAGE 3.4 — Plan Approval: inline expand (read-only)

Same inline expand pattern as Review, but:
- Financial items displayed as plain text (no `<input>` fields)
- No auto-fill, add, or delete buttons
- Total shown as badge in panel header
- Missing cost → red alert message inside expanded row
- `financial-panel-readonly` class (sky blue, distinct from editable blue)

### PAGE 3.2 — Plan Entry: inline expand (course detail only)

Same inline expand pattern, but:
- Shows course detail + conditions only (no financial items panel at all)
- UTM doesn't see/manage financial items — that's Staff's job
- Edit/Delete buttons use `$event.stopPropagation()` to not trigger row expand

### Financial Items Dropdown — `<optgroup>` grouped

Children grouped under parent category using native `<optgroup>`:
```
──── تكاليف الدورة (CC) ────
     رسوم التدريب (CC-TF)
     المواد التدريبية (CC-TM)
──── مصاريف السفر (TE) ────
     التذاكر (TE-TK)
     بدل السفر (TE-TA)
     ...
```

### BudgetRecalculator — Domain Service (NEW)

Wired into `TrainingPlanAppService.FinalApproveAsync()`:
1. Gets all PlanItemFinancialItems for the approved plan
2. Resolves each child FinancialItem → ParentId
3. SUMs amounts grouped by parent
4. UPDATEs TrainingBudget.TotalAmount for (Year, ParentFinancialItemId)
5. Creates budget record if it doesn't exist for that parent+year

### Backend: updateAmount + updateNotes endpoints

Added to `PlanItemFinancialItemAppService`:
- `PUT /api/app/plan-item-financial-item/{id}/amount` — updates OMR, auto-calcs USD from exchange rate
- `PUT /api/app/plan-item-financial-item/{id}/notes` — updates notes only

### Localization Keys Added

~42 keys added to en.json and ar.json covering:
- Plan Review page (title, role description, warnings, hints)
- Plan Item columns (course, type, priority, officers, enlisted, capacity, cost, unit)
- Financial item labels (name, amount OMR/USD, notes, total)
- Common actions (save, cancel, delete)
- Plan status labels
- Condition type names

---

## 📐 Patterns Updated (Carry Forward — Post-Fixes)

### Inline Expandable Row Pattern (KEY PATTERN for all future pages)

```typescript
// Signal for which item is expanded (only one at a time)
expandedItemId = signal<string | null>(null);

// Cache loaded data per item to avoid re-fetching
financialItemsMap = signal(new Map<string, PlanItemFinancialItemDto[]>());

// Toggle: collapse if same, expand + load if new
async toggleItemDetail(itemId: string): Promise<void> {
  if (this.expandedItemId() === itemId) { this.expandedItemId.set(null); return; }
  this.expandedItemId.set(itemId);
  if (!this.financialItemsMap().has(itemId)) {
    // Load data and cache it
    const data = await firstValueFrom(this.service.getByItem(itemId));
    this.financialItemsMap.update(m => { const n = new Map(m); n.set(itemId, data); return n; });
  }
}
```

Template pattern:
```html
@for (item of items(); track item.id) {
  <tr class="item-row" [class.item-expanded]="isItemExpanded(item.id)" (click)="toggleItemDetail(item.id)">
    <!-- main row columns -->
  </tr>
  @if (isItemExpanded(item.id)) {
    <tr class="detail-row">
      <td colspan="N">
        <div class="inline-detail-wrap">
          <!-- course detail card + financial panel -->
        </div>
      </td>
    </tr>
  }
}
```

### 5 Domain Services (carry forward)

| Service | Injected By | Purpose |
|---------|------------|---------|
| EmployeeResolver | PlanItemAppService, NominationAppService, HrLookupAppService | CurrentUser → Employee → MainUnitId/Rank |
| CourseNameResolver | PlanItemAppService, NominationAppService | TenantCourseId → CourseCatalog names |
| PlanItemCostCalculator | PlanAppService, PlanItemAppService | SUM(PlanItemFinancialItem.EstimatedAmountOMR) |
| NominationConditionValidator | NominationAppService | 9 condition checks against Employee HR data |
| BudgetRecalculator | PlanAppService.FinalApproveAsync | SUM by parent FinancialItem → update TrainingBudget.TotalAmount |

---

*Checkpoint updated: April 14, 2026*
*Session: GTMS Phase 3 + Page Fixes*
