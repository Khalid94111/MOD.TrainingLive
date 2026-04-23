# GTMS Phase 4A — Frontend Implementation Prompt (Claude Code)

**Version:** v1.0
**Date:** April 23, 2026
**Repo:** local working tree at `D:\Training\MOD.Training\angular` (branch `phase4a-frontend`, branched from `phase4a-backend` AFTER backend is merged + proxies are regenerated)
**Scope:** Frontend only — `angular/src/app/Projects/casual-courses/`, shared component extensions, `training-enums.ts`, routes, localization
**Prerequisites (hard gates):**
- Phase 4A backend merged and running locally at `https://localhost:44324`
- Proxies regenerated: `cd angular && abp generate-proxy -t ng` — produces:
  - `src/app/proxy/training/casual-courses/**` (3 services, ~14 DTOs)
  - `src/app/proxy/training/enums/casual-course-status.enum.ts` (9 values)
  - `src/app/proxy/training/enums/funding-scenario.enum.ts` (3 values starting at `1`)
  - `src/app/proxy/training/enums/financial-amount-source.enum.ts`
  - Updated `plan-note-entity-type.enum.ts` with 2 new values

**Parent docs (load into context before executing):**
- `docs/GTMS-Phase4A-Implementation-Prompt.md` (v1.2 — §3 critical rules, §7 page specs)
- `docs/GTMS-Phase4A-API-List.md` (25 endpoints — maps to Angular service calls)
- `docs/GTMS-Phase4A-Mockup.html` (**visual style anchor — match this feel, exactly**)
- `docs/GTMS-Phase4A-Backend-Implementation-Prompt.md` (for DTO/error-code names)
- `docs/GTMS-Phase3-Changes-Implementation-Prompt.md` §12 + §12A (non-negotiable patterns)

---

## 0. Defaulted Decisions

| # | Question | Decision | Rationale |
|---|---|---|---|
| 0.1 | Feature folder location | `angular/src/app/Projects/casual-courses/` — matches capital-P `Projects/` convention of every other training feature | Don't invent a new folder scheme; mirror `Projects/plans/` structure 1:1 |
| 0.2 | Route prefix | `training/casual-courses` — place under the existing `training/` parent | Consistent with `training/plans`, `training/catalog`, etc. Placeholder lines 155–157 in `training.routes.ts` already anticipate this. |
| 0.3 | Local enum stubs in `training-enums.ts` | **Overwrite** `CasualCourseStatus` + `FundingScenario` to match backend proxies byte-for-byte | Pre-v4.5 stubs exist with wrong values — same overwrite rationale as the backend prompt §0.2. Risk #1 there applies here. |
| 0.4 | Cost preview endpoint caller | **Debounce inline on the Request page** (400ms per spec §3.6) — do NOT wrap in a service layer | Matches Phase 3 convention: call proxies directly (see `gtms_frontend_conventions.md` — "inject the generated proxy directly, call via `firstValueFrom()`") |
| 0.5 | Shared component extension strategy | **Extend `NotesDrawerComponent` + `ReturnModalComponent` in place** — add `CasualCourse` + `CasualCourseNomination` cases to their internal switches. No new drawer/modal variants. | Notes/return are polymorphic by design — the whole point of the Phase 3 shared components. Forking would duplicate ~300 lines of drawer UI. |
| 0.6 | `estimate-preview` caching | **No cache — recompute on every keystroke after 400ms debounce.** The endpoint is a pure calculator; caching introduces stale-preview bugs for negligible perf gain. | Mockup confirms live recompute UX (§7.2 of parent). |
| 0.7 | Nominee picker on Request form | Reuse `NominationPickerComponent` unchanged — pass `unitId` from the current user's `MainUnitId`. The picker already knows nothing about plans vs casual courses. | The component's contract (§ existing `nomination-picker.component.ts:17-19`) is pure unit-scoped employee selection — entity-agnostic. |
| 0.8 | Side-menu entry | Add once, under the "Training" group, position after "الدورات المعتمدة" (Plans) — matches IA of the mockup | Single-call update to `side-menu.component.ts` (or equivalent — grep "plans" + "🎓" in layout folder). |

---

## 1. Files to DELETE — none

No deletions. Pure additive work.

---

## 2. Files to MODIFY (6)

### 2.1 `angular/src/app/Projects/shared/models/training-enums.ts`
**Change:** overwrite `CasualCourseStatus` and `FundingScenario` to match backend (lines 77–83 + 27–31 respectively).

```ts
export enum FundingScenario {
  FundingSourceCoversAll    = 1,
  FundingSourceCoversCourse = 2,
  FinancialItemsCoverAll    = 3,
}

export enum CasualCourseStatus {
  Draft             = 0,
  Submitted         = 1,
  UGMApproved       = 2,
  UnderReview       = 3,
  StaffReviewed     = 4,
  TDApproved        = 5,
  THApproved        = 6,
  ReturnedToCreator = 7,
  Rejected          = 8,
}

// NEW — also add FinancialAmountSource mirroring backend enum
export enum FinancialAmountSource {
  FundingSource = 0,
  FinancialItem = 1,
}
```

Also add localization-key maps following the existing `RESULT_TYPE_OPTIONS` / `CONDITION_TYPE_OPTIONS` pattern (after line 135):

