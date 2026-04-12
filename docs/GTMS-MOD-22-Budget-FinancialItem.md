# GTMS Modification — MOD-22: Budget Linked to Parent FinancialItem
## Date: April 12, 2026

---

## Summary

**Previous:** `TrainingBudgets.BudgetType` is a hardcoded enum with 4 values (Internal, ExternalInternational, Planning, HigherEducation).

**New:** `TrainingBudgets.FinancialItemId` is a FK to parent FinancialItems. Budget categories are dynamic — one budget per parent financial item per year per tenant.

---

## Reason

The 4 hardcoded budget types don't match how tenants actually organize their finances. Each tenant has its own parent FinancialItems hierarchy. Budgets should follow the same structure rather than forcing a fixed classification.

---

## Database Changes

### Modified Table: TrnTrainingBudgets

| Field | Change | Notes |
|-------|--------|-------|
| BudgetType | **REMOVE** | Was `BudgetType` enum column |
| FinancialItemId | **ADD** | `Guid FK → TrnFinancialItems` (parents only, where ParentId is null) |

### Modified Index

| Before | After |
|--------|-------|
| Unique `(TenantId, Year, BudgetType)` | Unique `(TenantId, Year, FinancialItemId)` |

### Removed Enum

Delete `BudgetType.cs` from `Domain.Shared/Training/Enums/`:
```
Internal = 0
ExternalInternational = 1
Planning = 2
HigherEducation = 3
```

---

## Backend File Changes

### Delete
- `Domain.Shared/Training/Enums/BudgetType.cs`

### Modify: Entity
**File:** `Domain/Training/Finance/TrainingBudget.cs`
```csharp
// REMOVE
public BudgetType BudgetType { get; set; }

// ADD
public Guid FinancialItemId { get; set; }
```

### Modify: EF Configuration
**File:** `EntityFrameworkCore/Training/Configurations/FinanceConfiguration.cs` (TrainingBudget section)
```csharp
// REMOVE
b.Property(x => x.BudgetType).HasConversion<string>().HasMaxLength(30);
b.HasIndex(x => new { x.TenantId, x.Year, x.BudgetType }).IsUnique();

// ADD
b.HasOne<FinancialItem>()
    .WithMany()
    .HasForeignKey(x => x.FinancialItemId)
    .OnDelete(DeleteBehavior.Restrict);
b.HasIndex(x => new { x.TenantId, x.Year, x.FinancialItemId }).IsUnique();
```

### Modify: DTOs
**File:** `Application.Contracts/Training/Finance/Dtos/TrainingBudgetDto.cs`
```csharp
// REMOVE
public BudgetType BudgetType { get; set; }

// ADD
public Guid FinancialItemId { get; set; }
public string FinancialItemNameAr { get; set; } = null!;
public string FinancialItemNameEn { get; set; } = null!;
```

**File:** `Application.Contracts/Training/Finance/Dtos/CreateUpdateTrainingBudgetDto.cs`
```csharp
// REMOVE
[Required]
public BudgetType BudgetType { get; set; }

// ADD
[Required]
public Guid FinancialItemId { get; set; }
```

**File:** `Application.Contracts/Training/Finance/Dtos/TrainingBudgetGetListInput.cs` (if BudgetType filter exists)
```csharp
// REMOVE
public BudgetType? BudgetType { get; set; }

// ADD
public Guid? FinancialItemId { get; set; }
```

### Modify: AppService
**File:** `Application/Training/Finance/TrainingBudgetAppService.cs`

**CreateAsync — add validations:**
```csharp
// Validate FinancialItemId is a parent item (ParentId == null)
var financialItem = await financialItemRepository.GetAsync(input.FinancialItemId);
if (financialItem.ParentId != null)
{
    throw new BusinessException("Training:TrainingBudget:MustBeParentItem");
}

// Validate unique (TenantId, Year, FinancialItemId)
var exists = await Repository.AnyAsync(
    x => x.Year == input.Year && x.FinancialItemId == input.FinancialItemId);
if (exists)
{
    throw new BusinessException("Training:TrainingBudget:AlreadyExists");
}
```

**GetListAsync — resolve FinancialItem names:**
```csharp
// Batch load financial item names
var fiIds = entities.Select(e => e.FinancialItemId).Distinct().ToList();
var financialItems = await financialItemRepository.GetListAsync(x => fiIds.Contains(x.Id));
var fiMap = financialItems.ToDictionary(f => f.Id);

var dtos = entities.Select(entity =>
{
    var dto = _toDtoMapper.Map(entity);
    if (fiMap.TryGetValue(entity.FinancialItemId, out var fi))
    {
        dto.FinancialItemNameAr = fi.NameAr;
        dto.FinancialItemNameEn = fi.NameEn;
    }
    return dto;
}).ToList();
```

**Constructor — add repository:**
```csharp
IRepository<FinancialItem, Guid> financialItemRepository
```

