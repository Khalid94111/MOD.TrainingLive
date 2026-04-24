import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, firstValueFrom } from 'rxjs';
import { debounceTime, groupBy, mergeMap } from 'rxjs/operators';

import {
  CasualCourseService,
  CasualCourseFinancialService,
  CasualCourseFinancialItemRankService,
} from 'src/app/proxy/training/casual-courses';
import type {
  CasualCourseDetailDto,
  CasualCourseFinancialDto,
  CreateUpdateCasualCourseDto,
} from 'src/app/proxy/training/casual-courses/dtos/models';
import { TenantCourseService } from 'src/app/proxy/training/tenant-courses/tenant-course.service';
import type { TenantCourseDto } from 'src/app/proxy/training/tenant-courses/dtos/models';
import { HrLookupService } from 'src/app/proxy/training/hr-integration/hr-lookup.service';

import { CasualCourseStatus, CourseType, TrainingLocalizationHelper } from '../../shared';
import { NominationPickerComponent } from '../../shared/components/nomination-picker/nomination-picker.component';
import { NotesDrawerComponent } from '../../shared/components/notes-drawer/notes-drawer.component';
import { PlanNoteEntityType } from 'src/app/proxy/training/enums/plan-note-entity-type.enum';

interface RateEdit {
  rankRowId: string;
  newRate: number;
}