```ts
export const CASUAL_COURSE_STATUS_OPTIONS = [
  { value: CasualCourseStatus.Draft,             key: '::Training.CasualCourseStatus.Draft',             cssClass: 'status-draft' },
  { value: CasualCourseStatus.Submitted,         key: '::Training.CasualCourseStatus.Submitted',         cssClass: 'status-submitted' },
  { value: CasualCourseStatus.UGMApproved,       key: '::Training.CasualCourseStatus.UGMApproved',       cssClass: 'status-ugm-approved' },
  { value: CasualCourseStatus.UnderReview,       key: '::Training.CasualCourseStatus.UnderReview',       cssClass: 'status-under-review' },
  { value: CasualCourseStatus.StaffReviewed,     key: '::Training.CasualCourseStatus.StaffReviewed',     cssClass: 'status-staff-reviewed' },
  { value: CasualCourseStatus.TDApproved,        key: '::Training.CasualCourseStatus.TDApproved',        cssClass: 'status-td-approved' },
  { value: CasualCourseStatus.THApproved,        key: '::Training.CasualCourseStatus.THApproved',        cssClass: 'status-th-approved' },
  { value: CasualCourseStatus.ReturnedToCreator, key: '::Training.CasualCourseStatus.ReturnedToCreator', cssClass: 'status-returned' },
  { value: CasualCourseStatus.Rejected,          key: '::Training.CasualCourseStatus.Rejected',          cssClass: 'status-rejected' },
];

export const FUNDING_SCENARIO_OPTIONS = [
  { value: FundingScenario.FundingSourceCoversAll,    key: '::Training.FundingScenario.FundingSourceCoversAll',    hint: '::Training.FundingScenario.NoReallocation' },
  { value: FundingScenario.FundingSourceCoversCourse, key: '::Training.FundingScenario.FundingSourceCoversCourse', hint: '::Training.FundingScenario.PartialReallocation' },
  { value: FundingScenario.FinancialItemsCoverAll,    key: '::Training.FundingScenario.FinancialItemsCoverAll',    hint: '::Training.FundingScenario.FullReallocation' },
];
```

The CSS class names exactly match the `.status-*` selectors in the mockup (`GTMS-Phase4A-Mockup.html` lines 39–47) and must be copied verbatim into `gtms-design.scss` (see §2.2).

### 2.2 `angular/src/app/Projects/shared/gtms-design.scss`
**Change:** append Phase 4A–specific status badge classes (copy lines 38–47 of the mockup) + `.scenario-card` family (lines 81–85 of the mockup). Do NOT duplicate existing classes — grep for `.status-draft` first; if already present from Phase 3 reuse, skip just that one.

Append block, at the end of the file:

```scss
/* ─── Phase 4A: Casual Course status badges (match mockup lines 38–47) ─── */
.status-ugm-approved     { background: #dbeafe; color: #2563eb; }
.status-staff-reviewed   { background: #ede9fe; color: #7c3aed; }
.status-th-approved      { background: #065f46; color: #ffffff; }

/* ─── Phase 4A: Funding scenario cards ─── */
.scenario-card {
  border: 2px solid #e2e8f0;
  border-radius: 14px;
  padding: 16px;
  transition: all 0.25s;
  cursor: pointer;
  background: white;
}
.scenario-card:hover    { border-color: #93c5fd; box-shadow: 0 4px 12px rgba(59,130,246,0.1); }
.scenario-card.selected { border-color: #2563eb; background: linear-gradient(135deg, #eff6ff, #dbeafe); box-shadow: 0 4px 16px rgba(37,99,235,0.2); }
.scenario-card.selected .scenario-num { background: #2563eb; }
.scenario-num {
  background: #94a3b8; color: white;
  width: 32px; height: 32px; border-radius: 50%;
  display: inline-flex; align-items: center; justify-content: center;
  font-weight: bold; font-size: 14px;
}

/* ─── Phase 4A: Preview caption — non-negotiable copy below estimate-preview results ─── */
.preview-caption {
  font-size: 12px;
  color: #b45309;
  background: #fffbeb;
  border-right: 3px solid #f59e0b;
  padding: 8px 12px;
  border-radius: 8px;
  margin-top: 8px;
}
```

### 2.3 `angular/src/app/Projects/shared/components/notes-drawer/notes-drawer.component.ts`
**Change:** the drawer already accepts `entityType: PlanNoteEntityType` — when the backend adds `CasualCourse = 3` and `CasualCourseNomination = 4`, the drawer auto-handles them because it filters via the same enum. **Zero code change** as long as:
- The title slot accepts whatever the caller passes (`title = input<string>('')`) — already does.
- The `loadNotes()` call passes `entityType` + `entityId` to `planNoteService.getList(...)` — already does.

But the template HAS a filter-tabs row that shows `::Training.NotesDrawer.PlanTab` etc. If the tab labels branch on `entityType`, add 2 new cases. Grep the template for `PlanNoteEntityType` — if it does `@switch` on the value to pick the header label, add:

```html
@case (PlanNoteEntityType.CasualCourse) { {{ '::Training.NotesDrawer.CasualCourseTab' | abpLocalization }} }
@case (PlanNoteEntityType.CasualCourseNomination) { {{ '::Training.NotesDrawer.CasualCourseNominationTab' | abpLocalization }} }
```

