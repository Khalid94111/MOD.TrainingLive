import { Component, OnInit, computed, inject, signal } from '@angular/core';
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
  AssignmentLineDto,
  AssignmentRankLineDto,
  AssignScenarioDto,
  CasualCourseDetailDto,
  CasualCourseFinancialDto,
  CasualCourseFinancialItemRankDto,
  CasualCourseNominationDto,
} from 'src/app/proxy/training/casual-courses/dtos/models';
import { FinancialItemService } from 'src/app/proxy/training/finance/financial-item.service';
import type { FinancialItemDto } from 'src/app/proxy/training/finance/dtos/models';

import {
  CasualCourseStatus,
  FinancialAmountSource,
  FundingScenario,
  FUNDING_SCENARIO_OPTIONS,
  TrainingLocalizationHelper,
} from '../../shared';
import { NotesDrawerComponent } from '../../shared/components/notes-drawer/notes-drawer.component';
import { ReturnModalComponent } from '../../shared/components/return-modal/return-modal.component';
import { FinancialItemType } from 'src/app/proxy/training/enums/financial-item-type.enum';
import { PlanNoteEntityType } from 'src/app/proxy/training/enums/plan-note-entity-type.enum';

interface RateEdit {
  rankRowId: string;
  newRate: number;
}

@Component({
  standalone: true,
  selector: 'app-casual-course-review',
  templateUrl: './casual-course-review.component.html',
  styleUrls: ['./casual-course-review.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, RouterLink, NotesDrawerComponent, ReturnModalComponent],
})
export class CasualCourseReviewComponent implements OnInit {
  private service = inject(CasualCourseService);
  private financialService = inject(CasualCourseFinancialService);
  private rankService = inject(CasualCourseFinancialItemRankService);
  private financialItemService = inject(FinancialItemService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  l = inject(TrainingLocalizationHelper);

  CasualCourseStatus = CasualCourseStatus;
  FundingScenario = FundingScenario;
  FinancialAmountSource = FinancialAmountSource;
  PlanNoteEntityType = PlanNoteEntityType;
  SCENARIO_OPTIONS = FUNDING_SCENARIO_OPTIONS;

  courseId = signal<string>('');
  casualCourse = signal<CasualCourseDetailDto | null>(null);
  financials = signal<CasualCourseFinancialDto[]>([]);
  financialItems = signal<FinancialItemDto[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  actionBusy = signal(false);

  fScenario = signal<FundingScenario | null>(null);
  expandedItemId = signal<string | null>(null);

  notesOpen = signal(false);
  returnModalOpen = signal(false);
  returnTargetType = signal<PlanNoteEntityType>(PlanNoteEntityType.CasualCourse);
  returnTargetId = signal<string>('');

  rejectOpen = signal(false);
  rejectReason = signal('');
  rejectSubmitting = signal(false);

  addItemSelectOpen = signal(false);
  addItemId = signal<string>('');

  // Debounced per-rank rate edits. keyed by rank-row id to avoid cross-talk.
  private rateEdits$ = new Subject<RateEdit>();

  isReviewable = computed(() => this.casualCourse()?.status === CasualCourseStatus.UnderReview);
  canStartReview = computed(() => this.casualCourse()?.status === CasualCourseStatus.UGMApproved);
  isReadOnly = computed(() => {
    const s = this.casualCourse()?.status;
    return s !== undefined && s !== CasualCourseStatus.UnderReview && s !== CasualCourseStatus.UGMApproved;
  });

  grandTotal = computed(() =>
    this.financials().reduce((sum, f) => sum + (f.estimatedAmountOMR ?? 0), 0),
  );

  totalItems = computed(() => this.financials().length);
  itemsMissingCost = computed(() =>
    this.financials().filter(f => (f.estimatedAmountOMR ?? 0) <= 0).length,
  );
  itemsComplete = computed(() => this.totalItems() - this.itemsMissingCost());
  progressPct = computed(() => {
    const t = this.totalItems();
    if (t === 0) return 0;
    return Math.round((this.itemsComplete() / t) * 100);
  });

  nominations = computed(() => this.casualCourse()?.nominations ?? []);

  constructor() {
    this.rateEdits$
      .pipe(
        groupBy(e => e.rankRowId),
        mergeMap(group => group.pipe(debounceTime(400))),
      )
      .subscribe(edit => this.flushRateEdit(edit));
  }

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.courseId.set(id);
    await this.loadAll();
  }

  private async loadAll(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const [detail, financials, items] = await Promise.all([
        firstValueFrom(this.service.getDetail(this.courseId())),
        firstValueFrom(this.financialService.getListByCasualCourse(this.courseId())),
        firstValueFrom(this.financialItemService.getList({ maxResultCount: 500, isActive: true })),
      ]);
      this.casualCourse.set(detail);
      this.financials.set(financials);
      this.financialItems.set(items.items ?? []);
      if (detail.fundingScenario !== null && detail.fundingScenario !== undefined) {
        this.fScenario.set(detail.fundingScenario);
      }
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
    } finally {
      this.loading.set(false);
    }
  }

  // ── Scenario + source derivation ─────────────────────────────────

  // Mirrors backend FundingScenarioSourceResolver. Under scenario 2 a leaf whose
  // itemType is null or non-CourseCost falls to FinancialItem (the safe default).
  sourceFor(financialItemId: string | undefined): FinancialAmountSource {
    const scenario = this.fScenario();
    const isCourseCost = this.isCourseCostItem(financialItemId);
    switch (scenario) {
      case FundingScenario.FundingSourceCoversAll:
        return FinancialAmountSource.FundingSource;
      case FundingScenario.FundingSourceCoversCourse:
        return isCourseCost ? FinancialAmountSource.FundingSource : FinancialAmountSource.FinancialItem;
      case FundingScenario.FinancialItemsCoverAll:
        return FinancialAmountSource.FinancialItem;
      case null:
      case undefined:
        return FinancialAmountSource.FundingSource;
    }
  }

  sourceLabel(src: FinancialAmountSource): string {
    return src === FinancialAmountSource.FundingSource ? 'الجهة الممولة' : 'بند مالي';
  }

  private isCourseCostItem(financialItemId: string | undefined): boolean {
    if (!financialItemId) return false;
    const item = this.financialItems().find(i => i.id === financialItemId);
    return item?.itemType === FinancialItemType.CourseCost;
  }

  onScenarioSelect(scenario: FundingScenario): void {
    if (this.isReadOnly()) return;
    this.fScenario.set(scenario);
  }

  scenarioNum(s: FundingScenario): number {
    return s;
  }

  // ── Accordion control ────────────────────────────────────────────

  isExpanded(id: string | undefined): boolean {
    return !!id && this.expandedItemId() === id;
  }

  toggleExpand(id: string | undefined): void {
    if (!id) return;
    this.expandedItemId.update(cur => (cur === id ? null : id));
  }

  /** Flat items are rendered without accordion affordance. */
  isFlatItem(fin: CasualCourseFinancialDto): boolean {
    return !fin.isPerNominee;
  }

  financialItemName(id: string | undefined): string {
    if (!id) return '—';
    return this.financialItems().find(i => i.id === id)?.nameAr ?? '—';
  }

  effectiveDaysExplainer(fin: CasualCourseFinancialDto): string {
    if (!fin.isPerDay) return 'ليس لكل يوم';
    const days = this.casualCourse()?.durationDays ?? 0;
    const before = fin.extraDaysBefore ?? 0;
    const after = fin.extraDaysAfter ?? 0;
    return `${days} + ${before} + ${after} = ${fin.effectiveDays ?? days + before + after}`;
  }

  rateSourceLabel(source: string | undefined): string {
    if (source === 'RankOverride') return 'معدل الرتبة';
    if (source === 'DefaultAmount') return 'افتراضي';
    return source ?? '—';
  }

  // ── Per-rank inline rate edit ────────────────────────────────────

  onRateInput(rankRowId: string | undefined, raw: string): void {
    if (!rankRowId || !this.isReviewable()) return;
    const newRate = +raw;
    if (Number.isNaN(newRate) || newRate < 0) return;

    // Optimistic: update local signal immediately so the subtotal re-renders.
    this.financials.update(list =>
      list.map(f => {
        if (!f.ranks) return f;
        const match = f.ranks.find(r => r.id === rankRowId);
        if (!match) return f;
        const duration = this.casualCourse()?.durationDays ?? 0;
        const effDays = f.effectiveDays ?? (f.isPerDay ? duration : 1);
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

  private async flushRateEdit(edit: RateEdit): Promise<void> {
    try {
      const updated = await firstValueFrom(
        this.rankService.updateRate(edit.rankRowId, { ratePerUnitOMR: edit.newRate }),
      );
      // Reconcile with server response (rates may have been clamped, source chips may have shifted).
      this.financials.update(list => list.map(f => (f.id === updated.id ? updated : f)));
    } catch (e: unknown) {
      // Rollback by refetching the full list.
      this.error.set(this.mapError(e));
      await this.refreshFinancials();
    }
  }

  private async refreshFinancials(): Promise<void> {
    try {
      const financials = await firstValueFrom(
        this.financialService.getListByCasualCourse(this.courseId()),
      );
      this.financials.set(financials);
    } catch {
      /* ignore */
    }
  }

  // ── Auto-fill + add + delete ─────────────────────────────────────

  async onAutoFill(): Promise<void> {
    if (this.actionBusy() || !this.isReviewable()) return;
    const scenario = this.fScenario();
    if (scenario === null) return;  // button should already be disabled; belt-and-braces
    this.actionBusy.set(true);
    this.error.set(null);
    try {
      const items = await firstValueFrom(
        this.financialService.autoFillFromDefaults(this.courseId(), scenario),
      );
      this.financials.set(items);
      // Scenario was persisted server-side as a side effect — reflect it on the detail view.
      this.casualCourse.update(cc => (cc ? { ...cc, fundingScenario: scenario } : cc));
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
    } finally {
      this.actionBusy.set(false);
    }
  }

  async onDeleteLine(fin: CasualCourseFinancialDto): Promise<void> {
    if (!fin.id || this.actionBusy()) return;
    if (!confirm('هل تريد حذف هذا البند؟ سيتم حذف تفاصيل الرتب المرتبطة به.')) return;
    this.actionBusy.set(true);
    try {
      await firstValueFrom(this.financialService.deleteItem(fin.id));
      this.financials.update(list => list.filter(f => f.id !== fin.id));
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
    } finally {
      this.actionBusy.set(false);
    }
  }

  toggleAddItem(): void {
    this.addItemSelectOpen.update(v => !v);
    this.addItemId.set('');
  }

  async onConfirmAddItem(): Promise<void> {
    if (!this.addItemId() || this.actionBusy()) return;
    this.actionBusy.set(true);
    try {
      await firstValueFrom(
        this.financialService.addItem(this.courseId(), {
          financialItemId: this.addItemId(),
        }),
      );
      await this.refreshFinancials();
      this.toggleAddItem();
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
    } finally {
      this.actionBusy.set(false);
    }
  }

  // ── Jump-to-next-incomplete ──────────────────────────────────────

  jumpToNextIncomplete(): void {
    const target = this.financials().find(f => (f.estimatedAmountOMR ?? 0) <= 0);
    if (target?.id) {
      this.expandedItemId.set(target.id);
      setTimeout(() => {
        document.getElementById(`fin-${target.id}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }, 0);
    }
  }

  // ── Save / Finalize ──────────────────────────────────────────────

  async onStartReview(): Promise<void> {
    if (!this.canStartReview() || this.actionBusy()) return;
    this.actionBusy.set(true);
    try {
      await firstValueFrom(this.service.startReview(this.courseId()));
      await this.loadAll();
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
    } finally {
      this.actionBusy.set(false);
    }
  }

  async onSaveAssignments(commit: boolean): Promise<void> {
    if (!this.isReviewable() || this.actionBusy()) return;
    const scenario = this.fScenario();
    if (scenario === null) {
      this.error.set('يجب اختيار سيناريو تمويل قبل الحفظ.');
      return;
    }
    this.actionBusy.set(true);
    this.error.set(null);
    try {
      const lines: AssignmentLineDto[] = this.financials().map(f => ({
        id: f.id,
        financialItemId: f.financialItemId ?? '',
        notes: f.notes ?? undefined,
        ranks: (f.ranks ?? []).map<AssignmentRankLineDto>(r => ({
          id: r.id,
          rankId: r.rankId ?? '00000000-0000-0000-0000-000000000000',
          nomineeCount: r.nomineeCount ?? 0,
          ratePerUnitOMR: r.ratePerUnitOMR ?? 0,
        })),
      }));
      const body: AssignScenarioDto = {
        fundingScenario: scenario,
        financialItems: lines,
        commit,
      };
      await firstValueFrom(this.service.assignScenario(this.courseId(), body));
      if (commit) {
        this.router.navigate(['/training/casual-courses']);
      } else {
        await this.loadAll();
      }
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
    } finally {
      this.actionBusy.set(false);
    }
  }

  // ── Return / Reject / Notes (unchanged) ──────────────────────────

  openReturnCourse(): void {
    this.returnTargetType.set(PlanNoteEntityType.CasualCourse);
    this.returnTargetId.set(this.courseId());
    this.returnModalOpen.set(true);
  }

  openReturnNomination(nomination: CasualCourseNominationDto): void {
    if (!nomination.id) return;
    this.returnTargetType.set(PlanNoteEntityType.CasualCourseNomination);
    this.returnTargetId.set(nomination.id);
    this.returnModalOpen.set(true);
  }

  onReturnCancelled(): void {
    this.returnModalOpen.set(false);
  }

  async onReturnConfirmed(): Promise<void> {
    this.returnModalOpen.set(false);
    await this.loadAll();
    if (this.returnTargetType() === PlanNoteEntityType.CasualCourse) {
      this.router.navigate(['/training/casual-courses']);
    }
  }

  openReject(): void {
    this.rejectReason.set('');
    this.rejectOpen.set(true);
  }

  closeReject(): void {
    this.rejectOpen.set(false);
  }

  async onConfirmReject(): Promise<void> {
    const reason = this.rejectReason().trim();
    if (reason.length < 10 || this.rejectSubmitting()) return;
    this.rejectSubmitting.set(true);
    try {
      await firstValueFrom(this.service.reject(this.courseId(), { reason }));
      this.rejectOpen.set(false);
      this.router.navigate(['/training/casual-courses']);
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
    } finally {
      this.rejectSubmitting.set(false);
    }
  }

  openNotesDrawer(): void {
    this.notesOpen.set(true);
  }
  closeNotesDrawer(): void {
    this.notesOpen.set(false);
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