@Component({
  standalone: true,
  selector: 'app-casual-course-request',
  templateUrl: './casual-course-request.component.html',
  styleUrls: ['./casual-course-request.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, RouterLink, NominationPickerComponent, NotesDrawerComponent],
})
export class CasualCourseRequestComponent implements OnInit {
  private service = inject(CasualCourseService);
  private financialService = inject(CasualCourseFinancialService);
  private rankService = inject(CasualCourseFinancialItemRankService);
  private tenantCourseService = inject(TenantCourseService);
  private hrService = inject(HrLookupService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  l = inject(TrainingLocalizationHelper);

  CourseType = CourseType;
  CasualCourseStatus = CasualCourseStatus;
  PlanNoteEntityType = PlanNoteEntityType;

  id = signal<string | null>(null);
  casualCourse = signal<CasualCourseDetailDto | null>(null);
  financials = signal<CasualCourseFinancialDto[]>([]);
  loading = signal(false);
  autosaving = signal(false);
  submitError = signal<string | null>(null);
  notesOpen = signal(false);
  expandedItemId = signal<string | null>(null);

  tenantCourses = signal<TenantCourseDto[]>([]);
  currentUnitId = signal<string>('');

  fTenantCourseId = signal<string>('');
  fCourseType = signal<CourseType>(CourseType.Internal);
  fPriority = signal<number>(3);
  fJustification = signal<string>('');
  fDescriptionAr = signal<string>('');
  fObjectivesAr = signal<string>('');
  fDurationYears = signal<number>(0);
  fDurationMonths = signal<number>(0);
  fDurationDays = signal<number>(0);
  fDateFrom = signal<string>('');
  fDateTo = signal<string>('');
  fFundingSource = signal<string>('');
  fNomineeIds = signal<string[]>([]);

  // Debounced per-rank rate edits keyed by rank-row id to avoid cross-talk.
  private rateEdits$ = new Subject<RateEdit>();

  isEdit = computed(() => !!this.id());
  isReturned = computed(() => this.casualCourse()?.status === CasualCourseStatus.ReturnedToCreator);
  showFundingSource = computed(() => this.fCourseType() !== CourseType.Internal);
  nomineeCount = computed(() => this.fNomineeIds().length);
  hasFinancials = computed(() => this.financials().length > 0);
  grandTotal = computed(() =>
    this.financials().reduce((sum, f) => sum + (f.estimatedAmountOMR ?? 0), 0),
  );
  hasAnyFinancialValue = computed(() => this.grandTotal() > 0);

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
    return (
      !!this.fTenantCourseId() &&
      this.fJustification().trim().length > 0 &&
      !!this.fDateFrom() &&
      !!this.fDateTo() &&
      this.fDurationDays() > 0 &&
      this.fNomineeIds().length > 0 &&
      (this.fCourseType() === CourseType.Internal || this.fFundingSource().trim().length > 0) &&
      this.hasAnyFinancialValue()
    );
  });

  constructor() {
    this.rateEdits$
      .pipe(
        groupBy(e => e.rankRowId),
        mergeMap(group => group.pipe(debounceTime(400))),
      )
      .subscribe(edit => this.flushRateEdit(edit));

    effect(() => {
      const next = this.computedDateTo();
      if (this.fDateTo() !== next) {
        this.fDateTo.set(next);
      }
    }, { allowSignalWrites: true });
  }

  async ngOnInit(): Promise<void> {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.id.set(idParam);
    this.loading.set(true);
    try {
      await Promise.all([this.loadCurrentEmployee(), this.loadTenantCourses()]);
      if (idParam) {
        await this.loadExisting(idParam);
      }
    } finally {
      this.loading.set(false);
    }
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
    this.fFundingSource.set(detail.fundingSource ?? '');
    const ids = (detail.nominations ?? [])
      .map(n => n.employeeId)
      .filter((v): v is string => !!v);
    this.fNomineeIds.set(ids);
    if (detail.unitId) this.currentUnitId.set(detail.unitId);

    await this.refreshFinancials();
  }

  private async refreshFinancials(): Promise<void> {
    const id = this.id();
    if (!id) { this.financials.set([]); return; }
    try {
      const rows = await firstValueFrom(this.financialService.getListByCasualCourse(id));
      this.financials.set(rows);
    } catch {
      /* non-fatal — shows empty panel */
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

  async onNomineesChange(ids: string[]): Promise<void> {
    const prevCount = this.fNomineeIds().length;
    this.fNomineeIds.set(ids);

    // Option P — first nominee add triggers silent autosave so financial panel appears.
    // On subsequent changes (while id is set), persist the nominee diff so the backend
    // recomputes rank rows.
    if (!this.id() && ids.length > 0) {
      await this.ensureDraftSaved();
    } else if (this.id() && (ids.length !== prevCount || !this.sameIds(ids))) {
      await this.persistUpdateAndRefresh();
    }
  }

  private sameIds(next: string[]): boolean {
    const prev = this.fNomineeIds();
    if (prev.length !== next.length) return false;
    const setA = new Set(prev);
    return next.every(id => setA.has(id));
  }

  private async ensureDraftSaved(): Promise<void> {
    if (this.id() || this.autosaving()) return;
    const dto = this.buildDto();
    this.autosaving.set(true);
    this.submitError.set(null);
    try {
      const saved = await firstValueFrom(this.service.create(dto));
      if (saved.id) {
        this.id.set(saved.id);
        await this.loadExisting(saved.id);
      }
    } catch (e: unknown) {
      this.submitError.set(this.mapError(e));
    } finally {
      this.autosaving.set(false);
    }
  }

  private async persistUpdateAndRefresh(): Promise<void> {
    const id = this.id();
    if (!id) return;
    const dto = this.buildDto();
    try {
      await firstValueFrom(this.service.update(id, dto));
      await this.refreshFinancials();
    } catch (e: unknown) {
      this.submitError.set(this.mapError(e));
    }
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
      fundingSource: this.fFundingSource().trim() || undefined,
      nomineeEmployeeIds: this.fNomineeIds(),
    };
  }

  // ── Financial items panel ────────────────────────────────────────

  isFlatItem(fin: CasualCourseFinancialDto): boolean {
    return !fin.isPerNominee;
  }

  isExpanded(id: string | undefined): boolean {
    return !!id && this.expandedItemId() === id;
  }

  toggleExpand(id: string | undefined): void {
    if (!id) return;
    this.expandedItemId.update(cur => (cur === id ? null : id));
  }

  effectiveDaysExplainer(fin: CasualCourseFinancialDto): string {
    if (!fin.isPerDay) return 'ليس لكل يوم';
    const days = this.fDurationDays();
    const before = fin.extraDaysBefore ?? 0;
    const after = fin.extraDaysAfter ?? 0;
    return `${days} + ${before} + ${after} = ${fin.effectiveDays ?? days + before + after}`;
  }

  rateSourceLabel(source: string | undefined): string {
    if (source === 'RankOverride') return 'معدل الرتبة';
    if (source === 'DefaultAmount') return 'افتراضي';
    return source ?? '—';
  }

  onRateInput(rankRowId: string | undefined, raw: string): void {
    if (!rankRowId || this.isReadOnlyFinancials()) return;
    const newRate = +raw;
    if (Number.isNaN(newRate) || newRate < 0) return;

    // Optimistic update so the subtotal + grand total re-render immediately.
    this.financials.update(list =>
      list.map(f => {
        if (!f.ranks) return f;
        const match = f.ranks.find(r => r.id === rankRowId);
        if (!match) return f;
        const effDays = f.effectiveDays ?? (f.isPerDay ? this.fDurationDays() : 1);
        const newSubtotal = newRate * effDays * (match.nomineeCount ?? 0);
        const newRanks = f.ranks.map(r =>
          r.id === rankRowId ? { ...r, ratePerUnitOMR: newRate, subtotalOMR: newSubtotal } : r,
        );
        const newTotal = newRanks.reduce((s, r) => s + (r.subtotalOMR ?? 0), 0);
        return { ...f, ranks: newRanks, estimatedAmountOMR: newTotal };
      }),
    );

    this.rateEdits$.next({ rankRowId, newRate });
  }

  /** UTM can edit rates only while the course is still theirs (Draft or ReturnedToCreator). */
  isReadOnlyFinancials(): boolean {
    const s = this.casualCourse()?.status;
    return s !== undefined &&
      s !== CasualCourseStatus.Draft &&
      s !== CasualCourseStatus.ReturnedToCreator;
  }

  private async flushRateEdit(edit: RateEdit): Promise<void> {
    try {
      const updated = await firstValueFrom(
        this.rankService.updateRate(edit.rankRowId, { ratePerUnitOMR: edit.newRate }),
      );
      this.financials.update(list => list.map(f => (f.id === updated.id ? updated : f)));
    } catch (e: unknown) {
      this.submitError.set(this.mapError(e));
      await this.refreshFinancials();
    }
  }

  openNotesDrawer(): void {
    this.notesOpen.set(true);
  }

  closeNotesDrawer(): void {
    this.notesOpen.set(false);
  }

  async onSave(isDraft: boolean): Promise<void> {
    if (!isDraft && !this.canSubmit()) return;
    this.submitError.set(null);
    const dto = this.buildDto();

    try {
      const wasReturned = this.isReturned();
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
      this.router.navigate(['/training/casual-courses']);
    } catch (e: unknown) {
      this.submitError.set(this.mapError(e));
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