If the template just echoes `title` from the input, zero change needed in the component — only in the callers (§3.3, §3.4, §3.5).

### 2.4 `angular/src/app/Projects/shared/components/return-modal/return-modal.component.ts`
**Change:** the component has a `switch(this.entityType())` around lines 53–60 that dispatches to `planService.returnToCreator(...)` / `itemService.return(...)` / `nominationService.return(...)`. Add 2 new cases that call the casual-course + casual-course-nomination return endpoints.

```ts
private casualCourseService = inject(CasualCourseService);                 // NEW proxy
private casualCourseNominationService = inject(CasualCourseNominationService); // NEW proxy

async onConfirm(): Promise<void> {
  if (!this.isValid || this.submitting()) return;
  this.submitting.set(true);
  this.error.set(null);
  try {
    const body = { reason: this.reason().trim() };
    switch (this.entityType()) {
      case PlanNoteEntityType.Plan:
        await firstValueFrom(this.planService.returnToCreator(this.entityId(), body));
        break;
      case PlanNoteEntityType.PlanItem:
        await firstValueFrom(this.itemService.return(this.entityId(), body));
        break;
      case PlanNoteEntityType.Nomination:
        await firstValueFrom(this.nominationService.return(this.entityId(), body));
        break;
      case PlanNoteEntityType.CasualCourse:                    // NEW
        await firstValueFrom(this.casualCourseService.return(this.entityId(), body));
        break;
      case PlanNoteEntityType.CasualCourseNomination:          // NEW
        await firstValueFrom(this.casualCourseNominationService.return(this.entityId(), body));
        break;
    }
    this.confirmed.emit(this.reason().trim());
    this.reason.set('');
  } catch (e: any) {
    this.error.set(e?.error?.error?.message ?? 'تعذر إرسال سبب الإعادة');
  } finally {
    this.submitting.set(false);
  }
}
```

Imports update at top of file:
```ts
import { CasualCourseService, CasualCourseNominationService } from 'src/app/proxy/training/casual-courses';
```

### 2.5 `angular/src/app/Projects/training.routes.ts`
**Change:** replace the commented Phase 4 block (lines 154–157) with 4 live routes. Keep alphabetical-by-prefix grouping consistent with existing entries.

```ts
  // ===== Phase 4A: Casual Courses =====
  {
    path: 'casual-courses',
    loadComponent: () =>
      import('./casual-courses/casual-courses-list/casual-courses-list.component')
        .then(m => m.CasualCoursesListComponent),
  },
  {
    path: 'casual-courses/new',
    loadComponent: () =>
      import('./casual-courses/casual-course-request/casual-course-request.component')
        .then(m => m.CasualCourseRequestComponent),
  },
  {
    path: 'casual-courses/:id/edit',
    loadComponent: () =>
      import('./casual-courses/casual-course-request/casual-course-request.component')
        .then(m => m.CasualCourseRequestComponent),
  },
  {
    path: 'casual-courses/:id/review',
    loadComponent: () =>
      import('./casual-courses/casual-course-review/casual-course-review.component')
        .then(m => m.CasualCourseReviewComponent),
  },
  {
    path: 'casual-courses/:id/approve',
    loadComponent: () =>
      import('./casual-courses/casual-course-approval/casual-course-approval.component')
        .then(m => m.CasualCourseApprovalComponent),
  },
```

The Request component serves both `/new` and `/:id/edit` — inspect `route.snapshot.params['id']` on init. Same pattern as `TenantCoursesListComponent` handles create/edit via one component.

### 2.6 `angular/src/app/Projects/shared/index.ts`
**Change:** add re-export for Phase 4A DTOs (grep the proxy output after regen for the correct path — `casual-courses/dtos`):

```ts
// Re-export ABP-generated Casual Course DTO types for convenience
export type {
  CasualCourseDto,
  CasualCourseDetailDto,
  CasualCourseListItemDto,
  CreateUpdateCasualCourseDto,
  CasualCourseGetListInput,
  AssignScenarioDto,
  AssignmentLineDto,
  EstimatePreviewInput,
  EstimatePreviewDto,
  EstimatePreviewItemDto,
  CasualCourseFinancialDto,
  CreateCasualCourseFinancialDto,
  UpdateAmountDto,
  CasualCourseNominationDto,
  RejectDto,
} from '../../proxy/training/casual-courses/dtos';
```

Also add to the enum re-exports the new local options constants (`CASUAL_COURSE_STATUS_OPTIONS`, `FUNDING_SCENARIO_OPTIONS`) — these flow through automatically via `export * from './models/training-enums'` (line 19), so zero edit needed there.

### 2.7 *(Optional — verify only)* `angular/src/app/layout/side-menu.component.ts` *or equivalent*
**Change:** add one menu entry under the Training group. Grep the layout folder for the annual-plans menu entry (emoji 🗓️ or 📋) and insert directly after:

```ts
{
  label: '::Menu.Training.CasualCourses',
  icon: '🎯',
  link: '/training/casual-courses',
  permission: 'Training.CasualCourses',
},
```

If the menu is data-driven from `route.provider.ts`, add there instead. Verification before starting: grep `Menu.Training.Plans` to locate the exact file.

