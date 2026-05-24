# GTMS Phase 4C-α — Patch 4: Section Visibility By Course Type

**Version:** v1.0
**Target:** v4.10.3 → v4.10.4 (patch bump)
**Scope:** Fix Section 3 (Price Quotes) and Section 4 (Travel Instructions) visibility logic in both Session Detail and Casual Course Detail pages. Currently Section 4 shows for ExternalLocal courses, which it shouldn't — local providers don't require travel arrangements.
**Reason:** Both Session Detail and Casual Course Detail pages currently show Section 4 (Travel Instructions) for ExternalLocal courses. They should hide it. Section 4 is only for ExternalInternational. Section 3 (Quotes) should hide only for Internal courses.

---

## What's wrong now

The `travelHidden()` computed signal in both detail components only checks for `CourseType.Internal`. It doesn't recognize that `ExternalLocal` also shouldn't have travel instructions because the provider is in-country.

The `quotesHidden()` computed signal may have a similar issue (untested), or may already be correct.

---

## The correct visibility rules

| CourseType | Section 3 (Quotes) | Section 4 (Travel) | Section 5 (Payments) |
|---|---|---|---|
| Internal | **hidden** (we run the course, no external provider) | **hidden** (no travel) | visible |
| ExternalLocal | visible (gather quotes from local providers) | **hidden** (in-country, no travel) | visible |
| ExternalInternational | visible | visible | visible |

**Why these rules:**
- **Internal:** We own the venue + trainer. No bidding, no travel.
- **ExternalLocal:** Provider is somewhere in Oman. We collect quotes from local institutes, but nobody travels internationally.
- **ExternalInternational:** Full external flow — quotes from overseas providers, flights, visa, insurance, allowance.

---

## Locked decisions

| # | Decision | Rationale |
|---|---|---|
| Apply to | Both Session Detail (4C-α) AND Casual Course Detail (4B-α) | Same bug class likely exists in both |
| Section 4 hidden for | Internal OR ExternalLocal | Provider in-country, no travel logistics |
| Section 3 hidden for | Internal only | Local courses still need quotes from local providers |
| Section 5 visible for | All course types | Payments always needed |
| Bonus | Add a downstream check in nav routes for Travel Instruction routes | Block direct URL access for Local courses |

---

## Backend changes

**No backend changes.** Pure frontend visibility fix.

The TravelInstruction entity remains polymorphic and capable of being created for any course type. The frontend just shouldn't expose the UI to do so for Local courses. If a malicious request creates a TravelInstruction for a Local course via direct API call, that's a separate concern (not in scope for this patch — could be future server-side validation).

---

## Frontend changes

### F1 — Fix `session-detail.component.ts`

**File:** `src/app/training/sessions/session-detail/session-detail.component.ts`

Find the `quotesHidden` and `travelHidden` computed signals. Update:

```typescript
import { CourseType } from 'src/app/proxy/training/enums/course-type.enum';

// BEFORE (likely):
quotesHidden = computed(() => 
  this.session()?.courseType === CourseType.Internal
);

travelHidden = computed(() => 
  this.session()?.courseType === CourseType.Internal
);

// AFTER:
quotesHidden = computed(() => {
  const ct = this.session()?.courseType;
  // Quotes hidden only for Internal courses
  return ct === CourseType.Internal;
});

travelHidden = computed(() => {
  const ct = this.session()?.courseType;
  // Travel hidden for both Internal AND ExternalLocal
  // (Internal = no provider, Local = provider in-country, no travel)
  return ct === CourseType.Internal || ct === CourseType.ExternalLocal;
});
```

If the existing code looks structurally different, just preserve the surrounding logic and adjust the `return` expression.

### F2 — Fix `casual-course-detail.component.ts`

**File:** `src/app/training/casual-courses/casual-course-detail/casual-course-detail.component.ts` (or equivalent shell component)

Apply the same correction:

```typescript
quotesHidden = computed(() => {
  const ct = this.course()?.courseType;
  return ct === CourseType.Internal;
});

travelHidden = computed(() => {
  const ct = this.course()?.courseType;
  return ct === CourseType.Internal || ct === CourseType.ExternalLocal;
});
```

