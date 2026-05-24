# GTMS Phase 4C-α — Patch 1: Course Info Bar + Variance Indicators

**Version:** v1.0
**Target:** v4.10.0 → v4.10.1 (patch bump)
**Scope:** Add a sticky Course Info Bar to both Session Detail and Casual Course Detail pages. Add live variance indicators in Add Quote / Pick Winner dialogs and in the quotes table.
**Reason:** Currently when Staff adds a price quote, they don't see the approved cost / course duration / nominee count without expanding Section 1. The Info Bar keeps this context visible at all times, and variance indicators make over/under-budget quotes immediately obvious.

---

## What's changing

**Before this patch:**
- Session Detail and Casual Course Detail pages show sections only — no persistent context
- Adding a quote requires the user to remember (or re-open Section 1) the approved cost
- Quotes table doesn't show how each quote compares to the approved cost

**After this patch:**
- A new sticky Course Info Bar sits below the status pipeline, always visible
- Quotes table adds a "Variance" column (4-tier color coding: green/gray/yellow/red)
- Add Quote dialog shows live variance as the user types the price
- Pick Winner dialog summarizes the variance prominently before confirmation

---

## Locked decisions

| # | Decision | Rationale |
|---|---|---|
| Apply to | Both Sessions AND Casual Courses | Consistency across the system |
| Component reuse | Single `CourseInfoBarComponent` accepts polymorphic input | DRY — avoid duplication |
| Variance thresholds | 4 tiers: ok / neutral / warn / danger at -10% / 0% / +10% | Conservative defaults |
| Casual-specific field | Show "Funding Scenario" (1/2/3) ONLY for casual courses | Sessions have no scenario |
| Session-specific field | Show "Unit Total" (cost × nominees count if applicable) | Useful for sessions where dates matter |
| Sticky behavior | Info Bar sticks below the status pipeline at scroll | Always visible during section work |

---

## Backend changes

The patch is mostly frontend, but the backend needs to expose computed values for the Info Bar.

### B1 — Extend `CourseSessionDetailDto`

Verify these fields exist (add if missing):

```csharp
public class CourseSessionDetailDto : EntityDto<Guid>
{
    // ... existing fields ...

    // For Info Bar
    public string TenantCourseNameAr { get; set; }
    public string? TenantCourseNameEn { get; set; }
    public int DurationDays { get; set; }           // Resolved from TenantCourse
    public int NomineesCount { get; set; }          // Computed: Nominations.Count
    public int OfficersCount { get; set; }          // Filtered by Rank.IsOfficer
    public int EnlistedCount { get; set; }
    public decimal ApprovedCostOMR { get; set; }    // From TrainingPlanItem.EstimatedTotalCost
    public decimal UnitTotalOMR { get; set; }       // Same as ApprovedCostOMR for sessions
}
```

Notes:
- `ApprovedCostOMR` comes from the source TrainingPlanItem (which itself inherits from approved annual plan)
- `UnitTotalOMR` for sessions equals ApprovedCostOMR (one session per plan item)
- `OfficersCount` / `EnlistedCount` computed via Rank lookup per Nomination

### B2 — Extend `CasualCourseDetailDto`

Verify (or add):

```csharp
public class CasualCourseDetailDto : EntityDto<Guid>
{
    // ... existing fields ...

    // For Info Bar — most likely already exist:
    public string TenantCourseNameAr { get; set; }
    public string? TenantCourseNameEn { get; set; }
    public int DurationDays { get; set; }
    public int NomineesCount { get; set; }
    public int OfficersCount { get; set; }
    public int EnlistedCount { get; set; }
    public decimal EstimatedTotalCost { get; set; }   // Already exists from 4A
    public FundingScenario? FundingScenario { get; set; }  // Already exists, but expose
    public string? FundingScenarioLabel { get; set; }  // Localized label, computed
}
```

### B3 — No new endpoints

The Info Bar reads from the existing detail endpoints. Just ensure the DTOs return the needed fields.

### B4 — Verify mapping

In `CourseSessionToDtoMapper` and `CasualCourseToDtoMapper` (Mapperly class-based mappers), ensure all Info Bar fields are populated. Add computation logic in mapper or in the AppService GetDetail method as needed.

---

## Frontend changes

### F1 — New shared component: `CourseInfoBarComponent`

**File:** `src/app/training/shared/course-info-bar/course-info-bar.component.ts`

A standalone, presentational component that accepts polymorphic input.

