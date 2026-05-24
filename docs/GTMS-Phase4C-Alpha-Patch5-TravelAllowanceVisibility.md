# GTMS Phase 4C-α — Patch 5: Travel Allowance Visibility By Course Type

**Version:** v1.0
**Target:** v4.10.4 → v4.10.5 (patch bump)
**Scope:** Hide the Travel Allowance Payments card inside Section 5 (Payments) for courses where travel is not applicable (Internal + ExternalLocal). Only ExternalInternational courses retain access to TravelAllowancePayment.
**Reason:** Patch 4 hid the Travel Instruction section for Internal and ExternalLocal courses. The TravelAllowance card depends on a travel instruction existing — without one, there are no allowances to pay. Showing an empty card with "Issue travel instruction first" message is misleading and clutters the UI.

---

## What's changing

**Before:**
- Section 5 (Payments) shows two cards for all course types:
  - Travel Allowance Payments
  - Course Payment
- For Internal/Local courses, TravelAllowance card is empty (no allowances exist) but takes UI space

**After:**
- Section 5 layout adapts based on CourseType:
  - **Internal:** Course Payment only (one card, full width)
  - **ExternalLocal:** Course Payment only (one card, full width)
  - **ExternalInternational:** Travel Allowance + Course Payment (two cards, side by side or stacked)

---

## Locked decisions

| # | Decision | Rationale |
|---|---|---|
| Apply to | Both Session Detail (4C-α) AND Casual Course Detail (4B-β) | Same UX both contexts |
| TravelAllowance hidden for | Internal AND ExternalLocal | No travel → no allowances |
| TravelAllowance visible for | ExternalInternational only | Standard international travel allowance |
| Course Payment | Always visible | Required for all course types |
| Backend | No change — entity remains polymorphic | Defensive — server still accepts TravelAllowance if needed |
| Existing data | Local courses with stray TravelAllowance records remain in DB | Test data only — no production data yet |

---

## Backend changes

**No backend changes.** Pure frontend visibility fix.

The TravelAllowancePayment entity continues to work polymorphically. The frontend just hides the UI for course types where it doesn't apply. If a developer or admin manually creates a TravelAllowancePayment for a Local course via direct API call, the record persists but no UI displays it (it would only show up in raw DB queries).

---

## Frontend changes

### F1 — Update Section 5 component for Sessions

**File:** Locate the Section 5 component used by Session Detail.

Likely: `src/app/training/sessions/session-section-payments/session-section-payments.component.ts` (or whatever the actual filename is — verify by searching for the component referenced in `session-detail.component.html` as `<app-session-section-payments>`).

Add a computed signal for travel allowance visibility:

```typescript
import { CourseType } from 'src/app/proxy/training/enums/course-type.enum';

// In the component class:

travelAllowanceVisible = computed<boolean>(() => {
  const ct = this.session()?.courseType;
  // Travel allowances apply only to international external courses
  return ct === CourseType.ExternalInternational;
});
```

Update the template to gate the Travel Allowance card:

```html
<!-- BEFORE -->
<div class="payments-grid">
  <div class="travel-allowance-card">
    <h4>{{ '::Training.Payments.TravelAllowances' | abpLocalization }}</h4>
    <!-- ... travel allowance content ... -->
  </div>
  <div class="course-payment-card">
    <h4>{{ '::Training.Payments.CoursePayment' | abpLocalization }}</h4>
    <!-- ... course payment content ... -->
  </div>
</div>

<!-- AFTER -->
<div class="payments-grid" [class.single-card]="!travelAllowanceVisible()">
  @if (travelAllowanceVisible()) {
    <div class="travel-allowance-card">
      <h4>{{ '::Training.Payments.TravelAllowances' | abpLocalization }}</h4>
      <!-- ... travel allowance content ... -->
    </div>
  }
  <div class="course-payment-card">
    <h4>{{ '::Training.Payments.CoursePayment' | abpLocalization }}</h4>
    <!-- ... course payment content ... -->
  </div>
</div>
```

