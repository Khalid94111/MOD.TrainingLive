# GTMS Phase 4B-α — Frontend Notes & Patterns

**Version:** v1.0
**Created:** April 2026, after Phase 4B-α frontend ship
**Audience:** Future maintainers, anyone extending the casual course detail page or building similar stage-based pages
**Length:** Read once before touching `casual-course-detail.component.ts` or any of its child sections.

---

## 1. Why this document exists

Phase 4B-α replaced the casual course detail page's tab-based layout with **stage-based progressive disclosure** — single page, status pipeline header, four accordion sections that expand/collapse based on `(course.status × user role)`.

The architecture is non-obvious. During integration, we found 12 bugs that all traced back to two systemic issues:

1. **Multiple component instances coexisting** across status transitions, with subscriptions or stale state outliving their visual presence
2. **State split** between local component state and shared services, with no canonical source of truth

This document captures the patterns we converged on, the bugs we found, and the principles that should guide future work in this area.

If you're adding a 5th section, changing status transitions, or extending mode-aware rendering, **read this first.**

---

## 2. The big architectural decisions

### 2.1 Stage-based layout (not tabs)

The page is a single scrollable surface with:
- Sticky header (course name + status badge + page-level action bar)
- Sticky 8-node status pipeline below the header
- Four accordion sections in document order: Course Details, Financials, Price Quotes, Travel Instructions

Each section is in one of three states at any time:
- **collapsed-summary** — one-line summary visible, click header to expand
- **expanded-active** — full content rendered, this is the user's current focus
- **locked-preview** — greyed out, "available after X" message, no interaction

Section state is computed reactively from `(course.status × current user role × section conditions)`. The matrix is implemented as `computed()` signals on the shell component.

**Why stage-based won over tabs:**
- Tabs imply equal-weight peers; casual course sections aren't peers, they're stages
- Status pipeline visualizes the workflow lifecycle in a way tabs can't
- Single-page scrolling adapts to mobile naturally
- Past stages stay accessible (collapsed) without taking attention from active work

### 2.2 Mode-driven content reuse

Rather than building four new section components, we reused three existing ones:
- `casual-course-request` — the original create/edit form
- `casual-course-approval` — the original approval page
- `casual-course-review` — the original Staff review page

Each accepts a `mode` input:
```typescript
mode = input<'details' | 'financials' | 'all'>('all');
```

Plus computed properties that gate HTML blocks:
```typescript
showDetails = computed(() => this.mode() === 'details' || this.mode() === 'all');
showFinancials = computed(() => this.mode() === 'financials' || this.mode() === 'all');
```

Inside the component templates, blocks are wrapped in `@if (showDetails()) { ... }` etc.

**Why this works:**
- Massive code reuse — no duplicated form logic, validation, or business rules
- Each component still owns the rules for its own status (request knows Draft, approval knows Submitted, review knows UnderReview)
- Legacy standalone routes (with `mode = 'all'`) keep working unchanged

**Why this is dangerous:**
- Three components are alive on screen at once
- Each component has its own subscriptions, signals, and event handlers
- The wrong instance can claim a dispatched action if mode-gating is incorrect
- This caused bugs 10, 11, 12 — see §4

The mode-gating discipline is **non-negotiable**. When you add a new section or change which component handles which status, update both the section state matrix AND the action dispatch gates.

### 2.3 Action dispatch via service

The page-level action bar lives in the shell, but the actions (Submit, Approve, Save Draft, etc.) are handled by the inner components that own that status's logic.

Architecture:
```
casual-course-header-action-bar (in shell)
        │
        │ click → CasualCourseActionService.dispatch('submit')
        ▼
CasualCourseActionService (root-provided RxJS Subject)
        │
        │ subscribers: request, approval, review components
        ▼
Each subscriber:
  - filters by current course status
  - filters by its own mode() === expected mode
  - if both match, runs its handler
```

**Critical: only ONE subscriber should match each (status, action) pair.**

The mode-gating filter is what guarantees this. Get it wrong and either zero handlers run (button does nothing) or multiple handlers run (cascading bugs).

### 2.4 Single source of truth via refresh service

State changes (Submit, Approve, Save Draft, etc.) are made by inner components, but the SHELL owns the canonical course state. After any state-changing action, the inner component must trigger a shell reload:

```typescript
// In any inner component that mutates course state
constructor(private refreshShell: CasualCourseDetailRefreshService) {}

async onApprove() {
  await firstValueFrom(this.service.approve(...));
  this.refreshShell.refresh();  // shell reloads course → all sections recompute states
}
```

Without this call, the shell's `course()` signal stays stale, the action bar's button list doesn't update, and the section state matrix doesn't recompute. This caused bug 12.

---

## 3. The state matrix (canonical reference)

Section state is `(course.status × user role × section conditions)`. Implementation:

```typescript
// In casual-course-detail.component.ts (shell)
detailsState = computed<SectionState>(() => {
  const c = this.course();
  if (!c) return 'locked';
  // ... per-status, per-role logic
});

financialsState = computed<SectionState>(() => { /* ... */ });
quotesState = computed<SectionState>(() => { /* ... */ });
travelState = computed<SectionState>(() => { /* ... */ });
```

### Section 1 — Course Details

| Course Status | User Role | State |
|---|---|---|
| Draft | UTM (creator) | active |
| Draft | Other roles | locked |
| Submitted | UGM | active |
| Submitted | Others | collapsed |
| UGMApproved+ | All | collapsed |
| Default fallback | Anyone | active (most recent stage) |

### Section 2 — Financials

| Course Status | User Role | State |
|---|---|---|
| Draft, Submitted | All | locked |
| UGMApproved | Staff (canReview) | **active** ⚠ (bug 11 — needs to mount review component) |
| UGMApproved | Others | collapsed |
| UnderReview | Staff | active |
| UnderReview | Others | collapsed |
| StaffReviewed | TD | active |
| TDApproved | TH | active |
| THApproved+ | All | collapsed |

### Section 3 — Price Quotes

| Course Status | User Role | State |
|---|---|---|
| Pre-THApproved | All | locked |
| THApproved (no winner) | Staff | active |
| THApproved (no winner) | Others | collapsed |
| THApproved (winner picked) | All | collapsed |
| Internal course type | All | **hidden** |

### Section 4 — Travel Instructions

| Course Status | User Role | State |
|---|---|---|
| Pre-quote selection | All | locked |
| THApproved + winner, no instruction | Staff | active |
| THApproved + winner, instruction issued | All | collapsed |
| Internal course type | All | **hidden** |

**Hidden vs locked:** "Hidden" means section doesn't render at all, no slot, no message. "Locked" means section renders with a greyed-out message saying when it'll unlock.

---

## 4. The 12 bugs we hit (and what they teach)

These are listed in the order they were found during integration, with the lesson each bug carries.

### Bug 1 — Memory of "tabs" architecture

**Symptom:** OmanAI's project memory described the page as tab-based; new contributors reading it would build the wrong thing.

**Root cause:** Memory not updated when architecture changed mid-phase.

**Fix:** Memory rewritten to describe stage-based as canonical; tabs marked superseded.

**Lesson:** When you make an architecture pivot mid-phase, update the project memory in the same commit. Outdated documentation is worse than missing documentation.

### Bug 2 — Funding scenario leaked into approval summary

**Symptom:** Approval page showed scenario picker + total cost in the course summary panel.

**Root cause:** Mode-aware rendering wasn't applied to all relevant sections of the approval template.

**Fix:** `@if (showFinancials())` wrapping added around the leaked fields.

**Lesson:** When you add mode gating to a component, audit every block of HTML. It's easy to miss one.

### Bug 3 — Modal frozen behind backdrop

**Symptom:** Price quote modal opened, but clicks went to the backdrop instead of the modal.

**Root cause:** Shared `gtms-design.scss` declared `.modal-backdrop { z-index: 1000 }`, and Angular's `styleUrls` processed component-local styles before the shared one — so the shared rule won the cascade and the backdrop sat at z-1000 while the modal sat at z-101.

**Fix:** Local rules raised via `:host` selector specificity + z-index 1100/1101 + explicit `display: block` to override shared `display: flex`.

**Lesson:** Shared CSS files in monorepos always win unless you use `:host` or `::ng-deep` to defeat them. When debugging "modal not centered" or "modal not visible," check the shared stylesheet first.

### Bug 4 — Course summary duplicated in price quotes

**Symptom:** Price quotes embedded view showed both the shell's course summary AND the legacy standalone route's summary.

**Root cause:** Embedded view didn't gate the legacy summary block.

**Fix:** Added `@if (!embedded())` around the duplicate block.