```typescript
import { Component, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';

export interface CourseInfoBarData {
  // Common fields
  courseName: string;
  durationDays: number;
  nomineesCount: number;
  officersCount: number;
  enlistedCount: number;
  approvedCostOMR: number;
  
  // Session-only
  unitTotalOMR?: number;
  
  // Casual-only
  fundingScenarioLabel?: string;
}

@Component({
  standalone: true,
  selector: 'app-course-info-bar',
  templateUrl: './course-info-bar.component.html',
  styleUrls: ['./course-info-bar.component.scss'],
  imports: [CommonModule, LocalizationPipe],
})
export class CourseInfoBarComponent {
  data = input.required<CourseInfoBarData>();
  variant = input<'casual' | 'session'>('session');
  showScenario = computed(() => 
    this.variant() === 'casual' && !!this.data().fundingScenarioLabel
  );
  showUnitTotal = computed(() => 
    this.variant() === 'session' && !!this.data().unitTotalOMR
  );
}
```

**Template highlights:**
```html
<div class="course-info-bar">
  <div class="course-info-icon">📚</div>
  
  <div class="course-info-bit">
    <span class="cib-label">{{ '::Training.InfoBar.Course' | abpLocalization }}</span>
    <span class="cib-value">{{ data().courseName }}</span>
  </div>
  
  <div class="course-info-bit">
    <span class="cib-label">{{ '::Training.InfoBar.Duration' | abpLocalization }}</span>
    <span class="cib-value">{{ data().durationDays }} {{ '::Training.Days' | abpLocalization }}</span>
  </div>
  
  <div class="course-info-bit">
    <span class="cib-label">{{ '::Training.InfoBar.Nominees' | abpLocalization }}</span>
    <span class="cib-value">
      {{ data().nomineesCount }}
      <span class="text-xs text-slate-500">
        ({{ data().officersCount }} {{ '::Training.Officers' | abpLocalization }}
         · {{ data().enlistedCount }} {{ '::Training.Enlisted' | abpLocalization }})
      </span>
    </span>
  </div>
  
  @if (showScenario()) {
    <div class="course-info-bit">
      <span class="cib-label">{{ '::Training.InfoBar.FundingScenario' | abpLocalization }}</span>
      <span class="cib-value text-orange-600">{{ data().fundingScenarioLabel }}</span>
    </div>
  }
  
  <div class="course-info-bit">
    <span class="cib-label">{{ '::Training.InfoBar.ApprovedCost' | abpLocalization }}</span>
    <span class="cib-value-money">{{ data().approvedCostOMR | number:'1.0-3' }} {{ '::Training.OMR' | abpLocalization }}</span>
  </div>
  
  @if (showUnitTotal()) {
    <div class="course-info-bit">
      <span class="cib-label">{{ '::Training.InfoBar.UnitTotal' | abpLocalization }}</span>
      <span class="cib-value-money">{{ data().unitTotalOMR | number:'1.0-3' }} {{ '::Training.OMR' | abpLocalization }}</span>
    </div>
  }
</div>
```

**SCSS:** mirror styles from the mockup. The bar has:
- Linear gradient background (light blue to light yellow)
- Soft purple border
- Sticky positioning when used in detail pages
- Vertical separators between "bits"

### F2 — Wire into Session Detail page

**File:** `src/app/training/sessions/session-detail/session-detail.component.html`

Insert below the status pipeline, above Section 1:

```html
<!-- After the pipeline div ends -->
@if (session(); as s) {
  <app-course-info-bar 
    [data]="infoBarData()" 
    variant="session"
    class="info-bar-sticky">
  </app-course-info-bar>
}
```

In the `.ts`:
```typescript
infoBarData = computed<CourseInfoBarData | null>(() => {
  const s = this.session();
  if (!s) return null;
  return {
    courseName: s.tenantCourseNameAr || s.tenantCourseNameEn || '',
    durationDays: s.durationDays,
    nomineesCount: s.nomineesCount,
    officersCount: s.officersCount,
    enlistedCount: s.enlistedCount,
    approvedCostOMR: s.approvedCostOMR,
    unitTotalOMR: s.unitTotalOMR,
  };
});
```

**Sticky positioning** — apply to the wrapper class:
```scss
.info-bar-sticky {
  position: sticky;
  top: calc(64px + 80px); /* header height + pipeline height */
  z-index: 5;
  margin-bottom: 16px;
  display: block;
}
```

### F3 — Wire into Casual Course Detail page