---

## 3. Files to ADD (~16)

### 3.1 Folder scaffold — `angular/src/app/Projects/casual-courses/`

```
casual-courses/
  casual-courses-list/
    casual-courses-list.component.ts
    casual-courses-list.component.html
    casual-courses-list.component.scss
  casual-course-request/
    casual-course-request.component.ts
    casual-course-request.component.html
    casual-course-request.component.scss
  casual-course-review/
    casual-course-review.component.ts
    casual-course-review.component.html
    casual-course-review.component.scss
  casual-course-approval/
    casual-course-approval.component.ts
    casual-course-approval.component.html
    casual-course-approval.component.scss
  models/
    casual-course-view-model.ts   (role-aware action map — see §3.6)
```

All components are `standalone: true`, import `CommonModule` + shared components, use `gtms-design.scss` via the relative path `../../shared/gtms-design.scss` (established pattern — see `plan-review.component.ts:51`).

### 3.2 PAGE 4.1 — `casual-courses-list.component.ts`

**Layout:** mirrors `annual-plan-list.component` — card-per-row grid, role-aware action column, client-side filters via signals.

**Template structure (HTML, condensed):**
```html
<div class="p-6" dir="rtl">
  <!-- Header -->
  <div class="card p-5 mb-5 flex items-center justify-between">
    <div>
      <h1 class="text-2xl font-bold">الدورات العرضية</h1>
      <p class="text-sm text-slate-500">طلبات الدورات خارج الخطة السنوية</p>
    </div>
    @if (canCreate()) {
      <a routerLink="/training/casual-courses/new" class="btn-primary px-5 py-2 rounded-xl">
        ✏️ طلب جديد
      </a>
    }
  </div>

  <!-- Filter toolbar -->
  <div class="card p-4 mb-5 flex gap-3 flex-wrap">
    <select [value]="fStatus()" (change)="fStatus.set($any($event.target).value)" class="input-field px-3 py-2">
      <option value="">كل الحالات</option>
      @for (opt of STATUS_OPTIONS; track opt.value) {
        <option [value]="opt.value">{{ opt.key | abpLocalization }}</option>
      }
    </select>
    <input [value]="fSearch()" (input)="fSearch.set($any($event.target).value)"
           placeholder="بحث بالاسم أو الجهة الممولة..." class="input-field px-3 py-2 flex-1" />
    <label class="flex items-center gap-2 text-sm">
      <input type="checkbox" [checked]="fOnlyMine()" (change)="fOnlyMine.set($any($event.target).checked)" />
      طلباتي فقط
    </label>
  </div>

  <!-- Grid -->
  @if (loading()) { <!-- skeleton rows --> }
  @else if (filteredRows().length === 0) {
    <div class="card p-12 text-center">
      <div class="text-6xl mb-3">🎯</div>
      <h3 class="font-bold mb-2">لا توجد طلبات</h3>
      <a routerLink="/training/casual-courses/new" class="btn-primary px-4 py-2 rounded-lg inline-block">إنشاء أول طلب</a>
    </div>
  }
  @else {
    <table class="card w-full text-sm">
      <thead>...</thead>
      <tbody>
        @for (row of filteredRows(); track row.id) {
          <tr class="table-row" [class.returned-row]="row.isReturned"
              (click)="onRowClick(row)">
            <td>{{ row.courseNameAr }}</td>
            <td>{{ row.fundingSource || '—' }}</td>
            <td>{{ row.unitName }}</td>
            <td>{{ row.requesterName }}</td>
            <td class="text-center">{{ row.nomineesCount }}</td>
            <td class="text-end">{{ row.estimatedTotalCost | number:'1.3-3' }} ر.ع</td>
            <td><span class="status-badge" [class]="statusCss(row.status)">{{ statusLabel(row.status) | abpLocalization }}</span></td>
            <td class="text-center">{{ actionLabel(row) }}</td>
          </tr>
        }
      </tbody>
    </table>
  }
</div>
```