Add to SCSS:
```scss
.payments-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;  /* two cards side by side */
  gap: 16px;
}

.payments-grid.single-card {
  grid-template-columns: 1fr;  /* one card full width when allowance hidden */
}

@media (max-width: 768px) {
  .payments-grid {
    grid-template-columns: 1fr;  /* always stack on mobile */
  }
}
```

### F2 — Update Section 5 component for Casual Courses

**File:** Locate the equivalent Section 5 component used by Casual Course Detail.

Likely: `src/app/training/casual-courses/casual-course-section-payments/casual-course-section-payments.component.ts` (verify path).

Apply the same change:

```typescript
travelAllowanceVisible = computed<boolean>(() => {
  const ct = this.course()?.courseType;
  return ct === CourseType.ExternalInternational;
});
```

Template update mirrors F1.

### F3 — Hide pipeline node for TravelAllowances if it exists

If the status pipeline has a node specifically for "Travel Allowance Payments" (separate from the Travel Instruction node), gate it the same way:

```typescript
// In the pipeline component
pipelineNodes = computed(() => {
  const ct = this.session()?.courseType;
  const isInternational = ct === CourseType.ExternalInternational;
  
  return [
    // ... earlier nodes ...
    {
      key: 'travel-allowances',
      labelKey: '::Training.Pipeline.TravelAllowances',
      state: this.computeTravelAllowanceState(),
      skipped: !isInternational,
    },
    // ... later nodes ...
  ];
});
```

If the pipeline only has a single "Payments" node (covering both TravelAllowance + CoursePayment), no change needed — the node stays visible because CoursePayment always applies.

### F4 — Verify no broken navigation

Search the codebase for any direct route or link to "travel-allowances" or "travel-allowance-payments". If such routes exist:

```typescript
// E.g., /training/casual-courses/:id/travel-allowances
```

Add a defensive guard:

```typescript
canActivate: async (route, state) => {
  const id = route.paramMap.get('id');
  const entity = await firstValueFrom(courseService.getDetail(id));
  if (entity.courseType !== CourseType.ExternalInternational) {
    router.navigate(['/training/casual-courses', id]);
    return false;
  }
  return true;
}
```

This is defensive — Section 5 hiding already prevents normal access, but blocks URL hacking.

### F5 — Update execution stage computation (backend-side)

The execution stage column from Patch 2 of Phase 4B-β computes stages based on the presence of TravelAllowancePayments. For Internal/Local courses, the stage "AwaitingTravelAllowances" should NEVER apply.

**File:** `src/YourApp.Application/Training/CasualCourses/CasualCourseAppService.cs` and equivalent for sessions.

Find the `ComputeExecutionStage` method and check:

```csharp
private (ExecutionStage stage, string label) ComputeExecutionStage(CasualCourse course)
{
    // ... existing checks ...
    
    // Stage 3 — Travel allowance payments (skip for non-International)
    if (course.CourseType == CourseType.ExternalInternational)
    {
        var totalNominees = course.Nominations.Count;
        var confirmedAllowances = course.TravelAllowancePayments
            .Count(p => p.Status == PaymentStatus.Confirmed);
        if (confirmedAllowances < totalNominees)
            return (ExecutionStage.AwaitingTravelAllowances, ...);
    }
    
    // ... continue to course payment check ...
}
```

If the method already handles this, no change needed. If not, wrap the travel-allowance check in the courseType condition.

**Backend change needed only if the execution stage computation doesn't already account for course type.**

### F6 — Frontend verification

After applying F1-F5:

```bash
ng build
```

Manual test scenarios:

**Scenario 1 — Casual Course, Internal in THApproved:**
1. Open the course → expand Section 5
2. Verify: only Course Payment card visible, full width
3. Verify: NO "Travel Allowance" card shown
4. Verify: list page execution stage column shows correct flow (no "AwaitingTravelAllowances")

**Scenario 2 — Casual Course, ExternalLocal in THApproved:**
1. Open → expand Section 5
2. Verify: only Course Payment card visible, full width
3. Verify: list page execution stage shows expected (no travel allowance step)