**File:** `src/app/training/casual-courses/casual-course-detail/casual-course-detail.component.html`

Same insertion pattern. Compute `infoBarData()`:

```typescript
infoBarData = computed<CourseInfoBarData | null>(() => {
  const c = this.course();
  if (!c) return null;
  return {
    courseName: c.tenantCourseNameAr || c.tenantCourseNameEn || '',
    durationDays: c.durationDays,
    nomineesCount: c.nomineesCount,
    officersCount: c.officersCount,
    enlistedCount: c.enlistedCount,
    approvedCostOMR: c.estimatedTotalCost,
    fundingScenarioLabel: c.fundingScenarioLabel,
  };
});
```

Use `variant="casual"` in the template.

### F4 — Variance helper service

**File:** `src/app/training/shared/variance/variance.helper.ts`

Pure computation helper (not a service — just a function module).

```typescript
export interface VarianceResult {
  amountOMR: number;            // (quoted - approved)
  percentDifference: number;    // ((quoted - approved) / approved) * 100
  tier: 'ok' | 'neutral' | 'warn' | 'danger';
  cssClass: string;             // 'variance-ok' | 'variance-neutral' | etc
}

export function computeVariance(quotedPriceOMR: number, approvedCostOMR: number): VarianceResult {
  if (approvedCostOMR <= 0) {
    return { amountOMR: 0, percentDifference: 0, tier: 'neutral', cssClass: 'variance-neutral' };
  }
  const diff = quotedPriceOMR - approvedCostOMR;
  const pct = (diff / approvedCostOMR) * 100;
  
  let tier: 'ok' | 'neutral' | 'warn' | 'danger';
  if (pct < -2) tier = 'ok';           // significant savings
  else if (pct < 2) tier = 'neutral';   // within ±2% noise
  else if (pct < 10) tier = 'warn';     // moderate overrun
  else tier = 'danger';                  // significant overrun
  
  return {
    amountOMR: Math.abs(diff),
    percentDifference: pct,
    tier,
    cssClass: `variance-${tier}`,
  };
}

export function varianceArrow(tier: VarianceResult['tier']): string {
  return tier === 'ok' || (tier === 'neutral' && false) ? '↓' : '↑';
}

export function varianceSign(pct: number): string {
  if (pct < 0) return '↓';
  if (pct === 0) return '=';
  return '↑';
}
```

### F5 — New `VarianceChipComponent`

A reusable presentational component for the variance pill.

**File:** `src/app/training/shared/variance/variance-chip.component.ts`

```typescript
import { Component, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { computeVariance, VarianceResult } from './variance.helper';

@Component({
  standalone: true,
  selector: 'app-variance-chip',
  template: `
    <span [class]="result().cssClass + ' variance-live'">
      {{ symbol() }}
      <span class="variance-pct">{{ pctDisplay() }}</span>
      <span class="variance-amount">({{ amountDisplay() }})</span>
    </span>
  `,
  styleUrls: ['./variance-chip.component.scss'],
  imports: [CommonModule, LocalizationPipe],
})
export class VarianceChipComponent {
  quotedPrice = input.required<number>();
  approvedCost = input.required<number>();
  
  result = computed<VarianceResult>(() => 
    computeVariance(this.quotedPrice(), this.approvedCost())
  );
  
  symbol = computed(() => {
    const pct = this.result().percentDifference;
    if (pct < 0) return '↓';
    if (Math.abs(pct) < 0.01) return '=';
    return '↑';
  });
  
  pctDisplay = computed(() => {
    const pct = this.result().percentDifference;
    const sign = pct >= 0 ? '+' : '';
    return `${sign}${pct.toFixed(1)}%`;
  });
  
  amountDisplay = computed(() => {
    return `${this.result().amountOMR.toLocaleString('en-US', { 
      minimumFractionDigits: 0, 
      maximumFractionDigits: 3 
    })} OMR`;
  });
}
```

**SCSS:**
```scss
.variance-live {
  font-size: 11px;
  font-weight: 700;
  padding: 4px 10px;
  border-radius: 6px;
  display: inline-flex;
  gap: 4px;
  align-items: center;
}
.variance-ok      { background: #dcfce7; color: #166534; }
.variance-neutral { background: #f1f5f9; color: #475569; }
.variance-warn    { background: #fef3c7; color: #92400e; }
.variance-danger  { background: #fee2e2; color: #991b1b; }
```

### F6 — Wire variance into quotes table