**TS skeleton (key parts):**
```ts
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { LocalizationModule } from '@abp/ng.core';   // for abpLocalization pipe
import { CasualCourseService } from 'src/app/proxy/training/casual-courses';
import { CasualCourseListItemDto } from 'src/app/proxy/training/casual-courses/dtos';
import {
  CasualCourseStatus,
  CASUAL_COURSE_STATUS_OPTIONS,
  TrainingLocalizationHelper,
} from '../../shared';
import { actionForRow, CasualCourseRowAction } from '../models/casual-course-view-model';

@Component({
  standalone: true,
  selector: 'app-casual-courses-list',
  templateUrl: './casual-courses-list.component.html',
  styleUrls: ['./casual-courses-list.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, RouterLink, LocalizationModule],
})
export class CasualCoursesListComponent implements OnInit {
  private service = inject(CasualCourseService);
  private router = inject(Router);
  l = inject(TrainingLocalizationHelper);

  readonly STATUS_OPTIONS = CASUAL_COURSE_STATUS_OPTIONS;

  rows = signal<CasualCourseListItemDto[]>([]);
  loading = signal(true);

  fStatus = signal<string>('');
  fSearch = signal<string>('');
  fOnlyMine = signal<boolean>(false);

  filteredRows = computed(() => {
    const q = this.fSearch().trim().toLowerCase();
    const status = this.fStatus();
    const onlyMine = this.fOnlyMine();
    return this.rows().filter(r => {
      if (status !== '' && r.status !== +status) return false;
      if (q && !(r.courseNameAr?.toLowerCase().includes(q) || r.fundingSource?.toLowerCase().includes(q))) return false;
      if (onlyMine && !r.isMine) return false;
      return true;
    });
  });

  async ngOnInit(): Promise<void> {
    await this.loadRows();
  }

  async loadRows(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await firstValueFrom(this.service.getList({ maxResultCount: 1000 }));
      this.rows.set(result.items ?? []);
    } finally {
      this.loading.set(false);
    }
  }

  statusCss(s: CasualCourseStatus): string {
    return this.STATUS_OPTIONS.find(o => o.value === s)?.cssClass ?? '';
  }
  statusLabel(s: CasualCourseStatus): string {
    return this.STATUS_OPTIONS.find(o => o.value === s)?.key ?? '';
  }

  onRowClick(row: CasualCourseListItemDto): void {
    const action = actionForRow(row);      // see §3.6
    if (action.route) this.router.navigate([action.route(row.id)]);
  }

  actionLabel(row: CasualCourseListItemDto): string {
    return actionForRow(row).labelAr;
  }

  canCreate(): boolean { return true; /* permission check via ABP — real impl injects PermissionService */ }
}
```

**Explicit `@switch` rule:** the status-to-action mapping (§3.6) MUST have a case for every one of the 9 enum values — no `@default` fallback (Phase 3 Changes lesson from the v4.4 session).

### 3.3 PAGE 4.2 — `casual-course-request.component.ts` (UTM)

**Purpose:** UTM creates a new casual course OR edits one in `Draft` / `ReturnedToCreator`. Single component reused for both via route param.

**Template (7 sections — match mockup lines 486–1050):**
1. **Returned-state banner** — render when `casualCourse()?.status === ReturnedToCreator`. Amber; shows the latest return reason (from `latestReturnNote.text`). Button opens the `NotesDrawerComponent`.
2. **Section A — Course details** — TenantCourse select, CourseType, Priority, Justification, DescriptionAr, ObjectivesAr. On TenantCourse change, default DescriptionAr/ObjectivesAr from the selected course's entity (read via `TenantCourseService.get(id)`).
3. **Section B — Duration + dates** — DurationYears/Months/Days + EstimatedDateFrom/To with client-side warning when date range ≠ duration.
4. **Section C — Funding source** — conditional: only visible when `fCourseType() !== CourseType.Internal`. `FundingSource` text input + Nebras-balance placeholder card (hard-coded Arabic line from mockup line 817).
5. **Section D — Nominees** — `<gtms-nomination-picker [unitId]="currentUnitId()" [initialSelection]="fNomineeIds()" (selectionChange)="fNomineeIds.set($event)"></gtms-nomination-picker>`. Chip count display.
6. **Section E — Cost preview** (debounced 400ms):
   - Trigger on change to any of `fCourseType`, `fDurationDays`, `fNomineeIds().length`, `fTenantCourseId`.
   - Call `service.getEstimatePreview({ tenantCourseId, courseType, durationDays, nomineeCount })` via `firstValueFrom`.
   - Render table of `items[]` + total + **mandatory caption**: `<p class="preview-caption">{{ '::Training.CasualCourse.EstimateDisclaimer' | abpLocalization }}</p>` — the disclaimer text is the non-negotiable messaging rule from parent §3.6.
7. **Section F — Actions** — `Save Draft` / `Submit` / `Cancel`. Submit is disabled until required fields pass client-side validation (duplicated server-side by `CasualCourseValidator.ValidateForSubmitAsync`).

**Debounce implementation (signals + effect):**
```ts
import { Component, OnInit, effect, inject, signal, computed } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, switchMap, of, firstValueFrom } from 'rxjs';

// inside the component class:
private previewInputs = computed(() => ({
  tenantCourseId: this.fTenantCourseId(),
  courseType: this.fCourseType(),
  durationDays: this.fDurationDays(),
  nomineeCount: this.fNomineeIds().length,
}));

preview = signal<EstimatePreviewDto | null>(null);
previewLoading = signal(false);

constructor() {
  toObservable(this.previewInputs)
    .pipe(
      debounceTime(400),
      distinctUntilChanged((a, b) =>
        a.tenantCourseId === b.tenantCourseId &&
        a.courseType === b.courseType &&
        a.durationDays === b.durationDays &&
        a.nomineeCount === b.nomineeCount),
      switchMap(inputs => {
        if (!inputs.tenantCourseId || inputs.durationDays < 1 || inputs.nomineeCount < 1) {
          this.preview.set(null);
          return of(null);
        }
        this.previewLoading.set(true);
        return this.service.getEstimatePreview(inputs);
      }),
    )
    .subscribe(result => {
      if (result) this.preview.set(result);
      this.previewLoading.set(false);
    });
}
```