**Lesson:** Whenever a component is embeddable, audit its template for "things that look like they're at the page level" — those need to be gated by the embedded flag.

### Bug 5 — Modal centering off

**Symptom:** Modal appeared off-center.

**Root cause:** Used `inset: 50% auto auto 50% + transform: translate(50%, -50%)` which placed left edge at viewport-center + half-width, putting the modal way off to the right.

**Fix:** Replaced with the standard pattern: `top: 50%; left: 50%; transform: translate(-50%, -50%)`.

**Lesson:** Use the CSS pattern everyone knows. Inventing your own positioning math is 3 minutes saved and 3 hours debugged.

### Bug 6 — Section animation captured fixed-position elements

**Symptom:** Modal opening during the 200ms section expand animation collapsed onto the section instead of the viewport.

**Root cause:** Keyframes used `transform: translateY(...)` which creates a containing block during animation. `position: fixed` elements inside become positioned relative to the animating ancestor, not the viewport.

**Fix:** Replaced translate with opacity-only fade.

**Lesson:** Any CSS transform creates a containing block. If you have modals or fixed-position elements that might open during an animation, use opacity-only or filter-based animations.

### Bug 7 — Autosave race conditions

**Symptom:** Adding nominees rapidly caused some to be silently dropped.

**Root cause:** Original autosave fired `service.create` and on long awaits dropped subsequent nominee picks. Then user requested autosave be removed entirely.

**Fix:** Removed `ensureDraftSaved`, `dtoEquals`, `persistUpdate`, `sameIds`, the `autosaving` signal, and the autosave chip. Save Draft / Submit are now the only persistence triggers.

**Lesson:** Autosave introduces complex race conditions. If you must implement it, debounce aggressively and ignore stale callbacks. If you can avoid it, do.

### Bug 8 — Dropdown values silently not pre-selecting

**Symptom:** `<select [value]>` with dynamic option lists silently kept default value instead of pre-selecting.

**Root cause:** Angular set the select value before the browser processed the option list, so the browser kept the default.

**Fix:** Removed `[value]` from the select, added `[selected]="fieldX() === optionValue"` to each `<option>`. Robust pre-selection at element-creation time.

**Lesson:** For dynamic options, prefer `[selected]` on options over `[value]` on selects. The latter has a known timing issue.

### Bug 9 — Stale-id leak via BehaviorSubject

**Symptom:** `/new` route picked up an ID from a previous course session, and Save Draft hit `service.update(staleId, ...)` → `EntityNotFoundException`.

**Root cause:** `attachEvents BehaviorSubject` retained the previous course's ID. Reading `currentAttachedId()` on `/new` got the stale ID.

**Fix:** Shell calls `refresh.clearAttachment()` whenever it boots without an ID (i.e., on `/new`).

**Lesson:** **`BehaviorSubject` is dangerous for cross-route state.** Either use `Subject` (no replay, no leak) or explicitly clear the subject when entering a fresh state. This bug only surfaces when users navigate between courses or to `/new` — easy to miss in single-course testing.

### Bug 10 — Subscription leak misrouted Submit action

**Symptom:** Click Submit, navigate away, return, Submit again — old destroyed component's handler fires alongside the live one. Old handler runs `service.update(staleId, ...)` → `CannotEditInThisStatus`.

**Root cause:** Action service subscriptions had no cleanup. Components destroyed during navigation kept their subscriptions alive (closures held references), so the next dispatch fired all of them — dead and live.

**Fix:** Every subscription to `CasualCourseActionService` now pipes through `takeUntilDestroyed(this.destroyRef)`:
```typescript
this.actionService.actions$.pipe(
  takeUntilDestroyed(this.destroyRef),
  filter(...)
).subscribe(...);
```

**Lesson:** **Any RxJS subscription wrapping a long-lived Subject MUST use `takeUntilDestroyed`.** This isn't a "nice to have" — it's the difference between working code and silent state corruption. Make this a hard rule in code review.

### Bug 11 — Start Review button no-op at UGMApproved

**Symptom:** Staff sees casual course at UGMApproved, clicks "Start Review" in the action bar — nothing happens.

**Root cause:** Section 2 (Financials) defaulted to `collapsed` for UGMApproved. The review component (which handles Start Review) is mounted only in `expanded-active` mode. With no mounted instance, the dispatched action had no subscriber.