**File:** `src/app/training/casual-courses/casual-course-price-quotes/casual-course-price-quotes.component.html`

Add a new column to the quotes table:

```html
<!-- ... existing columns ... -->
<th>{{ '::Training.PriceQuote.Variance' | abpLocalization }}</th>
<!-- ... -->

<tbody>
  @for (quote of sortedQuotes(); track quote.id) {
    <tr>
      <!-- ... existing cells ... -->
      <td>
        @if (approvedCost() > 0) {
          <app-variance-chip 
            [quotedPrice]="quote.quotedPriceOMR ?? 0"
            [approvedCost]="approvedCost()">
          </app-variance-chip>
        } @else {
          <span class="muted">—</span>
        }
      </td>
      <!-- ... -->
    </tr>
  }
</tbody>
```

In the `.ts`:

```typescript
// Approved cost is read from the parent — different field name for sessions vs casual
approvedCost = computed<number>(() => {
  if (this.parentArm() === 'casualCourse') {
    return this.course()?.estimatedTotalCost ?? 0;
  } else {
    // For sessions, approved cost comes from the loaded session
    return this.session()?.approvedCostOMR ?? 0;
  }
});
```

This requires the price-quotes component to also fetch the session data when `parentArm === 'session'`. Add a `session = signal<CourseSessionDetailDto | null>(null)` and a `loadSession()` method (mirror of `loadCourse()`).

### F7 — Wire variance into Add Quote dialog

**File:** Same component, dialog section in the template.

Below the "السعر المعروض" input:

```html
<div>
  <label>{{ '::Training.PriceQuote.QuotedPriceOMR' | abpLocalization }} *</label>
  <input 
    type="number"
    [value]="fQuotedPriceOMR()"
    (input)="fQuotedPriceOMR.set($any($event.target).valueAsNumber || 0)">
  
  @if (fQuotedPriceOMR() > 0 && approvedCost() > 0) {
    <div class="variance-inline">
      <app-variance-chip 
        [quotedPrice]="fQuotedPriceOMR()"
        [approvedCost]="approvedCost()">
      </app-variance-chip>
      <span class="approved-cost-hint">
        {{ '::Training.InfoBar.Approved' | abpLocalization }}: 
        {{ approvedCost() | number:'1.0-3' }} {{ '::Training.OMR' | abpLocalization }}
      </span>
    </div>
  }
</div>
```

The variance updates **live** as the user types — because `fQuotedPriceOMR` is a signal and `VarianceChipComponent.result` is a computed that depends on it.

### F8 — Wire variance into Pick Winner dialog

**File:** Same component, pick dialog section.

Add to the dialog body, BEFORE the date fields:

```html
<!-- Course context recap at top of dialog -->
@if (approvedCost() > 0 && pickQuote()) {
  <div class="picked-quote-summary">
    <div class="picked-row">
      <span class="picked-label">{{ '::Training.PriceQuote.SelectedProvider' | abpLocalization }}:</span>
      <span class="picked-value">{{ providerName(pickQuote()?.providerId) }}</span>
    </div>
    <div class="picked-row">
      <span class="picked-label">{{ '::Training.PriceQuote.QuotedPriceOMR' | abpLocalization }}:</span>
      <span class="picked-value-bold">{{ pickQuote()?.quotedPriceOMR | number:'1.0-3' }} OMR</span>
    </div>
    <div class="picked-row">
      <span class="picked-label">{{ '::Training.PriceQuote.Variance' | abpLocalization }}:</span>
      <app-variance-chip 
        [quotedPrice]="pickQuote()?.quotedPriceOMR ?? 0"
        [approvedCost]="approvedCost()">
      </app-variance-chip>
    </div>
  </div>
}
```

### F9 — Localization keys

**Files:** `ar.json` + `en.json`

```json
// ar.json additions
"Training.InfoBar.Course": "الدورة",
"Training.InfoBar.Duration": "المدة",
"Training.InfoBar.Nominees": "المرشحون",
"Training.InfoBar.FundingScenario": "سيناريو التمويل",
"Training.InfoBar.ApprovedCost": "التكلفة المعتمدة",
"Training.InfoBar.UnitTotal": "للوحدة (إجمالي)",
"Training.InfoBar.Approved": "المعتمد",
"Training.Days": "أيام",
"Training.Officers": "ضباط",
"Training.Enlisted": "أفراد",
"Training.OMR": "ر.ع",
"Training.PriceQuote.Variance": "الفرق عن المعتمد",

// en.json mirrors
"Training.InfoBar.Course": "Course",
"Training.InfoBar.Duration": "Duration",
"Training.InfoBar.Nominees": "Nominees",
"Training.InfoBar.FundingScenario": "Funding Scenario",
"Training.InfoBar.ApprovedCost": "Approved Cost",
"Training.InfoBar.UnitTotal": "Unit Total",
"Training.InfoBar.Approved": "Approved",
"Training.Days": "days",
"Training.Officers": "officers",
"Training.Enlisted": "enlisted",
"Training.OMR": "OMR",
"Training.PriceQuote.Variance": "Variance"
```