### Modify: Mapper
**File:** `Application/Training/Finance/Mappers/TrainingBudgetToDtoMapper.cs`
- Mapperly will auto-map `FinancialItemId` since names match
- `FinancialItemNameAr` and `FinancialItemNameEn` are set manually in AppService (not mapped)

### Modify: Seeder
**File:** `Domain/Training/TrainingDataSeeder.cs` (budget seeder section)
```csharp
// BEFORE: BudgetType = BudgetType.Internal
// AFTER: FinancialItemId = parentFinancialItemId (reference actual seeded parent item)
```

---

## Frontend File Changes

### Modify: Proxy DTO models
**File:** `proxy/training/finance/dtos/models.ts`
```typescript
// REMOVE from TrainingBudgetDto
budgetType: BudgetType;

// ADD to TrainingBudgetDto
financialItemId: string;
financialItemNameAr: string;
financialItemNameEn: string;

// REMOVE from CreateUpdateTrainingBudgetDto
budgetType: BudgetType;

// ADD to CreateUpdateTrainingBudgetDto
financialItemId: string;

// REMOVE BudgetType enum entirely
```

### Modify: Proxy service
**File:** `proxy/training/finance/training-budget.service.ts`
- Replace `budgetType` query param with `financialItemId` in `getList`

### Modify: PAGE 2.4 Training Budgets
**File:** `training/finance/training-budgets/training-budgets.component.ts`

**Stat cards — dynamic:**
```typescript
// BEFORE: 4 fixed cards based on BudgetType enum
// AFTER: load parent financial items, create one card per parent

parentFinancialItems = signal<any[]>([]);

async loadParentItems(): Promise<void> {
  // Use FinancialItem getList filtered to parents only (parentId == null)
  // Or use a dedicated endpoint like getSubItems but for parents
}

// Cards generated dynamically from parentFinancialItems()
```

**Grid column:**
```typescript
// REMOVE: BudgetType column
// ADD: FinancialItemNameAr column
```

**Create/Edit dialog:**
```typescript
// REMOVE: BudgetType dx-select-box with enum values
// ADD: FinancialItem dx-select-box with parent items only
```

---

## Localization Changes

### Remove from en.json / ar.json
```json
"Training.BudgetType.Internal": "Internal Training",
"Training.BudgetType.ExternalInternational": "External International",
"Training.BudgetType.Planning": "Planning",
"Training.BudgetType.HigherEducation": "Higher Education"
```

### Add to en.json / ar.json
```json
"Training.FinancialItemName": "Financial Item",
"Training:TrainingBudget:MustBeParentItem": "Budget must be linked to a parent financial item, not a sub-item.",
"Training:TrainingBudget:AlreadyExists": "A budget already exists for this financial item in the selected year."
```

```json
"Training.FinancialItemName": "البند المالي",
"Training:TrainingBudget:MustBeParentItem": "يجب ربط الميزانية ببند مالي رئيسي وليس بند فرعي.",
"Training:TrainingBudget:AlreadyExists": "توجد ميزانية بالفعل لهذا البند المالي في السنة المحددة."
```

---

## Migration

```sql
-- 1. Add new column
ALTER TABLE TrnTrainingBudgets ADD FinancialItemId uniqueidentifier NOT NULL;

-- 2. Map existing BudgetType values to actual FinancialItem parent IDs
-- This requires manual mapping based on tenant data:
-- UPDATE TrnTrainingBudgets SET FinancialItemId = '<parent-fi-id>' WHERE BudgetType = 'Internal';
-- (repeat for each BudgetType value per tenant)

-- 3. Add FK constraint
ALTER TABLE TrnTrainingBudgets ADD CONSTRAINT FK_TrnTrainingBudgets_FinancialItemId
    FOREIGN KEY (FinancialItemId) REFERENCES TrnFinancialItems(Id);

-- 4. Drop old index, add new
DROP INDEX IX_TrnTrainingBudgets_TenantId_Year_BudgetType ON TrnTrainingBudgets;
CREATE UNIQUE INDEX IX_TrnTrainingBudgets_TenantId_Year_FinancialItemId 
    ON TrnTrainingBudgets(TenantId, Year, FinancialItemId);

-- 5. Drop old column
ALTER TABLE TrnTrainingBudgets DROP COLUMN BudgetType;
```

---

## Impact Summary

| Area | Change |
|------|--------|
| Tables modified | 1 (TrnTrainingBudgets) |
| Enums removed | 1 (BudgetType) |
| Backend files modified | ~6 (entity, EF config, 2 DTOs, AppService, seeder) |
| Frontend files modified | ~3 (proxy models, proxy service, budgets component) |
| New API endpoints | 0 |
| Removed API endpoints | 0 |

---

*Document generated: April 12, 2026*
*Session: Phase 2B wrap-up*
