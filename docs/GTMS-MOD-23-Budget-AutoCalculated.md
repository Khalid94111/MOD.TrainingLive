# GTMS Modification — MOD-23: TrainingBudgets Auto-Calculated from Approved Plans
## Date: April 12, 2026
## Depends on: MOD-22 (FinancialItemId replaces BudgetType)

---

## Summary

**Previous:** `TrainingBudgets` is a full CRUD entity. TD manually enters `TotalAmount` on PAGE 2.4.

**New:** `TrainingBudgets` is an auto-calculated summary. `TotalAmount` is the sum of `EstimatedAmount` from `PlanItemFinancialItems` for approved plan items, grouped by parent FinancialItem. Only `AlertThreshold` is editable by TD. PAGE 2.4 becomes read-only with threshold editing only.

---

## How TotalAmount is Calculated

```
TotalAmount = SUM(PlanItemFinancialItems.EstimatedAmount)
  WHERE PlanItemFinancialItems.FinancialItemId belongs to this parent FinancialItem
    AND PlanItemFinancialItems.PlanItem → TrainingPlan is approved
    AND TrainingPlan.Year = TrainingBudgets.Year
    AND TrainingPlan.TenantId = TrainingBudgets.TenantId
```

The join path:
```
TrainingBudgets.FinancialItemId (parent)
    ↑ matches parent of...
FinancialItems (sub-items where ParentId = TrainingBudgets.FinancialItemId)
    ↑ referenced by...
PlanItemFinancialItems.FinancialItemId (sub-item)
    ↑ linked to...
TrainingPlanItems (approved plan items)
    ↑ linked to...
TrainingPlans (Status = Approved, Year = budget year)
```

---

## When TotalAmount Recalculates

| Trigger | Action |
|---------|--------|
| Annual plan approved | Recalculate all budgets for that plan's year + tenant |
| Plan item added/edited/deleted (on approved plan) | Recalculate affected budget (by parent FinancialItem) |
| PlanItemFinancialItems changed | Recalculate affected budget |
| Plan unapproved/returned | Recalculate (amounts removed) |

---

## Database Changes

### Modified Table: TrnTrainingBudgets

| Field | Change | Notes |
|-------|--------|-------|
| TotalAmount | **Behavior change** | No longer user-entered. Auto-calculated. Still stored (not computed column). |
| SpentAmount | Unchanged | Updated when payments confirmed |
| AlertThreshold | Unchanged | Only field editable by TD |

No schema changes — `TotalAmount` remains a `decimal` column. The change is behavioral (who writes to it).

### Auto-Creation Rule

Budget records are **auto-created** when an annual plan is approved, if they don't already exist. One budget per parent FinancialItem per year per tenant.

```
On plan approval:
  1. Get all PlanItemFinancialItems for this plan's items
  2. Group by parent FinancialItem (via FinancialItem.ParentId)
  3. For each parent:
     a. Find or create TrainingBudget (TenantId, Year, FinancialItemId)
     b. Set TotalAmount = SUM of EstimatedAmount for that parent's sub-items
     c. Set AlertThreshold = 80 (default, if new record)
```

---

## Backend Changes

### Remove from TrainingBudgetAppService

```csharp
// REMOVE these endpoints
CreateAsync()   // Budgets are auto-created on plan approval
DeleteAsync()   // Budgets should not be deleted manually
```

### Keep / Modify

```csharp
// KEEP — read-only list
GetListAsync()  // Returns budgets with calculated Remaining

// KEEP — read-only single
GetAsync()      // Returns single budget

// MODIFY — only allows AlertThreshold update
UpdateAsync()   // Only accepts AlertThreshold, ignores TotalAmount/SpentAmount
```

### New: UpdateAlertThresholdDto

```csharp
public class UpdateAlertThresholdDto
{
    [Required]
    public decimal AlertThreshold { get; set; }
}
```

### Modify: TrainingBudgetAppService.UpdateAsync