**Submit flow:**
```ts
async onSubmit(isDraft: boolean): Promise<void> {
  const dto: CreateUpdateCasualCourseDto = {
    tenantCourseId: this.fTenantCourseId(),
    courseType: this.fCourseType(),
    priority: this.fPriority(),
    justification: this.fJustification(),
    descriptionAr: this.fDescriptionAr(),
    objectivesAr: this.fObjectivesAr(),
    durationYears: this.fDurationYears(),
    durationMonths: this.fDurationMonths(),
    durationDays: this.fDurationDays(),
    estimatedDateFrom: this.fDateFrom(),
    estimatedDateTo: this.fDateTo(),
    fundingSource: this.fFundingSource() || undefined,
    nomineeEmployeeIds: this.fNomineeIds(),
  };
  try {
    const saved = this.id()
      ? await firstValueFrom(this.service.update(this.id()!, dto))
      : await firstValueFrom(this.service.create(dto));
    if (!isDraft) {
      await firstValueFrom(this.service.submit(saved.id));
    }
    this.router.navigate(['/training/casual-courses']);
  } catch (e: any) {
    this.submitError.set(this.mapError(e));
  }
}
```

**Error mapping:** server error codes like `Training:CasualCourse:FundingSourceRequired` translate via `abpLocalization`. Wrap in a small private `mapError(e)` that reads `e?.error?.error?.code` first, falls back to `message`.

### 3.4 PAGE 4.3 — `casual-course-review.component.ts` (Staff)

**Layout:** 5 panels per parent spec §7.3:

1. **Course summary** — read-only, right side of top row
2. **Nominations** — table with condition status + nomination-level return button (opens `ReturnModalComponent` with `entityType = PlanNoteEntityType.CasualCourseNomination`)
3. **Funding Scenario** — 3 `.scenario-card` radios; bound to `fScenario` signal. Each card shows a reallocation-preview badge (no / partial / full)
4. **Financial Items** — flat table (no rank grid). Columns: name, source auto-set by scenario, rate, days (readonly from `DurationDays + extras`), nominee count, estimated amount (editable), notes, delete. Auto-fill button at top. Grand total row at bottom (computed via `financials().reduce((s,r) => s + r.estimatedAmountOMR, 0)`). "Save Assignments" stays in `UnderReview`; "Submit to TD" sends `Commit=true` in `AssignScenarioDto`.
5. **Notes drawer** — `<gtms-notes-drawer [entityType]="PlanNoteEntityType.CasualCourse" [entityId]="courseId" ...>`

**Action buttons:** `Save Assignments` / `Submit to TD` / `Return to Creator` / `Reject`.

**Source-derivation logic** (client-side preview of what server will write):
```ts
sourceFor(financialItemId: string): FinancialAmountSource {
  const scenario = this.fScenario();
  const isTravel = this.isTravelItem(financialItemId);   // code-match local helper
  switch (scenario) {
    case FundingScenario.FundingSourceCoversAll:    return FinancialAmountSource.FundingSource;
    case FundingScenario.FundingSourceCoversCourse: return isTravel ? FinancialAmountSource.FinancialItem : FinancialAmountSource.FundingSource;
    case FundingScenario.FinancialItemsCoverAll:    return FinancialAmountSource.FinancialItem;
    default: return FinancialAmountSource.FundingSource;
  }
}
```

`isTravelItem` checks the cached financial-item's `code` against `['TRAVEL_ALLOWANCE', 'ACCOMMODATION', 'TRANSPORT']` — same set the backend uses (Risk #4 in backend prompt). If this set ever changes server-side, **both** must update in lockstep.

### 3.5 PAGE 4.4 — `casual-course-approval.component.ts` (TD / TH)

**Layout:** read-only version of Review (no scenario editing, no financial edits). Role-aware — determine TD vs TH by the casual course's current status:
- `Status == StaffReviewed` → TD action
- `Status == TDApproved` → TH action
- anything else → read-only with banner

**Cost gate UX:** if `estimatedTotalCost == null || estimatedTotalCost <= 0`, disable the Approve button (`btn-disabled`) with a Tajawal tooltip: `"لا يمكن الاعتماد — التكلفة الإجمالية غير محددة"`. Same rule as the Phase 3 plan-approval page.

**Action buttons:** `Approve` / `Return to Creator` / `Reject`.

### 3.6 `angular/src/app/Projects/casual-courses/models/casual-course-view-model.ts`

Role/status → action mapping. Pure TypeScript (no DI). Covers all 9 statuses explicitly — no fallback:

