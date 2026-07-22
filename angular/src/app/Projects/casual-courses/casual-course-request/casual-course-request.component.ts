import { Component, OnInit, computed, effect, inject, input, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, firstValueFrom } from 'rxjs';
import { debounceTime } from 'rxjs/operators';

import { CasualCourseService } from 'src/app/proxy/training/casual-courses';
import type {
  CalculatePreviewDto,
  CalculatePreviewInput,
  CasualCourseDetailDto,
  CreateUpdateCasualCourseDto,
} from 'src/app/proxy/training/casual-courses/dtos/models';
import { TenantCourseService } from 'src/app/proxy/training/tenant-courses/tenant-course.service';
import type { TenantCourseDto } from 'src/app/proxy/training/tenant-courses/dtos/models';
import { HrLookupService } from 'src/app/proxy/training/hr-integration/hr-lookup.service';
import type { EmployeeLookupDto } from 'src/app/proxy/training/hr-integration/models';

import { CasualCourseStatus, CourseType, TrainingLocalizationHelper } from '../../shared';
import { NominationPickerComponent } from '../../shared/components/nomination-picker/nomination-picker.component';
import { NotesDrawerComponent } from '../../shared/components/notes-drawer/notes-drawer.component';
import { PlanNoteEntityType } from 'src/app/proxy/training/enums/plan-note-entity-type.enum';
import { CasualCourseActionService } from '../casual-course-detail/casual-course-action.service';
import { CasualCourseDetailRefreshService } from '../casual-course-detail/casual-course-detail-refresh.service';

