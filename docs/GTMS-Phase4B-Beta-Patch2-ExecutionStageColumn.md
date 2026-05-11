# GTMS Phase 4B-β — Patch 2: Execution Stage Column on Casual Course List

**Version:** v1.0
**Target:** v4.9.1 → v4.9.2 (patch bump)
**Scope:** Add a computed "Execution Stage" column to the casual course list page (PAGE 4.1). Closes the visibility gap where post-TH-approval execution status isn't visible at the list level.
**Reason:** Staff currently has to click into each course to know its execution stage. With the column, the list answers "where is this course right now?" at a glance.

---

## Why this patch exists

After `THApproved`, casual courses progress through:
1. Quote selection (Staff picks winner)
2. Travel instruction issuance (Staff)
3. Travel allowance payments (Finance, per nominee)
4. Course payment (Finance, one)
5. Reallocation approvals (TD, per reallocation row)

Currently all post-TH courses look identical in the list (all show "TH Approved ✓"). The new column tells Staff exactly where each course is.

---

## Locked decisions

| # | Decision |
|---|---|
| Q1 | Column is hidden/empty for pre-TH rows (Status column tells that story) |
| Q2 | Column is filterable — Staff can filter "show me all 'awaiting quote' courses" |
| Q3 | Internal courses use a simplified 2-stage path; External use the full 5-stage path |

---

## Backend changes

### B1 — Add `ExecutionStageLabel` to `CasualCourseListDto`

**File:** `src/YourApp.Application.Contracts/Training/CasualCourses/CasualCourseListDto.cs`

```csharp
public class CasualCourseListDto : EntityDto<Guid>
{
    // ... existing fields ...
    
    // NEW
    public string? ExecutionStageLabel { get; set; }   // null for pre-THApproved rows
    public ExecutionStage? ExecutionStage { get; set; } // enum for filtering + frontend logic
}
```

### B2 — New enum `ExecutionStage`

**File:** `src/YourApp.Domain.Shared/Training/Enums/ExecutionStage.cs`

```csharp
public enum ExecutionStage
{
    // External flow (Internal courses skip 1, 2, 3)
    AwaitingQuoteSelection      = 1,
    AwaitingTravelInstruction    = 2,   // TravelInstruction null OR Status=Draft
    AwaitingTravelAllowances     = 3,   // some allowances not yet confirmed
    AwaitingCoursePayment        = 4,   // payment null OR Status=Draft
    AwaitingReallocationApproval = 5,   // some reallocations not yet Approved
    FinanciallyComplete          = 6    // everything done
}
```

The enum is used for:
- Frontend filtering (the dropdown filter on the column)
- Programmatic logic (future reports, dashboards)

`ExecutionStageLabel` is the localized display string — derived from the enum but human-readable in current language.

### B3 — `CasualCourseAppService.GetListAsync` — compute the stage

**File:** `src/YourApp.Application/Training/CasualCourses/CasualCourseAppService.cs`

In the `GetListAsync` method, after fetching the courses, compute the stage per row. Use a private helper:

```csharp
private (ExecutionStage? stage, string? label) ComputeExecutionStage(CasualCourse course)
{
    // Only relevant after THApproved
    if (course.Status != CasualCourseStatus.THApproved)
        return (null, null);
    
    // Internal courses skip quote + travel — simpler 2-stage path
    if (course.CourseType == CourseType.Internal)
    {
        if (course.CoursePayment == null || course.CoursePayment.Status != PaymentStatus.Confirmed)
            return (ExecutionStage.AwaitingCoursePayment, 
                    L["Training:ExecutionStage:AwaitingCoursePayment"]);
        
        // Internal courses don't generate reallocations either (no funding scenario flip)
        return (ExecutionStage.FinanciallyComplete, 
                L["Training:ExecutionStage:FinanciallyComplete"]);
    }
    
    // External courses — full 5-stage path
    
    // Stage 1 — Quote selection
    if (course.SelectedPriceQuoteId == null)
        return (ExecutionStage.AwaitingQuoteSelection, 
                L["Training:ExecutionStage:AwaitingQuoteSelection"]);
    
    // Stage 2 — Travel instruction
    var travelInstruction = course.TravelInstruction; // navigation property
    if (travelInstruction == null || travelInstruction.Status != TravelInstructionStatus.Issued)
        return (ExecutionStage.AwaitingTravelInstruction, 
                L["Training:ExecutionStage:AwaitingTravelInstruction"]);
    
    // Stage 3 — Travel allowance payments (per nominee)
    var totalNominees = course.Nominations.Count;
    var confirmedAllowances = course.TravelAllowancePayments
        .Count(p => p.Status == PaymentStatus.Confirmed);
    if (confirmedAllowances < totalNominees)
        return (ExecutionStage.AwaitingTravelAllowances, 
                L["Training:ExecutionStage:AwaitingTravelAllowancesProgress", 
                  confirmedAllowances, totalNominees]);
    
    // Stage 4 — Course payment
    if (course.CoursePayment == null || course.CoursePayment.Status != PaymentStatus.Confirmed)
        return (ExecutionStage.AwaitingCoursePayment, 
                L["Training:ExecutionStage:AwaitingCoursePayment"]);
    
    // Stage 5 — Reallocations (scenario 1 has no reallocations to wait for)
    if (course.FundingScenario == FundingScenario.FundingSourceCoversAll)
        return (ExecutionStage.FinanciallyComplete, 
                L["Training:ExecutionStage:FinanciallyComplete"]);
    
    var totalReallocations = course.BudgetReallocations.Count;
    var approvedReallocations = course.BudgetReallocations
        .Count(r => r.Status == ReallocationStatus.Approved);
    if (approvedReallocations < totalReallocations)
        return (ExecutionStage.AwaitingReallocationApproval, 
                L["Training:ExecutionStage:AwaitingReallocationApprovalProgress",
                  approvedReallocations, totalReallocations]);
    
    return (ExecutionStage.FinanciallyComplete, 
            L["Training:ExecutionStage:FinanciallyComplete"]);
}
```

In `GetListAsync`, populate the new DTO fields:

```csharp
foreach (var dto in result.Items)
{
    var course = courses.First(c => c.Id == dto.Id);
    var (stage, label) = ComputeExecutionStage(course);
    dto.ExecutionStage = stage;
    dto.ExecutionStageLabel = label;
}
```

**Performance note:** `ComputeExecutionStage` reads:
- `CoursePayment` (one-to-one with course)
- `TravelInstruction` (one-to-one polymorphic)
- `TravelAllowancePayments` (collection)
- `BudgetReallocations` (collection)
- `Nominations` (collection)

Make sure the repository call includes these via `WithDetailsAsync()` or explicit `Include`. If `GetListAsync` was returning lightweight projections without these navigation properties, you'll need to add them — this will increase the query size but is unavoidable for accurate computation.

### B4 — Filter support in `CasualCourseGetListInput`

**File:** `src/YourApp.Application.Contracts/Training/CasualCourses/CasualCourseGetListInput.cs`

```csharp
public class CasualCourseGetListInput : PagedAndSortedResultRequestDto
{
    // ... existing fields ...
    
    // NEW
    public ExecutionStage? ExecutionStage { get; set; }
}
```

In `GetListAsync`, apply the filter:

```csharp
if (input.ExecutionStage.HasValue)
{
    // Filter has to be applied AFTER stage computation since it's a derived field.
    // Alternative: build the query so each stage maps to specific DB conditions
    // (faster but more verbose).
    var filtered = result.Items.Where(d => d.ExecutionStage == input.ExecutionStage).ToList();
    result = new PagedResultDto<CasualCourseListDto>(filtered.Count, filtered);
}
```

**Trade-off:** Post-filter is slow if the dataset is huge. For now (tens to low-hundreds of post-TH courses per tenant), it's fine. If performance becomes an issue, refactor to query-level filtering.

### B5 — Localization keys

**Files:** `ar.json` + `en.json`

```json
// ar.json
"Training.ExecutionStage.AwaitingQuoteSelection": "بانتظار اختيار العرض",
"Training.ExecutionStage.AwaitingTravelInstruction": "بانتظار إصدار تعليمات السفر",
"Training.ExecutionStage.AwaitingTravelAllowances": "بدلات السفر",
"Training.ExecutionStage.AwaitingTravelAllowancesProgress": "بدلات السفر: {0}/{1} مؤكدة",
"Training.ExecutionStage.AwaitingCoursePayment": "بانتظار دفع رسوم الدورة",
"Training.ExecutionStage.AwaitingReallocationApproval": "إعادة التخصيص",
"Training.ExecutionStage.AwaitingReallocationApprovalProgress": "إعادة التخصيص: {0}/{1} معتمدة",
"Training.ExecutionStage.FinanciallyComplete": "✓ مكتملة مالياً",
"Training.ExecutionStage.Column": "مرحلة التنفيذ",

// en.json (mirrored)
"Training.ExecutionStage.AwaitingQuoteSelection": "Awaiting Quote Selection",
"Training.ExecutionStage.AwaitingTravelInstruction": "Awaiting Travel Instruction",
"Training.ExecutionStage.AwaitingTravelAllowances": "Travel Allowances",
"Training.ExecutionStage.AwaitingTravelAllowancesProgress": "Travel Allowances: {0}/{1} confirmed",
"Training.ExecutionStage.AwaitingCoursePayment": "Awaiting Course Payment",
"Training.ExecutionStage.AwaitingReallocationApproval": "Reallocation Approvals",
"Training.ExecutionStage.AwaitingReallocationApprovalProgress": "Reallocations: {0}/{1} approved",
"Training.ExecutionStage.FinanciallyComplete": "✓ Financially Complete",
"Training.ExecutionStage.Column": "Execution Stage"
```

