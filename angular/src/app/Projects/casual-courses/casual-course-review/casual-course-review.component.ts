import { Component, OnInit, computed, inject, input, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, firstValueFrom } from 'rxjs';
import { debounceTime, groupBy, mergeMap } from 'rxjs/operators';

import {
  CasualCourseService,
  CasualCourseFinancialItemService,
  CasualCourseFinancialItemRankService,
} from 'src/app/proxy/training/casual-courses';
import type {
  AssignScenarioDto,
  CasualCourseDetailDto,
  CasualCourseFinancialItemDto,
  CasualCourseNominationDto,
  StaffAdjustmentDto,
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
import { CasualCourseActionService } from '../casual-course-detail/casual-course-action.service';
import { CasualCourseDetailRefreshService } from '../casual-course-detail/casual-course-detail-refresh.service';

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
  private financialService = inject(CasualCourseFinancialItemService);
  private rankService = inject(CasualCourseFinancialItemRankService);
  private financialItemService = inject(FinancialItemService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private actions = inject(CasualCourseActionService);
  private refreshShell = inject(CasualCourseDetailRefreshService);
  private destroyRef = inject(DestroyRef);
  l = inject(TrainingLocalizationHelper);

  CasualCourseStatus = CasualCourseStatus;
  FundingScenario = FundingScenario;
  FinancialAmountSource = FinancialAmountSource;
  PlanNoteEntityType = PlanNoteEntityType;
  SCENARIO_OPTIONS = FUNDING_SCENARIO_OPTIONS;

  /** Render slot — see CasualCourseRequestComponent for semantics. */
  mode = input<'details' | 'financials' | 'all'>('all');
  showDetails    = computed(() => this.mode() === 'details'    || this.mode() === 'all');
  showFinancials = computed(() => this.mode() === 'financials' || this.mode() === 'all');

  courseId = signal<string>('');
  embedded = signal<boolean>(false);
  casualCourse = signal<CasualCourseDetailDto | null>(null);
  financials = signal<CasualCourseFinancialItemDto[]>([]);
  financialItems = signal<FinancialItemDto[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  actionBusy = signal(false);

  fScenario = signal<FundingScenario | null>(null);
  expandedItemId = signal<string | null>(null);

  // Patch 4 — Staff adjustments collected between the last server sync and the next
  // Save/Finalize click. Keyed by rank-row id so a second edit on the same row just
  // overwrites the previous pending value.
  private pendingAdjustments = new Map<string, StaffAdjustmentDto>();

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
    this.embedded.set(!!this.route.snapshot.data['embedded']);
    await this.loadAll();

    // Header action bar dispatch — Staff actions during review. The review
    // variant's default-active section is Section 2 (mode='financials') —
    // Section 1 is collapsed for UGMApproved+/UnderReview/etc. Gate on
    // mode='financials' so the sole live instance handles the action;
    // 'details' guard would drop the event because Section 1 is collapsed.
    this.actions.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(action => {
      if (!this.embedded() || this.mode() !== 'financials') return;
      if (action === 'startReview' && this.canStartReview()) {
        void this.onStartReview();
      } else if (this.isReviewable()) {
        if (action === 'saveProgress')        void this.onSaveAssignments(false);
        else if (action === 'finalizeReview') void this.onSaveAssignments(true);
        else if (action === 'return')         this.openReturnCourse();
        else if (action === 'reject')         this.openReject();
      }
    });
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

  async onScenarioSelect(scenario: FundingScenario): Promise<void> {
    if (this.isReadOnly()) return;
    if (this.fScenario() === scenario) return;
    this.fScenario.set(scenario);

    if (!this.isReviewable()) return;
    // Patch 4 — the picker persists immediately so Source chips on the rows reflect
    // the chosen scenario. Any in-flight adjustments go along for the ride.
    this.actionBusy.set(true);
    this.error.set(null);
    try {
      const adjustments = Array.from(this.pendingAdjustments.values());
      await firstValueFrom(
        this.service.assignScenario(this.courseId(), {
          fundingScenario: scenario,
          adjustments,
          commit: false,
        }),
      );
      this.pendingAdjustments.clear();
      // Reload to pick up new Source values + recomputed totals.
      await this.loadAll();
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
      // Roll back the local pick if the server refused it (e.g. status changed).
      await this.loadAll();
    } finally {
      this.actionBusy.set(false);
    }
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
  isFlatItem(fin: CasualCourseFinancialItemDto): boolean {
    return !fin.isPerNominee;
  }

  financialItemName(id: string | undefined): string {
    if (!id) return '—';
    return this.financialItems().find(i => i.id === id)?.nameAr ?? '—';
  }

  effectiveDaysExplainer(fin: CasualCourseFinancialItemDto): string {
    if (!fin.isPerDay) return 'ليس لكل يوم';
    const days = this.casualCourse()?.durationDays ?? 0;
    const before = fin.extraDaysBefore ?? 0;
    const after = fin.extraDaysAfter ?? 0;
    return `${days} + ${before} + ${after} = ${fin.effectiveDays ?? days + before + after}`;
  }

  rateSourceLabel(source: string | undefined): string {
    if (source === 'RankOverride') return 'معدل الرتبة';
    if (source === 'DefaultAmount') return 'افتراضي';
    if (source === 'FromUTMForm') return 'من UTM';
    return source ?? '—';
  }

  isFromUtmForm(source: string | undefined): boolean {
    return source === 'FromUTMForm';
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

    // Patch 4 — rate edits land in pendingAdjustments and flush via Save/Finalize
    // (or inline via rankService.updateRate for immediate persistence).
    this.pendingAdjustments.set(rankRowId, {
      casualCourseFinancialItemRankId: rankRowId,
      newRatePerUnitOMR: newRate,
    });
    this.rateEdits$.next({ rankRowId, newRate });
  }

  private async flushRateEdit(edit: RateEdit): Promise<void> {
    try {
      const updated = await firstValueFrom(
        this.rankService.updateRate(edit.rankRowId, { ratePerUnitOMR: edit.newRate }),
      );
      // Server accepted; drop from pending set so Save/Finalize doesn't double-apply.
      this.pendingAdjustments.delete(edit.rankRowId);
      this.financials.update(list => list.map(f => (f.id === updated.id ? updated : f)));
    } catch (e: unknown) {
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

  // ── Add + delete ─────────────────────────────────────────────────

  async onDeleteLine(fin: CasualCourseFinancialItemDto): Promise<void> {
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
      // Shell's course() drives the header action bar — refresh so the
      // buttons recompute for the new status (UGMApproved → UnderReview).
      if (this.embedded()) this.refreshShell.refresh();
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
      // Patch 4 — send scenario + any pending adjustments in one shot.
      // Individual rate edits may already have been persisted via rankService
      // (flushRateEdit), but the pendingAdjustments map is the source of truth
      // for anything still in flight.
      const body: AssignScenarioDto = {
        fundingScenario: scenario,
        adjustments: Array.from(this.pendingAdjustments.values()),
        commit,
      };
      await firstValueFrom(this.service.assignScenario(this.courseId(), body));
      this.pendingAdjustments.clear();
      if (commit) {
        this.router.navigate(['/training/casual-courses']);
      } else {
        await this.loadAll();
        // Save Progress keeps the user on the page — refresh the shell
        // so the financial-items count / total reflects new edits.
        if (this.embedded()) this.refreshShell.refresh();
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
