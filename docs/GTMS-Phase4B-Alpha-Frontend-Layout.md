# GTMS Phase 4B-α — Frontend Layout: Stage-Based Progressive Disclosure

## CRITICAL: Replace tab-based layout with stage-based single-page progressive disclosure

The casual course detail page does **NOT** use tabs. Instead, it uses a single-page layout with a workflow status pipeline at the top and accordion sections below. This is a significant departure from the typical detail page pattern — implement it carefully.

---

## Page structure

```
┌──────────────────────────────────────────────────────────┐
│ HEADER (always visible, sticky)                          │
│  • Course name                                           │
│  • Status badge                                          │
│  • Action bar (role + status driven)                     │
│                                                          │
│ STATUS PIPELINE (always visible)                         │
│  ✓ Draft → ✓ Submit → ✓ UGM → ✓ Staff → ✓ TH            │
│  → ○ Quote Selection → ○ Travel Prep                     │
└──────────────────────────────────────────────────────────┘

▼ SECTION 1: Course Details                                
▼ SECTION 2: Financials                                    
▼ SECTION 3: Price Quotes                                  
▼ SECTION 4: Travel Instructions                           
```

Each section is an expandable accordion with three possible states: **collapsed-summary**, **expanded-active**, or **locked-preview**.

---

## Section state matrix

Each section's state is determined by `(course.status × current user role × section conditions)`. Implement as a reactive computed signal.

### Section 1 — Course Details