If the casual course shell uses different signal names (e.g., `showTravelSection`, `isTravelVisible`), find and update them — the logic is what matters, not the naming.

### F3 — Hide pipeline node for Travel when applicable

The session and casual course pages have a status pipeline at the top showing 8+ nodes. One node is "Travel" / "تعليمات السفر".

For Internal and ExternalLocal courses, this node should also be hidden (or marked as "skipped" with a dash) — to maintain consistency with the section visibility.

**Check the pipeline component:** `session-status-pipeline.component.ts` or `casual-course-status-pipeline.component.ts`.

If the pipeline already handles this (via the `skipped` flag on each node based on `courseType`), great — no change needed. If not, add a check:

```typescript
// In the pipeline component
pipelineNodes = computed(() => {
  const ct = this.session()?.courseType;
  const isInternal = ct === CourseType.Internal;
  const isLocal = ct === CourseType.ExternalLocal;
  
  return [
    // ... earlier nodes ...
    {
      key: 'quote-selection',
      labelKey: '::Training.Pipeline.QuoteSelection',
      state: this.computeQuoteState(),
      skipped: isInternal,  // hide for Internal only
    },
    {
      key: 'dates-confirmed',
      labelKey: '::Training.Pipeline.DatesConfirmed',
      state: this.computeDatesState(),
      skipped: isInternal,
    },
    {
      key: 'travel-instruction',
      labelKey: '::Training.Pipeline.TravelInstruction',
      state: this.computeTravelState(),
      skipped: isInternal || isLocal,  // hide for Internal AND Local
    },
    // ... later nodes ...
  ];
});
```

The pipeline template should render `skipped` nodes with a dash icon and grayed-out style — this should already exist from Phase 4C-α.

### F4 — Defensive: block direct URL access

If there are standalone routes for adding a travel instruction (`/training/casual-courses/:id/travel-instruction` or similar), add a route guard that redirects if the course type is Internal or Local.

This is **defensive** — the section UI hiding already prevents normal access, but it's worth blocking URL hacking too. If route guards aren't already in place for this, it's a quick add:

```typescript
// Pseudo-code for route guard
canActivate: async (route, state) => {
  const id = route.paramMap.get('id');
  const parentArm = route.data['parentArm'];  // 'casualCourse' or 'session'
  
  const entity = parentArm === 'casualCourse'
    ? await firstValueFrom(casualCourseService.getDetail(id))
    : await firstValueFrom(sessionService.getDetail(id));
  
  if (entity.courseType === CourseType.Internal || entity.courseType === CourseType.ExternalLocal) {
    router.navigate(['/training', parentArm === 'casualCourse' ? 'casual-courses' : 'sessions', id]);
    return false;
  }
  
  return true;
}
```

**Skip F4 if there are no standalone travel instruction routes** — the section embedding is the only entry point.

### F5 — Update the section state matrix (if visible to users via documentation)

If the section state matrix is in `Phase4B-Alpha-Frontend-Notes.md`, update it to reflect the corrected rules:

```markdown
### Section 4 — Travel Instructions

| Course Status | CourseType | User Role | State |
|---|---|---|---|
| Any | Internal | All | **hidden** |
| Any | ExternalLocal | All | **hidden** |
| Pre-quote selection | ExternalIntl | All | locked |
| THApproved + winner, no instruction | ExternalIntl | Staff | active |
| THApproved + winner, instruction issued | ExternalIntl | All | collapsed |
```

This is documentation hygiene — not blocking, but good practice.

### F6 — Frontend verification

After applying F1-F5:

```bash
ng build
```

Manual test scenarios:

**Scenario 1 — Casual Course, Internal:**
1. Open a casual course with CourseType=Internal in THApproved state
2. Verify Section 3 (Quotes) is NOT rendered (no card, no header)
3. Verify Section 4 (Travel) is NOT rendered
4. Verify Section 5 (Payments) IS rendered