```csharp
public async Task<TrainingBudgetDto> UpdateAsync(Guid id, UpdateAlertThresholdDto input)
{
    var entity = await Repository.GetAsync(id);
    entity.AlertThreshold = input.AlertThreshold;
    await Repository.UpdateAsync(entity, autoSave: true);
    return MapToDto(entity);
}
```

### New: Budget Recalculation Service

Create a domain service or method that recalculates budgets. Called from plan approval and plan item changes.

```csharp
// Domain/Training/Finance/BudgetRecalculationService.cs

public class BudgetRecalculationService(
    IRepository<TrainingBudget, Guid> budgetRepository,
    IRepository<PlanItemFinancialItem, Guid> planItemFinancialRepository,
    IRepository<TrainingPlanItem, Guid> planItemRepository,
    IRepository<FinancialItem, Guid> financialItemRepository)
{
    /// <summary>
    /// Recalculates all budgets for a given tenant + year.
    /// Called when a plan is approved/unapproved or plan items change.
    /// </summary>
    public async Task RecalculateAsync(Guid tenantId, int year)
    {
        // 1. Get all approved plan item IDs for this tenant + year
        var approvedPlanItems = await planItemRepository.GetListAsync(
            x => x.Plan.TenantId == tenantId
              && x.Plan.Year == year
              && x.Plan.Status == PlanStatus.Approved);

        var planItemIds = approvedPlanItems.Select(x => x.Id).ToList();

        // 2. Get all PlanItemFinancialItems for those plan items
        var allFinancials = await planItemFinancialRepository.GetListAsync(
            x => planItemIds.Contains(x.PlanItemId));

        // 3. Get all financial items to resolve parent IDs
        var fiIds = allFinancials.Select(x => x.FinancialItemId).Distinct().ToList();
        var financialItems = await financialItemRepository.GetListAsync(
            x => fiIds.Contains(x.Id));
        var fiMap = financialItems.ToDictionary(x => x.Id);

        // 4. Group by parent FinancialItem and sum
        var grouped = allFinancials
            .Where(x => fiMap.ContainsKey(x.FinancialItemId))
            .GroupBy(x => fiMap[x.FinancialItemId].ParentId ?? fiMap[x.FinancialItemId].Id)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.EstimatedAmount));

        // 5. Find or create budget records, update TotalAmount
        foreach (var (parentFiId, total) in grouped)
        {
            var budget = await budgetRepository.FindAsync(
                x => x.TenantId == tenantId
                  && x.Year == year
                  && x.FinancialItemId == parentFiId);

            if (budget == null)
            {
                budget = new TrainingBudget
                {
                    TenantId = tenantId,
                    Year = year,
                    FinancialItemId = parentFiId,
                    TotalAmount = total,
                    SpentAmount = 0,
                    AlertThreshold = 80
                };
                await budgetRepository.InsertAsync(budget, autoSave: true);
            }
            else
            {
                budget.TotalAmount = total;
                await budgetRepository.UpdateAsync(budget, autoSave: true);
            }
        }

        // 6. Zero out budgets for parent FIs that no longer have approved items
        var existingBudgets = await budgetRepository.GetListAsync(
            x => x.TenantId == tenantId && x.Year == year);

        foreach (var budget in existingBudgets)
        {
            if (!grouped.ContainsKey(budget.FinancialItemId))
            {
                budget.TotalAmount = 0;
                await budgetRepository.UpdateAsync(budget, autoSave: true);
            }
        }
    }
}
```

### Integration Points (Phase 3)

When implementing the Annual Plan in Phase 3, call recalculation at these points:

```csharp
// In TrainingPlanAppService:

public async Task<TrainingPlanDto> ApproveAsync(Guid id)
{
    // ... existing approval logic ...
    entity.Status = PlanStatus.Approved;
    await Repository.UpdateAsync(entity, autoSave: true);

    // Recalculate budgets
    await budgetRecalculationService.RecalculateAsync(
        entity.TenantId!.Value, entity.Year);

    return MapToDto(entity);
}

// Also call on: ReturnAsync, plan item CRUD (if plan is approved)
```