@Component({
  standalone: true,
  selector: 'app-casual-course-request',
  templateUrl: './casual-course-request.component.html',
  styleUrls: ['./casual-course-request.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, RouterLink, NominationPickerComponent, NotesDrawerComponent],
})
export class CasualCourseRequestComponent implements OnInit {
  private service = inject(CasualCourseService);
  private tenantCourseService = inject(TenantCourseService);
  private hrService = inject(HrLookupService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private actions = inject(CasualCourseActionService);
  private refreshShell = inject(CasualCourseDetailRefreshService);
  private destroyRef = inject(DestroyRef);
  l = inject(TrainingLocalizationHelper);

  CourseType = CourseType;
  CasualCourseStatus = CasualCourseStatus;
  PlanNoteEntityType = PlanNoteEntityType;

  /** Render slot:
   *   'details'   — course form + nominees only (Section 1 active body)
   *   'financials'— calculator preview only (Section 2 active body, future)
   *   'all'       — full standalone layout (legacy `/new` route + reviewers)
   */
  mode = input<'details' | 'financials' | 'all'>('all');
  showDetails    = computed(() => this.mode() === 'details'    || this.mode() === 'all');
  showFinancials = computed(() => this.mode() === 'financials' || this.mode() === 'all');

  id = signal<string | null>(null);
  embedded = signal<boolean>(false);
  casualCourse = signal<CasualCourseDetailDto | null>(null);
  preview = signal<CalculatePreviewDto | null>(null);
  previewLoading = signal(false);
  loading = signal(false);
  saving = signal(false);
  submitError = signal<string | null>(null);
  notesOpen = signal(false);
  expandedItemId = signal<string | null>(null);

  tenantCourses = signal<TenantCourseDto[]>([]);
  currentUnitId = signal<string>('');

  fTenantCourseId = signal<string>('');
  fCourseType = signal<CourseType>(CourseType.ExternalLocal);
  fPriority = signal<number>(3);
  fJustification = signal<string>('');
  fDescriptionAr = signal<string>('');
  fObjectivesAr = signal<string>('');
  fDurationYears = signal<number>(0);
  fDurationMonths = signal<number>(0);
  fDurationDays = signal<number>(0);
  fDateFrom = signal<string>('');
  fDateTo = signal<string>('');
  fFundingSourceName = signal<string>('');
  fFundingSourceVoteCode = signal<string>('');
  fCourseCost = signal<number | null>(null);
  fNomineeIds = signal<string[]>([]);

  // Patch 5 — Section E is now a read-only projection driven by POST /calculate-preview.
  // Recompute on any input change that affects the breakdown, debounced 400ms.
  private previewTrigger$ = new Subject<void>();

  isEdit = computed(() => !!this.id());
  isReturned = computed(() => this.casualCourse()?.status === CasualCourseStatus.ReturnedToCreator);
  showFundingSource = computed(() => this.fCourseType() !== CourseType.Internal);
  nomineeCount = computed(() => this.fNomineeIds().length);
  initialNomineeEmployees = computed<Partial<EmployeeLookupDto>[]>(() =>
    (this.casualCourse()?.nominations ?? []).map(nomination => ({
      id: nomination.employeeId,
      fullNameAr: nomination.employeeName ?? '',
      rankNameAr: nomination.rankName ?? '',
      serviceNumber: nomination.serviceNumber ?? '',
    })),
  );
  hasPreview = computed(() => (this.preview()?.items?.length ?? 0) > 0);
  grandTotal = computed(() => this.preview()?.totalOMR ?? 0);
  canSaveDraft = computed(() =>
    !!this.fTenantCourseId()
    && !!this.currentUnitId()
    && this.fDurationDays() > 0
    && !!this.fDateFrom()
    && !!this.fDateTo());

  readinessChecks = computed(() => {
    const checks = [
      { label: 'اختيار الدورة', done: !!this.fTenantCourseId() },
      { label: 'المبرر', done: this.fJustification().trim().length > 0 },
      {
        label: 'المدة والتاريخ',
        done: this.fDurationDays() > 0 && !!this.fDateFrom() && !!this.fDateTo(),
      },
      { label: 'المرشحون', done: this.fNomineeIds().length > 0 },
    ];

    if (this.fCourseType() !== CourseType.Internal) {
      checks.splice(3, 0, {
        label: 'بيانات التمويل',
        done: this.fFundingSourceName().trim().length > 0
          && this.fFundingSourceVoteCode().trim().length > 0,
      });
    }

    return checks;
  });
  readinessPercent = computed(() => {
    const checks = this.readinessChecks();
    return checks.length === 0
      ? 0
      : Math.round((checks.filter(check => check.done).length / checks.length) * 100);
  });

  computedDateTo = computed(() => {
    const from = this.fDateFrom();
    const years = this.fDurationYears();
    const months = this.fDurationMonths();
    const days = this.fDurationDays();
    if (!from || (years <= 0 && months <= 0 && days <= 0)) return '';
    const d = new Date(from);
    if (Number.isNaN(d.getTime())) return '';
    d.setFullYear(d.getFullYear() + years);
    d.setMonth(d.getMonth() + months);
    d.setDate(d.getDate() + Math.max(days - 1, 0));
    return d.toISOString().substring(0, 10);
  });

  canSubmit = computed(() => {
    const externalFundingOk =
      this.fCourseType() === CourseType.Internal ||
      (this.fFundingSourceName().trim().length > 0 &&
        this.fFundingSourceVoteCode().trim().length > 0);
    return (
      !!this.fTenantCourseId() &&
      this.fJustification().trim().length > 0 &&
      !!this.fDateFrom() &&
      !!this.fDateTo() &&
      this.fDurationDays() > 0 &&
      this.fNomineeIds().length > 0 &&
      externalFundingOk
    );
  });

  constructor() {
    // Debounced preview refresh — fires after the user stops typing for 400ms.
    this.previewTrigger$
      .pipe(debounceTime(400))
      .subscribe(() => this.refreshPreview());

    // Auto-recompute DateTo from start date + duration.
    effect(() => {
      const next = this.computedDateTo();
      if (this.fDateTo() !== next) {
        this.fDateTo.set(next);
      }
    }, { allowSignalWrites: true });

    // Auto-trigger preview on inputs that affect the breakdown.
    effect(() => {
      // Read the signals so the effect tracks them.
      this.fCourseType();
      this.fDurationDays();
      this.fNomineeIds();
      this.fCourseCost();
      this.previewTrigger$.next();
    }, { allowSignalWrites: true });
  }

  async ngOnInit(): Promise<void> {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.embedded.set(!!this.route.snapshot.data['embedded']);

    // When embedded, the URL may have been changed by history.replaceState
    // (after /new autosave) without Angular Router knowing. In that case
    // route.snapshot.paramMap is null but the refresh service holds the
    // newly-attached id — use that as the effective id.
    const attachedId = this.embedded() ? this.refreshShell.currentAttachedId() : null;
    const effectiveId = idParam ?? attachedId;
    this.id.set(effectiveId);

    this.loading.set(true);
    try {
      await Promise.all([this.loadCurrentEmployee(), this.loadTenantCourses()]);
      if (effectiveId) {
        await this.loadExisting(effectiveId);
      }
    } finally {
      this.loading.set(false);
    }

    // Header action bar dispatch — only acts when this component is the active
    // variant (Draft / ReturnedToCreator OR creating a new course on /new).
    // Gated on mode='details' so a sibling instance dispatched in Section 2
    // (mode='financials') doesn't double-fire the same action.
    // takeUntilDestroyed cleans up when the component is torn down — without
    // it, dead instances (e.g., after navigating to the list post-submit)
    // would keep firing onSave against their stale id and hit the backend's
    // CannotEditInThisStatus guard.
    this.actions.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(action => {
      if (!this.embedded() || this.mode() !== 'details') return;
      const s = this.casualCourse()?.status;
      const isCreatingNew = !this.id();
      const isOurTurn = isCreatingNew ||
        s === CasualCourseStatus.Draft || s === CasualCourseStatus.ReturnedToCreator;
      if (!isOurTurn) return;
      if (action === 'saveDraft') void this.onSave(true);
      else if (action === 'submit') void this.onSave(false);
    });

    // Hydrate when the shell attaches a freshly-created course id (post /new
    // autosave) — covers both the dispatching instance and any later-mounting
    // sibling instance (e.g., Section 2 transitioning from locked to active).
    this.refreshShell.attachEvents.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(async newId => {
      if (!this.embedded() || !newId || this.id() === newId) return;
      this.id.set(newId);
      await this.loadExisting(newId);
    });
  }

  private async loadCurrentEmployee(): Promise<void> {
    try {
      const me = await firstValueFrom(this.hrService.getCurrentEmployee());
      this.currentUnitId.set(me?.mainUnitId ?? '');
    } catch {
      this.currentUnitId.set('');
    }
  }

  private async loadTenantCourses(): Promise<void> {
    const r = await firstValueFrom(
      this.tenantCourseService.getList({ maxResultCount: 500, isActive: true }),
    );
    this.tenantCourses.set(r.items ?? []);
  }

  private async loadExisting(id: string): Promise<void> {
    const detail = await firstValueFrom(this.service.getDetail(id));
    this.casualCourse.set(detail);
    this.fTenantCourseId.set(detail.tenantCourseId ?? '');
    this.fCourseType.set(detail.courseType ?? CourseType.Internal);
    this.fPriority.set(detail.priority ?? 3);
    this.fJustification.set(detail.justification ?? '');
    this.fDescriptionAr.set(detail.descriptionAr ?? '');
    this.fObjectivesAr.set(detail.objectivesAr ?? '');
    this.fDurationYears.set(detail.durationYears ?? 0);
    this.fDurationMonths.set(detail.durationMonths ?? 0);
    this.fDurationDays.set(detail.durationDays ?? 0);
    this.fDateFrom.set((detail.estimatedDateFrom ?? '').substring(0, 10));
    this.fDateTo.set((detail.estimatedDateTo ?? '').substring(0, 10));
    this.fFundingSourceName.set(detail.fundingSourceName ?? '');
    this.fFundingSourceVoteCode.set(detail.fundingSourceVoteCode ?? '');
    this.fCourseCost.set(detail.courseCost ?? null);
    const ids = (detail.nominations ?? [])
      .map(n => n.employeeId)
      .filter((v): v is string => !!v);
    this.fNomineeIds.set(ids);
    if (detail.unitId) this.currentUnitId.set(detail.unitId);

  }

  private async refreshPreview(): Promise<void> {
    // Skip until we have at least the inputs the calculator needs.
    if (this.fDurationDays() <= 0 || this.fNomineeIds().length === 0) {
      this.preview.set(null);
      return;
    }
    const input: CalculatePreviewInput = {
      courseType: this.fCourseType(),
      durationDays: this.fDurationDays(),
      nomineeEmployeeIds: this.fNomineeIds(),
      courseCost: this.fCourseCost(),
    };
    this.previewLoading.set(true);
    try {
      const result = await firstValueFrom(this.service.calculatePreview(input));
      this.preview.set(result);
    } catch {
      // Non-fatal — calculator is informational only.
      this.preview.set(null);
    } finally {
      this.previewLoading.set(false);
    }
  }

  onTenantCourseChange(value: string): void {
    this.fTenantCourseId.set(value);
    const tc = this.tenantCourses().find(t => t.id === value);
    if (tc && !this.fDescriptionAr()) {
      this.fDescriptionAr.set(tc.catalogCourseNameAr ?? '');
    }
  }

  onCourseTypeChange(value: string): void {
    this.fCourseType.set(+value as CourseType);
  }

  selectCourseType(type: CourseType): void {
    this.fCourseType.set(type);
  }

  onCourseCostInput(raw: string): void {
    const trimmed = raw.trim();
    if (trimmed === '') {
      this.fCourseCost.set(null);
      return;
    }
    const n = +trimmed;
    if (!Number.isNaN(n) && n >= 0) this.fCourseCost.set(n);
  }

  onNomineesChange(ids: string[]): void {
    // Picker selection is local-only — no autosave. The Save Draft / Submit
    // buttons in the header bar persist whenever the user is ready. The
    // calculator preview still reflects the latest selection because the
    // preview effect tracks fNomineeIds.
    this.fNomineeIds.set(ids);
  }

  /** When embedded inside the new-course shell, swap /new → /:id silently and
   *  let the shell load the freshly-created course. Component instance is
   *  preserved so the form state survives the URL change. */
  private attachToShell(newId: string): void {
    if (!this.embedded()) return;
    if (typeof window !== 'undefined' && window.location.pathname.endsWith('/new')) {
      const path = `/training/casual-courses/${newId}${window.location.search}${window.location.hash}`;
      window.history.replaceState(null, '', path);
    }
    this.refreshShell.attachNewCourse(newId);
  }

  private buildDto(): CreateUpdateCasualCourseDto {
    return {
      tenantCourseId: this.fTenantCourseId(),
      unitId: this.currentUnitId(),
      courseType: this.fCourseType(),
      priority: this.fPriority(),
      justification: this.fJustification().trim() || 'مسودة',
      descriptionAr: this.fDescriptionAr().trim() || undefined,
      objectivesAr: this.fObjectivesAr().trim() || undefined,
      durationYears: this.fDurationYears(),
      durationMonths: this.fDurationMonths(),
      durationDays: this.fDurationDays(),
      estimatedDateFrom: this.fDateFrom(),
      estimatedDateTo: this.fDateTo(),
      fundingSourceName: this.fFundingSourceName().trim() || undefined,
      fundingSourceVoteCode: this.fFundingSourceVoteCode().trim() || undefined,
      courseCost: this.fCourseCost(),
      nomineeEmployeeIds: this.fNomineeIds(),
    };
  }

  // ── Calculator preview (read-only) ───────────────────────────────

  isExpanded(id: string | undefined): boolean {
    return !!id && this.expandedItemId() === id;
  }

  toggleExpand(id: string | undefined): void {
    if (!id) return;
    this.expandedItemId.update(cur => (cur === id ? null : id));
  }

  effectiveDaysExplainer(item: { isPerDay: boolean; effectiveDays: number }): string {
    if (!item.isPerDay) return 'ليس لكل يوم';
    return `${item.effectiveDays} يوم فعّال`;
  }

  openNotesDrawer(): void {
    this.notesOpen.set(true);
  }

  closeNotesDrawer(): void {
    this.notesOpen.set(false);
  }

  async onSave(isDraft: boolean): Promise<void> {
    if (this.saving() || !this.canSaveDraft() || (!isDraft && !this.canSubmit())) return;
    this.submitError.set(null);
    this.saving.set(true);
    const dto = this.buildDto();

    try {
      const wasReturned = this.isReturned();
      const wasNew = !this.id();
      let currentId = this.id();
      if (currentId) {
        await firstValueFrom(this.service.update(currentId, dto));
      } else {
        const saved = await firstValueFrom(this.service.create(dto));
        currentId = saved.id ?? null;
        if (currentId) this.id.set(currentId);
      }

      if (!isDraft && currentId) {
        if (wasReturned) {
          await firstValueFrom(this.service.resubmit(currentId));
        } else {
          await firstValueFrom(this.service.submit(currentId));
        }
      }

      // Embedded inside the shell:
      //   Save Draft → stay on the page; attach the new course to the shell
      //                so URL switches /new → /:id without re-instantiation.
      //   Submit     → workflow done, return to the list as before.
      if (this.embedded()) {
        if (isDraft && currentId && wasNew) {
          this.attachToShell(currentId);
          await this.loadExisting(currentId);
          return;
        }
        if (isDraft && currentId) {
          await this.loadExisting(currentId);
          this.refreshShell.refresh();
          return;
        }
      }

      this.router.navigate(['/training/casual-courses']);
    } catch (e: unknown) {
      this.submitError.set(this.mapError(e));
    } finally {
      this.saving.set(false);
    }
  }

  onCancel(): void {
    this.router.navigate(['/training/casual-courses']);
  }

  private mapError(e: unknown): string {
    const anyErr = e as { error?: { error?: { code?: string; message?: string } }; message?: string };
    const code = anyErr?.error?.error?.code;
    if (code) {
      const localized = this.l.t(`::${code}`);
      if (localized && localized !== `::${code}`) return localized;
    }
    return anyErr?.error?.error?.message ?? anyErr?.message ?? 'حدث خطأ غير متوقع';
  }
}