```ts
import { CasualCourseStatus } from '../../shared';
import { CasualCourseListItemDto } from 'src/app/proxy/training/casual-courses/dtos';

export interface CasualCourseRowAction {
  labelAr: string;
  route: ((id: string) => string) | null;
  allowedFor: 'utm' | 'ugm' | 'staff' | 'td' | 'th' | 'readonly';
}

export function actionForRow(row: CasualCourseListItemDto): CasualCourseRowAction {
  switch (row.status) {
    case CasualCourseStatus.Draft:
      return { labelAr: 'تعديل / إرسال', route: id => `/training/casual-courses/${id}/edit`, allowedFor: 'utm' };
    case CasualCourseStatus.Submitted:
      return { labelAr: 'اعتماد UGM', route: id => `/training/casual-courses/${id}/review`, allowedFor: 'ugm' };
    case CasualCourseStatus.UGMApproved:
      return { labelAr: 'بدء المراجعة', route: id => `/training/casual-courses/${id}/review`, allowedFor: 'staff' };
    case CasualCourseStatus.UnderReview:
      return { labelAr: 'فتح صفحة المراجعة', route: id => `/training/casual-courses/${id}/review`, allowedFor: 'staff' };
    case CasualCourseStatus.StaffReviewed:
      return { labelAr: 'اعتماد TD', route: id => `/training/casual-courses/${id}/approve`, allowedFor: 'td' };
    case CasualCourseStatus.TDApproved:
      return { labelAr: 'اعتماد نهائي TH', route: id => `/training/casual-courses/${id}/approve`, allowedFor: 'th' };
    case CasualCourseStatus.ReturnedToCreator:
      return { labelAr: 'تعديل وإعادة إرسال', route: id => `/training/casual-courses/${id}/edit`, allowedFor: 'utm' };
    case CasualCourseStatus.THApproved:
      return { labelAr: 'عرض فقط (معتمد)', route: id => `/training/casual-courses/${id}/review`, allowedFor: 'readonly' };
    case CasualCourseStatus.Rejected:
      return { labelAr: 'عرض فقط (مرفوض)', route: id => `/training/casual-courses/${id}/review`, allowedFor: 'readonly' };
  }
}
```

### 3.7 Localization additions — backend territory

Localization `en.json` + `ar.json` live under the .NET side (`src/MOD.Training.Domain.Shared/Localization/Training/`). They're maintained in the backend prompt §3.10. Frontend has ZERO JSON edits. The `abpLocalization` pipe resolves keys at runtime.

### 3.8 *(Do NOT create)* Wrapper proxy service

Phase 3 convention (locked in `gtms_frontend_conventions.md`): "ABP proxy services directly — no wrapper services in `shared/services/*-proxy.service.ts`". The generated `CasualCourseService`, `CasualCourseFinancialService`, `CasualCourseNominationService` under `src/app/proxy/training/casual-courses/` are consumed directly via `inject()`.

Exception: nothing in Phase 4A justifies a wrapper. If one looks tempting, re-read `gtms_frontend_conventions.md`.

---

## 4. Non-Negotiable Patterns Checklist (inherited from Phase 3)

Apply to every new component file. Flag any violation during review.

- [x] `standalone: true` — no NgModules
- [x] Signals per form field (`fNameAr = signal('')`), not Reactive Forms
- [x] `[value]="fField()"` + `(input)="fField.set($any($event.target).value)"` bindings
- [x] `@if` / `@for` / `@switch` — never `*ngIf` / `*ngFor`
- [x] No arrow functions in templates — named methods only
- [x] `firstValueFrom` from rxjs — never `.toPromise()`
- [x] `$any($event.target).value` for native input binding
- [x] Hard-coded Arabic in templates + helper methods (Phase 3 convention — NOT localization keys except for error messages + enum options)
- [x] Map-of-signals pattern for expand caching (if any accordion lands on PAGE 4.1)
- [x] Localization keys: `'::Training.KeyName'` in HTML/TS, `'Training.KeyName'` in JSON, `'Training:Entity:Error'` error codes
- [x] **Explicit `@switch` cases for all 9 `CasualCourseStatus` values — no `@default`**
- [x] Inject ABP proxies directly — no wrapper services
- [x] Styles imported via `../../shared/gtms-design.scss` relative path
- [x] NO DevExtreme anywhere in this phase

---

## 5. Implementation Sequence (Claude Code session)

Execute on branch `phase4a-frontend` (off `phase4a-backend`):

1. **Prereq — run proxy regen** — `cd angular && abp generate-proxy -t ng` against the running backend. Verify `src/app/proxy/training/casual-courses/` exists with 3 services + DTOs folder.
2. **Enum + style updates** — §2.1, §2.2 (2 files)
3. **Shared component extensions** — §2.3, §2.4 (add 2 cases to return modal, verify drawer has no template branch)
4. **Routes** — §2.5 (1 file)
5. **View-model helper** — §3.6 (1 file)
6. **PAGE 4.1 — List** — §3.2 (3 files: .ts/.html/.scss)
7. **PAGE 4.2 — Request** — §3.3 (3 files)
8. **PAGE 4.3 — Review** — §3.4 (3 files)
9. **PAGE 4.4 — Approval** — §3.5 (3 files)
10. **Shared barrel export** — §2.6 (1 file)
11. **Side-menu entry** — §2.7 (1 file, verify path first)
12. **`ng build`** — must pass with zero errors and zero NEW warnings
13. **Smoke test** — `ng serve`; log in as each seeded role (UTM / UGM / Staff / TD / TH) against Ground Forces tenant; walk:
    - UTM: create → submit → see `Submitted` row
    - UGM: open `Submitted` row → approve → see `UGMApproved`
    - Staff: open → Start Review → Assign Scenario + financials → Submit to TD
    - TD: approve → see `TDApproved`
    - TH: approve → see `THApproved`
    - Return + resubmit loop: UGM returns → UTM sees amber banner → fixes → resubmit → lands back at the returning stage (not forced to UnderReview)

After step 12 the build MUST be clean. `ng build --configuration production` must also pass if the CI pipeline runs it.