**Scenario 2 — Casual Course, ExternalLocal:**
1. Open a casual course with CourseType=ExternalLocal in THApproved state
2. Verify Section 3 (Quotes) IS rendered
3. Verify Section 4 (Travel) is NOT rendered (the fix)
4. Verify Section 5 (Payments) IS rendered

**Scenario 3 — Casual Course, ExternalInternational:**
1. Open a casual course with CourseType=ExternalInternational in THApproved state
2. Verify Section 3, Section 4, Section 5 ALL rendered

**Scenario 4 — Session, Internal:**
1. Open a session in any status
2. Verify same as Scenario 1

**Scenario 5 — Session, ExternalLocal:**
1. Open a session in any status
2. Verify same as Scenario 2

**Scenario 6 — Session, ExternalInternational:**
1. Open a session in any status
2. Verify same as Scenario 3

**Scenario 7 — Pipeline node visibility:**
1. Open a Local casual course
2. Verify the "Travel Instruction" node in the pipeline is either hidden OR shown as skipped (dash icon, grayed)
3. Same check for Internal

**Scenario 8 — Direct URL block (if applicable):**
1. With a Local casual course's ID, manually navigate to `/training/casual-courses/{id}/travel-instruction`
2. Verify redirect or 403, NOT the travel instruction form

---

## Cross-cutting

### Existing data

If there are existing TravelInstruction records for Local courses in the test data (from before this patch), they remain in the DB but become invisible in the UI. They're not deleted — they're just orphaned. This is fine for a test environment. For production, consider whether a data cleanup script is needed (probably not — Local courses shouldn't have had instructions created in the first place).

### Downstream Phase 4B-β

TravelAllowancePayments are scoped via Nominations on a course/session. If the new visibility rule means TravelAllowancePayment shouldn't apply to Local courses either:

**Check this with OmanAI before assuming:** Do Local courses still need TravelAllowancePayment? They might — Staff might pay nominees for local transport, meals, or per-diem even within Oman.

For now, this patch doesn't change TravelAllowancePayment visibility. That's a separate question:
- If yes (Local does need allowances): keep current behavior
- If no: a follow-up patch hides Section 5 partially for Local (only CoursePayment, not TravelAllowance)

### Internal courses on payment-only path

Internal courses go directly from Scheduled → InProgress → Completed → FinanciallyClosed, with only Section 5 (CoursePayment) needed. The status pipeline should reflect this with skipped nodes for Quote/Travel.

Verify the pipeline already handles this — if not, the F3 changes cover it.

---

## Verification checklist

- [ ] `travelHidden` computed in Session Detail updated to include ExternalLocal
- [ ] `travelHidden` computed in Casual Course Detail updated to include ExternalLocal
- [ ] `quotesHidden` confirmed correct (Internal only) in both
- [ ] Pipeline node for Travel Instruction shows as skipped/hidden for Internal AND Local
- [ ] No backend changes
- [ ] All 8 manual test scenarios pass
- [ ] Frontend Notes section matrix updated if applicable
- [ ] Defensive route guard added if standalone routes exist (F4 optional)

---

## Commit message suggestion

```
fix(section-visibility): hide Travel section for Local courses

Section 4 (Travel Instructions) was incorrectly showing for ExternalLocal 
courses in both Session Detail and Casual Course Detail pages. The 
visibility logic only checked for CourseType.Internal.

Correct rules:
- Internal: hide Section 3 (Quotes) AND Section 4 (Travel)
- ExternalLocal: hide Section 4 only (no travel, provider in-country)
- ExternalInternational: show all sections

Also updates the status pipeline to mark Travel Instruction node as 
skipped for Internal and Local courses.

Frontend-only fix. No backend, schema, or API changes. Existing 
TravelInstruction records for Local courses remain in DB but become 
invisible — not deleted.

Bumps v4.10.3 → v4.10.4
```

---

*End of Phase 4C-α Patch 4 — v1.0*
*Estimated effort: ~30 minutes Claude Code work. Two computed signals + pipeline node check + manual tests.*
*Risk: Very low — pure visibility fix, no logic changes. Worst case: hidden sections become visible again if logic regresses.*