**Fix:** Promoted UGMApproved + canReview to `active` state in `financialsDefaultState`. The review component now mounts, receives the action, runs the handler.

**Lesson:** **Section state matrix and action dispatch are coupled.** When you add a button that fires an action, verify the responsible component is mounted in the section state for that course status. Easy way: review the state matrix at the same time you add the button. Bad outcomes if you don't: silent click-no-response bugs.

### Bug 12 — Header buttons didn't update after Start Review

**Symptom:** Click Start Review, status transitions to UnderReview, but the action bar still shows the UGMApproved button set ("Start Review") instead of the UnderReview set ("Save Progress" / "Finalize Review").

**Root cause:** `onStartReview` reloaded only the review component's local state. The shell's `course().status` stayed at UGMApproved, so the action bar's `computed()` didn't recompute. The data was wrong; the UI matched the wrong data.

**Fix:** Inject `CasualCourseDetailRefreshService` in the review component, call `refreshShell.refresh()` after `onStartReview` (and after `onSaveAssignments` with `commit=false`).

**Lesson:** **Whenever an inner component mutates the course's persistent state, it MUST trigger a shell refresh.** No exceptions. The shell's `course()` signal is the single source of truth — local component state alone isn't enough. Adopt this as a code review checklist item.

---

## 5. Patterns and rules to follow

### 5.1 Subscriptions

Every subscription that wraps a long-lived `Subject` (any service-level event stream):

```typescript
// REQUIRED PATTERN
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

constructor(private destroyRef: DestroyRef) {}

ngOnInit() {
  this.someService.events$.pipe(
    takeUntilDestroyed(this.destroyRef),
    filter(event => this.shouldHandle(event))
  ).subscribe(event => this.handle(event));
}
```

No exceptions. If you create a subscription without `takeUntilDestroyed`, expect bug 10 to come back.

### 5.2 State mutation + shell refresh

Whenever an inner component mutates persistent course state:

```typescript
// REQUIRED PATTERN
constructor(private refreshShell: CasualCourseDetailRefreshService) {}

async onStateChangeAction() {
  await firstValueFrom(this.service.someMutation(...));
  this.refreshShell.refresh();  // shell reloads, all sections recompute
}
```

If you forget this, bug 12 returns.

### 5.3 Mode-aware HTML

When using mode-driven content split, audit every block:

```html
<!-- ALWAYS gate sections by mode -->
@if (showDetails()) {
  <div class="course-info">...</div>
  <div class="nominees-list">...</div>
}

@if (showFinancials()) {
  <div class="scenario-picker">...</div>
  <div class="financial-items">...</div>
}

<!-- Action bars only in legacy standalone view -->
@if (mode() === 'all') {
  <div class="action-bar">...</div>
}
```

Bug 2 was a missed gate. The fix is mechanical, but the prevention is a careful audit.

### 5.4 Embedded gating

When a component can be embedded inside another:

```typescript
embedded = computed(() => this.route.snapshot.data['embedded'] === true);
```

```html
<!-- Hide page-level chrome when embedded -->
@if (!embedded()) {
  <h1 class="page-title">{{ courseName() }}</h1>
  <div class="course-summary-panel">...</div>
}
```

Bug 4 was a missed embedded gate.

### 5.5 BehaviorSubject hygiene

If you use `BehaviorSubject` for cross-route state, **explicitly clear it** when entering a fresh state:

```typescript
// In shell ngOnInit
ngOnInit() {
  if (!this.route.snapshot.paramMap.get('id')) {
    this.refresh.clearAttachment();  // clear stale state
  }
  // ... rest of init
}
```

If you don't need replay semantics, use `Subject` instead — it doesn't retain state and avoids the issue entirely.

### 5.6 CSS specificity for shared styles

When fighting a shared stylesheet:

```scss
// Don't fight :host with element selectors — fight :host with :host
:host .modal {
  z-index: 1100;  // beats shared .modal-backdrop { z-index: 1000 }
  display: block; // overrides shared display: flex
}
```

Bug 3 cost an hour. The fix is one selector. Recognize the shape.

### 5.7 Section state ↔ action handler coupling

When adding a new action button:

1. Identify which component handles it (request / approval / review)
2. Identify which `course.status` values trigger it
3. **Verify** that for each of those statuses, the section containing the handler is in `active` state
4. If not, update the section state matrix to include the new condition