### F10 — Frontend verification

After applying F1-F9:

```bash
ng build
```

Manual test:
1. Open a Session Detail (CISSP from seeder) → Info Bar visible with all 5+ fields
2. Scroll down to Section 5 → Info Bar stays sticky at top
3. Open Section 3 (Quotes) → add a quote with price = approved cost → variance shows "= 0.0%" gray
4. Add another quote with price 90% of approved → variance shows green "↓ -10%"
5. Edit and bump price to 115% of approved → variance flips to red "↑ +15%"
6. Click "Pick Winner" → dialog shows quote variance prominently
7. Repeat for Casual Course Detail → confirm Funding Scenario shows ("سيناريو 2 — جزئي") instead of Unit Total
8. Toggle Arabic ↔ English → labels translate

---

## Cross-cutting

### Future enhancement (mentioned by OmanAI)

OmanAI has noted that financial visibility should later be permission-controlled — not all roles should see costs. This patch does NOT implement that. The Info Bar shows costs to everyone who can see the page.

**Future patch (separate, not in this scope):**
- New permission `TrainingFinance.ViewCosts`
- Granted to: Staff, TD, TH, Finance, UTM, UGM
- NOT granted to: certain restricted roles (TBD)
- Info Bar conditionally hides cost fields based on permission
- Quotes table hides variance column / shows "—" for restricted users
- Add/Pick dialogs still allow input but hide variance comparison

This is a separate patch (Phase 4C-α Patch 2 or later).

---

## Verification checklist

- [ ] Backend `CourseSessionDetailDto` has all Info Bar fields
- [ ] Backend `CasualCourseDetailDto` has all Info Bar fields including `FundingScenarioLabel`
- [ ] Mappers populate all new fields
- [ ] `CourseInfoBarComponent` created and standalone
- [ ] `CourseInfoBarComponent` accepts `data` and `variant` inputs
- [ ] `variance.helper.ts` exports `computeVariance` with 4-tier logic
- [ ] `VarianceChipComponent` uses signals, updates live
- [ ] Session Detail wires Info Bar with variant='session'
- [ ] Casual Course Detail wires Info Bar with variant='casual'
- [ ] Both pages: Info Bar is sticky below status pipeline
- [ ] Quotes table has new "Variance" column with chip
- [ ] Add Quote dialog shows live variance under price field
- [ ] Pick Winner dialog shows variance in quote summary
- [ ] All localization keys added in ar.json + en.json
- [ ] No regression in existing Session Detail / Casual Course Detail workflows
- [ ] Mobile responsive: Info Bar wraps gracefully on narrow viewports

---

## Commit message suggestion

```
feat(info-bar): persistent course context + variance indicators

Adds a sticky Course Info Bar below the status pipeline on both Session
Detail and Casual Course Detail pages, showing course name, duration,
nominee count, approved cost, and a context-specific field (Funding
Scenario for casual, Unit Total for sessions).

Adds a 4-tier variance indicator (ok/neutral/warn/danger at ±2%, ±10%
thresholds) in:
- Quotes table (new column)
- Add/Edit Quote dialog (live as user types)
- Pick Winner dialog (prominent comparison before confirmation)

A shared CourseInfoBarComponent + VarianceChipComponent enables reuse
across both casual courses and sessions. A computeVariance() helper
function centralizes the threshold logic.

Future work (separate patch): permission-controlled financial visibility 
to restrict cost display per role.

Bumps v4.10.0 → v4.10.1
```

---

*End of Phase 4C-α Patch 1 — v1.0*
*Estimated effort: ~3-4 hours Claude Code work. Backend changes are minimal (DTO field additions + mapper population). Frontend has 2 new shared components + wiring into 2 detail pages + 2 dialog enhancements.*
*Risk: Low — purely additive UI patch. No business logic changes. No state machine modifications.*