**Scenario 3 — Casual Course, ExternalInternational in THApproved:**
1. Open → expand Section 5
2. Verify: BOTH Travel Allowance + Course Payment cards visible, side by side
3. Verify: list page execution stage shows AwaitingTravelAllowances when applicable

**Scenario 4-6 — Session equivalents:**
Same checks for Session Detail pages with Internal, Local, International course types.

**Scenario 7 — Mobile viewport:**
1. Resize browser to < 768px width
2. Verify: payments cards stack vertically (single column) for all course types

**Scenario 8 — Direct URL block (if F4 applied):**
1. Manually navigate to a travel-allowances URL for a Local course
2. Verify redirect, not the form

---

## Cross-cutting

### Existing TravelAllowance records for Local/Internal courses

If test data has TravelAllowancePayment records attached to Local/Internal courses (created before this patch), they persist in the DB but become invisible. This is the same situation as Patch 4 (TravelInstruction records). No data cleanup needed for test environments.

For future production data, consider whether to add a server-side validation that rejects TravelAllowancePayment creation for non-International courses. NOT in this patch — that's a separate decision.

### Financial reports impact

If/when Phase 5 reports are built (Financial Items Budget Report, Casual Cost Report), they should also adapt:
- For Internal/Local courses: don't show travel allowance breakdown
- For International courses: show full breakdown

This is forward-looking — not in scope for this patch.

### The full visibility matrix (post-Patch 4 + Patch 5)

| CourseType | Sec 3 Quotes | Sec 4 Travel | Sec 5 TravelAllow | Sec 5 CoursePayment |
|---|---|---|---|---|
| Internal | hidden | hidden | hidden | visible |
| ExternalLocal | visible | hidden | hidden | visible |
| ExternalIntl | visible | visible | visible | visible |

This is the canonical reference. Update `GTMS-Phase4B-Alpha-Frontend-Notes.md` §3 if it has section state matrix documentation.

---

## Verification checklist

- [ ] Session Section 5 component has `travelAllowanceVisible` computed
- [ ] Session Section 5 template conditionally renders TravelAllowance card
- [ ] Casual Course Section 5 component has same logic
- [ ] Casual Course Section 5 template conditionally renders TravelAllowance card
- [ ] SCSS handles single-card vs two-card layout (CSS grid)
- [ ] Mobile responsive: cards stack on narrow viewports
- [ ] Pipeline node for TravelAllowance (if exists) shows skipped/hidden for non-International
- [ ] Execution stage computation skips AwaitingTravelAllowances for non-International (backend)
- [ ] Defensive route guards in place if travel-allowance standalone routes exist (optional)
- [ ] All 7-8 manual test scenarios pass
- [ ] Frontend Notes documentation matrix updated

---

## Commit message suggestion

```
fix(payments-visibility): hide travel allowances for non-international

Travel Allowance Payments card was showing for Internal and ExternalLocal
courses in both Session Detail and Casual Course Detail. Without travel,
there are no allowances to pay — the card was always empty and misleading.

Travel allowance visibility now requires CourseType = ExternalInternational.
Section 5 layout adapts: single-card (full width) when only Course Payment
is shown, two-card side-by-side for International courses.

Backend ExecutionStage computation also gated to skip AwaitingTravelAllowances
for non-International course types.

Frontend mostly. Minor backend change in execution stage computation.

Bumps v4.10.4 → v4.10.5

Visibility matrix (post-Patches 4+5):
  CourseType            | Quotes | Travel | TravelAllow | CoursePayment
  Internal              | hidden | hidden | hidden      | visible
  ExternalLocal         | visible| hidden | hidden      | visible
  ExternalInternational | visible| visible| visible     | visible
```

---

*End of Phase 4C-α Patch 5 — v1.0*
*Estimated effort: ~45 minutes Claude Code work. Mostly template + computed signal changes + responsive SCSS adjustment. Minor backend tweak for execution stage if not already conditional.*
*Risk: Low — visibility fix, no logic changes. Worst case: card hidden when it shouldn't be — fix the computed.*