### B6 — Backend verification

After applying B1–B5:

```bash
dotnet build
```

Manual test via Swagger `/api/app/casual-courses` (GET):
- Get a course in `THApproved` state without a selected quote → response should have `ExecutionStage = AwaitingQuoteSelection`, `ExecutionStageLabel = "بانتظار اختيار العرض"`
- Get a course at `Draft` → `ExecutionStage = null`, `ExecutionStageLabel = null`
- Filter by `?ExecutionStage=1` → only courses awaiting quote selection returned

---

## Frontend changes

### F1 — Proxy regenerate

```bash
abp generate-proxy -t ng
```

Verify:
- `CasualCourseListDto` interface has new `executionStage` + `executionStageLabel` fields
- `ExecutionStage` enum is exported
- `CasualCourseGetListInput` accepts `executionStage?`

### F2 — Add column to the grid

**File:** `src/app/training/casual-courses/list/casual-courses-list.component.html` (or equivalent)

Add the column AFTER the existing Status column:

```html
<dx-data-grid [dataSource]="dataSource">
  <!-- ... existing columns ... -->
  
  <dxi-column dataField="status" caption="..."></dxi-column>
  
  <!-- NEW column -->
  <dxi-column 
    dataField="executionStageLabel" 
    [caption]="'::Training.ExecutionStage.Column' | abpLocalization"
    [allowFiltering]="true"
    [allowSorting]="false"
    cellTemplate="executionStageTpl"
    [width]="220">
    <dxo-header-filter [dataSource]="executionStageFilterValues"></dxo-header-filter>
  </dxi-column>
  
  <div *dxTemplate="let cell of 'executionStageTpl'">
    @if (cell.value) {
      <span [ngClass]="getExecutionStageClass(cell.data.executionStage)">
        {{ cell.value }}
      </span>
    } @else {
      <span class="text-slate-400">—</span>
    }
  </div>
  
  <!-- ... rest of columns ... -->
</dx-data-grid>
```

### F3 — Component logic

**File:** `src/app/training/casual-courses/list/casual-courses-list.component.ts`

```typescript
import { ExecutionStage } from '@proxy/training/casual-courses';

// In the component class:

// Filter values for the column header dropdown
executionStageFilterValues = [
  { value: null, text: this.l.t('::Common.All') },
  { value: ExecutionStage.AwaitingQuoteSelection, 
    text: this.l.t('::Training.ExecutionStage.AwaitingQuoteSelection') },
  { value: ExecutionStage.AwaitingTravelInstruction, 
    text: this.l.t('::Training.ExecutionStage.AwaitingTravelInstruction') },
  { value: ExecutionStage.AwaitingTravelAllowances, 
    text: this.l.t('::Training.ExecutionStage.AwaitingTravelAllowances') },
  { value: ExecutionStage.AwaitingCoursePayment, 
    text: this.l.t('::Training.ExecutionStage.AwaitingCoursePayment') },
  { value: ExecutionStage.AwaitingReallocationApproval, 
    text: this.l.t('::Training.ExecutionStage.AwaitingReallocationApproval') },
  { value: ExecutionStage.FinanciallyComplete, 
    text: this.l.t('::Training.ExecutionStage.FinanciallyComplete') }
];

// Cell color coding
getExecutionStageClass(stage: ExecutionStage | null): string {
  if (stage === null || stage === undefined) return '';
  
  switch (stage) {
    case ExecutionStage.AwaitingQuoteSelection:
    case ExecutionStage.AwaitingTravelInstruction:
    case ExecutionStage.AwaitingCoursePayment:
      return 'badge-stage-pending';      // amber/yellow tones
    
    case ExecutionStage.AwaitingTravelAllowances:
    case ExecutionStage.AwaitingReallocationApproval:
      return 'badge-stage-progress';     // blue tones (partial progress)
    
    case ExecutionStage.FinanciallyComplete:
      return 'badge-stage-complete';     // green tones
    
    default:
      return '';
  }
}
```