---

## Frontend Changes

### PAGE 2.4: Training Budgets — Now Read-Only Dashboard

**Remove:**
- "Add Budget" button
- Delete action on grid rows
- Create dialog
- TotalAmount field in any edit form

**Keep:**
- Stat cards (dynamic, one per parent FinancialItem)
- Grid showing all budgets
- AlertThreshold editing (inline edit or small dialog)

**Modify grid columns:**

| # | Column | Editable | Notes |
|---|--------|----------|-------|
| 1 | Financial Item | No | FinancialItemNameAr |
| 2 | Total Amount | No | Auto-calculated, display only |
| 3 | Spent Amount | No | Auto-updated on payment |
| 4 | Remaining | No | Computed: Total - Spent |
| 5 | Alert Threshold | **Yes** | Inline edit or edit button |
| 6 | Status | No | Progress bar or percentage |

**Stat cards:**
- Dynamic count (one per budget record, not fixed 4)
- Each card shows: FinancialItemName, TotalAmount, SpentAmount, Remaining, progress bar
- Color: green (under threshold), amber (near threshold), red (over threshold)

**AlertThreshold editing:**
- Either inline editing on the grid column
- Or small popup with single field when clicking edit button

### Modify: Proxy service

```typescript
// REMOVE
create()
delete()

// MODIFY update — only AlertThreshold
update = (id: string, input: UpdateAlertThresholdDto, config?: Partial<Rest.Config>) =>
  this.restService.request<any, TrainingBudgetDto>({
    method: 'PUT',
    url: `/api/app/training-budget/${id}`,
    body: input,
  }, { apiName: this.apiName, ...config });
```

### Modify: Proxy DTO models

```typescript
// ADD
export interface UpdateAlertThresholdDto {
  alertThreshold: number;
}

// TrainingBudgetDto — no changes needed (TotalAmount still exists, just read-only)
```

---

## Localization Changes

### Remove
```json
"Training.AddBudget": "...",
"Training.EditBudget": "..."
```

### Add
```json
// en.json
"Training.AlertThreshold": "Alert Threshold",
"Training.EditThreshold": "Edit Threshold",
"Training.BudgetAutoCalculated": "Budget amounts are automatically calculated from approved annual plan items",
"Training.Remaining": "Remaining"

// ar.json
"Training.AlertThreshold": "حد التنبيه",
"Training.EditThreshold": "تعديل حد التنبيه",
"Training.BudgetAutoCalculated": "مبالغ الميزانية تُحسب تلقائياً من بنود الخطة السنوية المعتمدة",
"Training.Remaining": "المتبقي"
```

---

## Seeder Changes

```csharp
// Budget seeder should create sample budgets linked to parent financial items
// with realistic TotalAmount values (as if calculated from approved plan items)
// Since no approved plans exist at seed time, use sample amounts
```

---

## Impact Summary

| Area | Change |
|------|--------|
| Tables modified | 0 (behavior change only, no schema change) |
| Endpoints removed | 2 (Create, Delete) |
| Endpoints modified | 1 (Update — only AlertThreshold) |
| New backend service | 1 (BudgetRecalculationService) |
| Frontend pages modified | 1 (PAGE 2.4 → read-only dashboard) |
| Phase 3 dependency | Plan approval must call BudgetRecalculationService |

---

## Dependency Chain

```
MOD-22 (FinancialItemId replaces BudgetType)
  └→ MOD-23 (TotalAmount auto-calculated from approved plans)
       └→ Phase 3 (Annual Plan approval triggers recalculation)
```

MOD-22 + MOD-23 can be implemented now in Phase 2A code.
The recalculation trigger (calling the service on plan approval) is wired in Phase 3.

---

*Document generated: April 12, 2026*
*Session: Phase 2B wrap-up + Budget redesign*
