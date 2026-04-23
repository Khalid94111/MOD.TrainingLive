import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { CasualCourseService, CasualCourseFinancialService } from 'src/app/proxy/training/casual-courses';
import type {
  AssignmentLineDto,
  AssignScenarioDto,
  CasualCourseDetailDto,
  CasualCourseFinancialDto,
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
import { PlanNoteEntityType } from 'src/app/proxy/training/enums/plan-note-entity-type.enum';

// Travel-item codes — keep in lockstep with backend CasualCourseFinancialManager.TRAVEL_CODES.
// If either side changes, update both in the same commit (Risk #2 of Phase 4A Frontend Prompt).
const TRAVEL_ITEM_CODES = new Set(['TRAVEL_ALLOWANCE', 'ACCOMMODATION', 'TRANSPORT']);

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

  notesOpen = signal(false);
  returnModalOpen = signal(false);
  returnTargetType = signal<PlanNoteEntityType>(PlanNoteEntityType.CasualCourse);
  returnTargetId = signal<string>('');

  rejectOpen = signal(false);
  rejectReason = signal('');
  rejectSubmitting = signal(false);

  addItemSelectOpen = signal(false);
  addItemId = signal<string>('');
  addItemAmount = signal<number>(0);

  isReviewable = computed(() => this.casualCourse()?.status === CasualCourseStatus.UnderReview);
  canStartReview = computed(() => this.casualCourse()?.status === CasualCourseStatus.UGMApproved);
  isReadOnly = computed(() => {
    const s = this.casualCourse()?.status;
    return s !== undefined && s !== CasualCourseStatus.UnderReview && s !== CasualCourseStatus.UGMApproved;
  });

  grandTotal = computed(() =>
    this.financials().reduce((sum, f) => sum + (f.estimatedAmountOMR ?? 0), 0),
  );

  nominations = computed(() => this.casualCourse()?.nominations ?? []);

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

  sourceFor(financialItemId: string | undefined): FinancialAmountSource {
    const scenario = this.fScenario();
    const isTravel = this.isTravelItem(financialItemId);
    switch (scenario) {
      case FundingScenario.FundingSourceCoversAll:
        return FinancialAmountSource.FundingSource;
      case FundingScenario.FundingSourceCoversCourse:
        return isTravel ? FinancialAmountSource.FinancialItem : FinancialAmountSource.FundingSource;
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

  private isTravelItem(financialItemId: string | undefined): boolean {
    if (!financialItemId) return false;
    const item = this.financialItems().find(i => i.id === financialItemId);
    return item?.code ? TRAVEL_ITEM_CODES.has(item.code) : false;
  }

  onScenarioSelect(scenario: FundingScenario): void {
    if (this.isReadOnly()) return;
    this.fScenario.set(scenario);
  }

  onAmountInput(fin: CasualCourseFinancialDto, raw: string): void {
    const value = +raw;
    if (Number.isNaN(value)) return;
    this.financials.update(list =>
      list.map(f => (f.id === fin.id ? { ...f, estimatedAmountOMR: value } : f)),
    );
  }

  onNotesInput(fin: CasualCourseFinancialDto, raw: string): void {
    this.financials.update(list =>
      list.map(f => (f.id === fin.id ? { ...f, notes: raw } : f)),
    );
  }

  async onAutoFill(): Promise<void> {
    if (this.actionBusy() || !this.isReviewable()) return;
    this.actionBusy.set(true);
    this.error.set(null);
    try {
      const items = await firstValueFrom(this.financialService.autoFillFromDefaults(this.courseId()));
      this.financials.set(items);
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
    } finally {
      this.actionBusy.set(false);
    }
  }

  async onDeleteLine(fin: CasualCourseFinancialDto): Promise<void> {
    if (!fin.id || this.actionBusy()) return;
    if (!confirm('هل تريد حذف هذا البند؟')) return;
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
    this.addItemAmount.set(0);
  }

  async onConfirmAddItem(): Promise<void> {
    if (!this.addItemId() || this.actionBusy()) return;
    this.actionBusy.set(true);
    try {
      const line = await firstValueFrom(
        this.financialService.addItem(this.courseId(), {
          financialItemId: this.addItemId(),
          estimatedAmountOMR: this.addItemAmount(),
        }),
      );
      this.financials.update(list => [...list, line]);
      this.toggleAddItem();
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
    } finally {
      this.actionBusy.set(false);
    }
  }

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
        amount: f.estimatedAmountOMR ?? 0,
        notes: f.notes ?? undefined,
      }));
      const body: AssignScenarioDto = {
        fundingScenario: scenario,
        estimatedTotalCost: this.grandTotal(),
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

  scenarioNum(s: FundingScenario): number {
    return s;
  }

  financialItemName(id: string | undefined): string {
    if (!id) return '—';
    return this.financialItems().find(i => i.id === id)?.nameAr ?? '—';
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