| Course Status | User Role | State |
|---|---|---|
| Draft | UTM (creator) | Expanded-active (form editable) |
| Draft | Other roles | Locked or hidden (drafts shouldn't be visible to others) |
| Submitted | UGM | Expanded-active (their decision context) |
| Submitted | Others | Collapsed-summary |
| UGMApproved+ | All | Collapsed-summary |
| Any | If user just navigated to this page | Expanded-active by default if no other section claims active |

### Section 2 — Financials

| Course Status | User Role | State |
|---|---|---|
| Draft, Submitted | All | Locked-preview ("Available after course is saved with nominees") |
| UGMApproved | Any | Collapsed-summary (calculator visible inline) |
| UnderReview | Staff | Expanded-active (scenario picker + financial items table) |
| UnderReview | Others | Collapsed-summary (read what Staff is doing) |
| StaffReviewed | TD | Expanded-active (TD's cost gate) |
| TDApproved | TH | Expanded-active (TH's cost gate) |
| THApproved | All | Collapsed-summary |

### Section 3 — Price Quotes

| Course Status | User Role | State |
|---|---|---|
| Pre-THApproved | All | Locked-preview ("Available after TH approval") |
| THApproved without selected quote | Staff | Expanded-active (quote workflow) |
| THApproved without selected quote | Others | Collapsed-summary |
| THApproved with selected quote | All | Collapsed-summary (winner shown) |
| Internal course type | All | Hidden entirely |

### Section 4 — Travel Instructions

| Course Status | User Role | State |
|---|---|---|
| Pre-THApproved | All | Locked-preview ("Available after winning quote is selected") |
| THApproved without selected quote | All | Locked-preview (same message) |
| THApproved with selected quote, no instruction | Staff | Expanded-active (issue instructions) |
| THApproved with issued instruction | All | Collapsed-summary |
| Internal course type | All | Hidden entirely |

---

## Section visual design

### Collapsed-summary state
```
▼ Section Title                              [Status chip]
  One-line summary of key facts               [Expand ↓]
```

Click header to expand. Hover shows hand cursor. Border `0.5px var(--color-border-tertiary)`, radius `var(--border-radius-lg)`, padding 14px 18px. Height ~50-70px.

Examples:
- Course Details: "PMP · External · 5 days · Apr 15-19 · 2 nominees"
- Financials: "Scenario 2 · 5,580 OMR · 4 items"
- Price Quotes: "3 quotes · SANS Institute selected · 5,400 OMR"
- Travel Instructions: "Issued · 7 travel days · Tickets booked"

### Expanded-active state
```
▼ Section Title                              [Status chip]
  ┌────────────────────────────────────────┐
  │ Full content rendered here              │
  │ Forms, tables, all interactive elements │
  └────────────────────────────────────────┘
                                    [Collapse ↑]
```

The section's full content. Padding 24px on all sides. Section header gets a subtle accent (left border `2px solid var(--color-border-info)` or similar) to indicate this is the active focus.

### Locked-preview state
```
▽ Section Title                              [🔒 Locked]
  Greyed-out section preview
  Reason: "Available after [trigger event]"
```

Visually de-emphasized — opacity 0.5, no expand affordance, no click action. Shows what the section will eventually contain (a hint of layout) but greyed out. Cursor `not-allowed` on hover.

---

## The status pipeline (top of page)

```
[Draft] ✓  →  [Submit] ✓  →  [UGM] ✓  →  [Staff] ✓  →  [TH] ✓  →  [Quote] ●  →  [Travel] ○  →  [Execution] ○
```

- `✓` = completed (green)
- `●` = in progress / active (blue)
- `○` = pending (gray)

Implementation:
- Horizontal stepper component
- 8 nodes (Draft, Submit, UGM, Staff Review, TH Approval, Quote Selection, Travel Prep, Execution)
- Connector lines between nodes (solid for completed, dashed for upcoming)
- Click on a completed node = scrolls to and expands that section
- Always visible — sticky to top of viewport on scroll

The "Execution" node is grayed out with label "(Phase 5)" since it's not implemented yet. Keep the visual placeholder so users see the full lifecycle.

---

## Default section expansion on page load

When a user lands on the page, decide which section to auto-expand using this priority:

1. If URL has hash anchor (e.g. `#financials`, `#quotes`, `#travel`) → expand that section
2. Otherwise, find the section that matches `expanded-active` state for current user role + course status
3. If multiple sections qualify, prefer the most recent stage of the pipeline
4. If none qualify (e.g., readonly viewer), default to Section 1 (Course Details)

Other sections remain in their default state (collapsed-summary or locked-preview).

URL anchors: when user clicks a node in the status pipeline, update the URL hash without page reload. Browser back/forward then works correctly.

---

## Cross-section interactions

The Execution Status content from the original mockup (the 4 cards showing winning quote, actual dates, travel instruction status, next steps) is **NOT a separate section**. It's distributed:

- "Winning quote" card → appears as the Section 3 collapsed-summary content
- "Actual dates" card → appears in Section 1 collapsed-summary as supplementary info ("Dates: Apr 15-19 confirmed")
- "Travel instruction" card → Section 4 collapsed-summary
- "Next steps" card → eliminated (the status pipeline already shows what's next)

Shortcut links between sections: when in Section 1 (Course Details) collapsed-summary, the supplementary "Selected quote: SANS Institute, 5,400 OMR" can be a clickable link that **scrolls to and expands** Section 3.

---

## Page-level action bar

Lives in the header, not in any individual section. Buttons appear based on `(course status × user role)`:

| Status | Role | Buttons |
|---|---|---|
| Draft | UTM | [Save Draft] [Submit ↗] |
| Submitted | UGM | [Approve] [Return] [Reject] |
| UGMApproved | Staff | [Start Review] |
| UnderReview | Staff | [Save Progress] [Finalize Review ↗] |
| StaffReviewed | TD | [TD Approve] [Return] [Reject] |
| TDApproved | TH | [TH Approve] [Return] [Reject] |
| THApproved+ | All | [Print] [Export PDF] (no workflow actions) |
| ReturnedToCreator | UTM | [Resubmit ↗] |
| Rejected | All | (no action buttons) |

The action bar is sticky to the top, so workflow actions are always one click away regardless of scroll position.

---

## Layout for Create mode (new casual course)

When UTM hits `/training/casual-courses/new`:

- Show the same page layout but with no course data
- Status pipeline shows all nodes as `○` (pending)
- Section 1 (Course Details) is `expanded-active` with the empty form
- Sections 2-4 are `locked-preview` ("Available after course is saved with nominees")
- Action bar shows [Save Draft] [Submit ↗]

When UTM adds the first nominee → autosave fires → `CasualCourse` row created with `Draft` status → URL updates to `/training/casual-courses/:id` (no full reload, just URL change). Section 2 unlocks and shows the calculator preview inline. Sections 3-4 remain locked.

**No layout shift on autosave.** The page is the same; sections just transition from locked to active/collapsed states based on course status.

---

## Routing

Clean URL pattern:
- `/training/casual-courses/new` — create mode
- `/training/casual-courses/:id` — single detail page (no sub-routes for tabs)
- `/training/casual-courses/:id#financials` — auto-scroll + expand Financials section
- `/training/casual-courses/:id#quotes` — same for quotes
- `/training/casual-courses/:id#travel` — same for travel

No child routes. The page is one component with internal section state.

---

## Component architecture suggestion

Top-level component:
```
casual-course-detail.component.ts
  - Loads course detail
  - Computes section states based on status × role
  - Renders header + pipeline + sections
```

Section components (independent, reusable):
```
casual-course-section-details.component.ts
casual-course-section-financials.component.ts
casual-course-section-quotes.component.ts
casual-course-section-travel.component.ts
```

Each section component receives:
- `state: signal<'collapsed' | 'active' | 'locked'>`
- `course: signal<CasualCourseDto>`
- The course data + state determine what to render

When state = `collapsed`, render only the summary header. When `active`, render full content. When `locked`, render the preview placeholder.

Status pipeline:
```
casual-course-status-pipeline.component.ts
  - Pure presentational
  - Takes course.status, computes node states
  - Click handler emits node clicked event
```

---

## What this replaces

The previous discussion about tabs (`[Details] [Financials] [Quotes] [Travel]`) is **not what we're building**. Don't implement tabs. Don't implement child routes. Implement the stage-based progressive disclosure described above.

If you've already started implementing tabs, stop and discuss with OmanAI before continuing.

---

## Mockup reference

The original mockup HTML (`docs/GTMS-Phase4B-Alpha-Mockup.html`) shows the *content* for each section — what fields, what tables, what dialogs. Use that as content reference. But the *layout* of how those sections fit together on the page follows the stage-based pattern in this document, NOT the tabbed pattern.

Specifically:
- "Price Quotes (PAGE 4.8)" content → Section 3 expanded content
- "Travel Instructions" content → Section 4 expanded content
- "Course Detail Panel — Execution Status" cards → distributed across collapsed-summary states of Sections 1, 3, 4

---

## Patterns to follow (from Phase 4A)

- Standalone components, NO NgModules
- Signals for all state (`signal()`, `computed()`)
- `@if` / `@for` control flow (not `*ngIf` / `*ngFor`)
- `firstValueFrom` for promises (NOT `toPromise()`)
- `LocalizationPipe` standalone import
- ABP proxies direct (NO wrapper services)
- No inline arrow functions in templates — call named methods
- No inline styles in HTML
- `$any($event.target).value` for native input binding
- Individual signals per form field (no getter arrays)
- DevExtreme `toolbarItems` typed property in `ngOnInit()`, not getter
- Localization keys: `'::Training.X'` in TS/HTML, `"Training.X"` in JSON, `"Training:X:Y"` for errors
- Animations: subtle fade/slide for accordion transitions, ~200ms duration

---

End of layout specification. Implement section by section, starting with Section 1 (Course Details) since it's used in both Create and View modes. Stop and review with OmanAI after each section is functional.