### F4 — SCSS for the badge classes

**File:** `src/app/training/casual-courses/list/casual-courses-list.component.scss`

```scss
.badge-stage-pending {
  display: inline-block;
  padding: 3px 10px;
  border-radius: 6px;
  font-size: 12px;
  font-weight: 600;
  background: #fef3c7;
  color: #92400e;
}

.badge-stage-progress {
  display: inline-block;
  padding: 3px 10px;
  border-radius: 6px;
  font-size: 12px;
  font-weight: 600;
  background: #dbeafe;
  color: #1e40af;
}

.badge-stage-complete {
  display: inline-block;
  padding: 3px 10px;
  border-radius: 6px;
  font-size: 12px;
  font-weight: 600;
  background: #dcfce7;
  color: #166534;
}
```

### F5 — Frontend verification

After applying F1–F4:

```bash
ng build
```

Manual test:
1. Open casual courses list
2. Verify the new column appears after Status
3. Pre-TH rows show `—` in execution stage cell
4. Post-TH rows show the appropriate label with color coding
5. Click the column filter dropdown → select "Awaiting Quote Selection" → list narrows to those courses
6. Switch to English language → labels update to English
7. Resize window — column still readable

---

## Cross-cutting

### Internal courses behavior

Internal courses follow the simplified path:
- `THApproved` + `CoursePayment.Status != Confirmed` → "Awaiting Course Payment"
- `THApproved` + `CoursePayment.Status == Confirmed` → "Financially Complete"

They skip stages 1-3 (no quote, no travel) entirely.

### Performance considerations

The `ComputeExecutionStage` method reads multiple navigation properties per course. For lists with 100+ post-TH courses, this could slow down `GetListAsync`. Two mitigations:

1. **Use `WithDetailsAsync()`** with explicit selectors to load only needed navigation properties
2. **Server-side caching** if list is re-fetched frequently (lookup by ETag etc.) — defer to Phase 5+ if needed

For Phase 4B-β scale (low-hundreds of courses per tenant), no optimization needed.

---

## Verification checklist

- [ ] Backend `CasualCourseListDto` has `ExecutionStage` + `ExecutionStageLabel` fields
- [ ] New enum `ExecutionStage` defined with 6 values
- [ ] `ComputeExecutionStage` method handles all paths (Internal vs External, all 5 stages, scenario 1 skip-reallocations)
- [ ] `GetListAsync` populates the new fields
- [ ] Filter parameter `ExecutionStage` works in `GetListAsync`
- [ ] All localization keys added in both `ar.json` and `en.json`
- [ ] Backend builds clean, manual Swagger tests pass for at least 3 scenarios
- [ ] Frontend proxy regenerated, new fields visible
- [ ] Grid column appears with correct positioning
- [ ] Column hidden value for pre-TH rows shows `—`
- [ ] Color coding applies (amber/blue/green per stage type)
- [ ] Filter dropdown works
- [ ] English/Arabic both render correctly

---

## Commit message suggestion

```
feat(casual-courses): execution stage column on list page

Adds visibility into post-TH execution progress at the list level.
Closes the gap where Staff had to click into each course to see whether
it needed quote selection, travel instruction, payments, or reallocations.

Changes:
- New enum ExecutionStage with 6 values (5 stages + complete)
- CasualCourseListDto extended with ExecutionStage + ExecutionStageLabel
- CasualCourseAppService.GetListAsync computes stage per row
  - External courses: 5-stage path (quote → travel → allowances → 
    payment → reallocations)
  - Internal courses: 2-stage path (payment → complete)
  - Scenario 1 skips reallocation stage (no reallocations generated)
- Filter param ExecutionStage added to GetListInput
- New localization keys in ar.json + en.json
- Grid column with header filter dropdown, color-coded badges:
  - Amber: awaiting (single action needed)
  - Blue: in progress (partial completion)
  - Green: complete

Bumps v4.9.1 → v4.9.2
```

---

*End of Phase 4B-β Patch 2 — v1.0*
*Estimated effort: ~45 minutes Claude Code work. Backend computation + DTO + frontend column + filter + localization. No architectural risk.*