### Commit granularity
One commit per logical step above. Prefix with `feat(phase4a-frontend):`:
```
feat(phase4a-frontend): regenerate casual-course proxies
feat(phase4a-frontend): update training-enums + gtms-design.scss for casual courses
feat(phase4a-frontend): extend return-modal for casual course return endpoints
feat(phase4a-frontend): add casual-courses routes
feat(phase4a-frontend): implement casual-courses list page
feat(phase4a-frontend): implement casual-course request page
feat(phase4a-frontend): implement casual-course review page
feat(phase4a-frontend): implement casual-course approval page
feat(phase4a-frontend): wire casual courses into shared index + side menu
```

---

## 6. File Inventory Summary

| Category | Modified | Added | Total |
|---|---|---|---|
| Shared enums + styles | 2 | 0 | 2 |
| Shared components | 2 | 0 | 2 |
| Routes | 1 | 0 | 1 |
| Shared barrel | 1 | 0 | 1 |
| Side menu | 1 | 0 | 1 |
| Casual course pages (4 × 3 files) | 0 | 12 | 12 |
| View-model helper | 0 | 1 | 1 |
| **Total** | **7** | **13** | **20** |

(Proxies are auto-generated, not counted.)

---

## 7. Risk Flags ⚠️

| # | Risk | Mitigation |
|---|---|---|
| 1 | **Enum overwrite in `training-enums.ts`** silently changes numeric values used elsewhere | Grep for `CasualCourseStatus.` + `FundingScenario.` across the codebase before rewriting. Phase 4A is the first real consumer, so hits should only be in stub files. |
| 2 | **Travel-item code set drift** between frontend (§3.4) and backend (§3.8.1 #10) | When either side changes the `['TRAVEL_ALLOWANCE', 'ACCOMMODATION', 'TRANSPORT']` set, both files must be updated in the same commit. Add a comment on both sides linking to the other file. |
| 3 | **`estimate-preview` 400ms debounce** can race with form reset — user clears a field, debounce fires with stale inputs | Use `distinctUntilChanged` on the tuple (see §3.3 code). Also clear `preview()` synchronously when `fTenantCourseId()` becomes empty. |
| 4 | **Edit flow nominee picker** repeats the Phase 3 bug: `initialSelection` didn't populate on Edit (see `gtms_phase3_v44_frontend.md` known polish gaps) | On `ngOnInit` of Request page when `id` param exists, call `GetAsync` → read `nominations[].employeeId` → `fNomineeIds.set(ids)` BEFORE the picker renders. Unit-test this path manually during smoke test. |
| 5 | **`abp generate-proxy` regenerates all proxies**, potentially overwriting Phase 3 tweaks | Verify `git status` after proxy regen — only `proxy/training/casual-courses/` and `proxy/training/enums/*.enum.ts` should have diffs. If unrelated proxy files change, the backend API contract drifted — investigate. |
| 6 | **Notes-drawer tab labels** might hardcode the 3 Phase-3 tabs | If `notes-drawer.component.html` has a switch/if chain on entity type to render tab pills, extend it (§2.3). If it accepts `title` input only, no change. Verify by grepping the .html for `PlanNoteEntityType`. |
| 7 | **`abpLocalization` pipe** — any typo in the `::Training.KeyName` style silently renders the literal key | Run the smoke test with the browser in AR locale; any displayed `::Training.X.Y` literal means the JSON key is missing in `ar.json` (backend §3.10). |

---

## 8. Final Confirmation Checklist

Before starting the Claude Code session, confirm:

- [ ] Backend Phase 4A is merged + DB migrated + seeder re-run (9 seeded casual courses exist)
- [ ] `abp generate-proxy -t ng` has been run and committed on its own branch
- [ ] Swagger at `https://localhost:44324/swagger/index.html` shows all 25 Phase 4A endpoints
- [ ] The 8 defaulted decisions in §0 are acceptable
- [ ] Risk #2 acknowledged — frontend/backend travel-item list will be kept in lockstep
- [ ] Dev SPA runs cleanly on `http://localhost:4200` before changes begin
- [ ] `ng build` passes on `phase4a-backend` branch pre-edit (baseline)

---

## 9. What Goes To Claude Code

This document + Phase 4A Implementation Prompt v1.2 + Mockup HTML + Backend Implementation Prompt (for DTO/error names) → load into a fresh Claude Code session against this repo with the task:

> "Execute Section 5 sequence from `docs/GTMS-Phase4A-Frontend-Implementation-Prompt.md`. Commit after each numbered step. Do NOT run proxy regen inside Claude Code — it's a prerequisite and must have been completed before this session starts (step 1 is verification only). Stop and ask before any destructive action."

Claude Code will:
- Edit files on branch `phase4a-frontend`
- Run `ng build` after each logical group
- Commit per logical step

---

## 10. Post-Frontend Deliverables (out of scope here)

After frontend lands:
1. Smoke-test report bug list → follow-up v4.5.1 pass
2. `GTMS-Project-Checkpoint-v4_5.md` — snapshot of backend + frontend state, cumulative endpoint count ~160
3. Memory update: new `gtms_phase4a_v45_frontend.md` mirroring the v4.4 memory doc (shipped components + known gaps)

---

*End of Phase 4A Frontend Implementation Prompt — v1.0 — April 23, 2026*