Bug 11 happened because step 4 was missed. Adopt this as a checklist.

---

## 6. Architecture summary diagram

```
┌─────────────────────────────────────────────────────────────────┐
│ CasualCourseDetailComponent (shell)                              │
│  • Owns course() signal — source of truth                       │
│  • Computes section states from (course × user role)            │
│  • Hosts header action bar                                      │
│  • Hosts status pipeline                                        │
│  • Listens to refresh service for child-driven reloads          │
└────────────────────────────────────────────────┬────────────────┘
                                                 │
                ┌────────────────────────────────┤
                │                                │
                ▼                                ▼
   ┌─────────────────────────┐    ┌─────────────────────────┐
   │ Section components      │    │ CasualCourseActionService │
   │ (Details/Financials/   │◄───│ (root-provided)          │
   │  Quotes/Travel)        │    │ Dispatches actions       │
   └────────────┬────────────┘    └─────────────────────────┘
                │
                │ Each delegates to one of three reused
                │ legacy components based on course status:
                ▼
   ┌─────────────────────────────────────────────────────────┐
   │ casual-course-request   (Draft / Returned / new)        │
   │ casual-course-approval  (Submitted / StaffRev / TDApp) │
   │ casual-course-review    (UGMApp / UnderReview / etc)   │
   │                                                         │
   │ Each component:                                         │
   │  - Has mode input ('details' | 'financials' | 'all')   │
   │  - Gates HTML blocks by showDetails() / showFinancials │
   │  - Subscribes to action service with status × mode     │
   │    filter + takeUntilDestroyed cleanup                  │
   │  - On state mutation: refreshShell.refresh()           │
   └─────────────────────────────────────────────────────────┘
```

---

## 7. Files to touch when extending

If you're adding a new section (Phase 5 Execution, for example):

1. **Section state matrix** — add a new `executionState` computed signal in shell
2. **Status pipeline** — replace the placeholder Phase 5 node with a real one
3. **Section component** — new `casual-course-section-execution` following the existing pattern
4. **Mode constants** — extend the mode union if the section has its own mode
5. **Action handlers** — if the section has actions, decide which existing component handles them, OR build a new one
6. **State matrix tests** — verify all new status × role combinations
7. **Documentation** — update this file with new bugs and patterns

If you're adding a new status (e.g., `Cancelled` post-approval):

1. **State matrix** — add row for the new status to all four section state computeds
2. **Action bar** — add buttons for the new status to `casual-course-header-action-bar`
3. **Component subscriptions** — verify which component handles actions in this status
4. **Pipeline node** — the pipeline may need a new node, or the new status maps to an existing one
5. **Smoke test** — verify the full lifecycle works with the new status inserted

---

## 8. Smoke test checklist

When making any change to the shell, status transitions, or section state matrix, run this 10-step manual smoke test:

1. **UTM creates new course** at `/new`, fills form, adds 2 nominees, clicks "Save Draft" — URL changes, form persists
2. **UTM submits** — pipeline node "Submit" turns green, status moves to Submitted
3. **UGM opens course** in different tab — Section 1 active, calculator visible, click "Approve"
4. **Staff opens** at UGMApproved, clicks "Start Review" — Section 2 expands, scenario picker visible (bug 11 test)
5. **Staff picks scenario** 2, table populates, edit a rate inline, click "Save Progress" — header buttons stay (UnderReview)
6. **Staff clicks "Finalize Review"** — header buttons swap to TD's view (bug 12 test)
7. **TD approves**, then **TH approves**
8. **Staff sees** Section 3 (Quotes) unlock, adds 3 quotes, picks winner with actual dates
9. **Staff fills** Section 4 (Travel) instruction, clicks Issue
10. **Reload page** — verify all sections render correct collapsed-summary content

Any failure = open this document, find the relevant bug class, fix at the pattern level (not the symptom).

---

## 9. Things this document does NOT cover

- Phase 4B-β (payments + reallocation) — separate doc when that ships
- Phase 5 (post-execution: certificates, evaluations, reports) — future
- The session arm of polymorphic entities (PriceQuote, TravelInstruction) — partially built, not exercised in 4B-α
- Mobile responsive layout — sections work at narrow viewports but no dedicated mobile UX pass

---

*End of Phase 4B-α Frontend Notes — v1.0*
*If this document is more than 2 patches old without an update, treat it as suspect.*
