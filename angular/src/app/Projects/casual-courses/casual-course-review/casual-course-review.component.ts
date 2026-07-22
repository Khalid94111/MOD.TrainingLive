import { Component, OnInit, computed, inject, input, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { CasualCourseService } from 'src/app/proxy/training/casual-courses';
import type {
  AssignScenarioDto,
  CasualCourseDetailDto,
  CasualCourseNominationDto,
} from 'src/app/proxy/training/casual-courses/dtos/models';
import { FinancialItemService } from 'src/app/proxy/training/finance/financial-item.service';
import type { FinancialItemDto } from 'src/app/proxy/training/finance/dtos/models';

import {
  CasualCourseStatus,
  CourseType,
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

interface TravelFundingRow {
  itemType: FinancialItemType;
  name: string;
  fundingSource: string;
  voteCode: string;
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
  private financialItemService = inject(FinancialItemService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private actions = inject(CasualCourseActionService);
  private refreshShell = inject(CasualCourseDetailRefreshService);
  private destroyRef = inject(DestroyRef);
  l = inject(TrainingLocalizationHelper);

  CasualCourseStatus = CasualCourseStatus;
  CourseType = CourseType;
  FundingScenario = FundingScenario;
  PlanNoteEntityType = PlanNoteEntityType;
  SCENARIO_OPTIONS = FUNDING_SCENARIO_OPTIONS;

  /** Render slot — see CasualCourseRequestComponent for semantics. */
  mode = input<'details' | 'financials' | 'all'>('all');
  showDetails    = computed(() => this.mode() === 'details'    || this.mode() === 'all');
  showFinancials = computed(() => this.mode() === 'financials' || this.mode() === 'all');

  courseId = signal<string>('');
  embedded = signal<boolean>(false);
  casualCourse = signal<CasualCourseDetailDto | null>(null);
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

  isReviewable = computed(() => this.casualCourse()?.status === CasualCourseStatus.UnderReview);
  canStartReview = computed(() => this.casualCourse()?.status === CasualCourseStatus.UGMApproved);
  isInternational = computed(
    () => this.casualCourse()?.courseType === CourseType.ExternalInternational,
  );
  isLocal = computed(() => this.casualCourse()?.courseType === CourseType.ExternalLocal);
  isReadOnly = computed(() => {
    const s = this.casualCourse()?.status;
    return s !== undefined && s !== CasualCourseStatus.UnderReview && s !== CasualCourseStatus.UGMApproved;
  });

  courseCost = computed(() => this.casualCourse()?.courseCost ?? 0);
  nominations = computed(() => this.casualCourse()?.nominations ?? []);
  travelFundingRows = computed<TravelFundingRow[]>(() => {
    if (!this.isInternational()) return [];

    const scenario = this.fScenario();
    const course = this.casualCourse();
    const useCourseSource = scenario === FundingScenario.FundingSourceCoversAll;
    const specs: Array<{ itemType: FinancialItemType; name: string }> = [
      { itemType: FinancialItemType.Ticket, name: 'تذاكر السفر' },
      { itemType: FinancialItemType.Visa, name: 'التأشيرة' },
      { itemType: FinancialItemType.Insurance, name: 'التأمين الصحي' },
      { itemType: FinancialItemType.Allowance, name: 'العلاوة اليومية' },
      { itemType: FinancialItemType.Clothing, name: 'بدل الملابس' },
    ];

    return specs.map(spec => {
      const configured = [...this.financialItems()]
        .reverse()
        .find(item => item.itemType === spec.itemType && !!item.voteCode?.trim());
      return {
        ...spec,
        fundingSource: scenario === null
          ? 'يُحدد بعد اختيار طريقة التغطية'
          : useCourseSource
            ? course?.fundingSourceName || 'مصدر تمويل الدورة'
            : configured?.nameAr || 'غير مضبوط في إعدادات التدريب',
        voteCode: scenario === null
          ? ''
          : useCourseSource
            ? course?.fundingSourceVoteCode || ''
            : configured?.voteCode || '',
      };
    });
  });
  missingTravelVoteCodes = computed(
    () => this.travelFundingRows().filter(row => !row.voteCode).length,
  );
  canFinalizeReview = computed(() => {
    if (!this.isReviewable() || this.courseCost() <= 0) return false;
    if (this.isLocal()) return true;
    return this.fScenario() !== null && this.missingTravelVoteCodes() === 0;
  });

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
      const detail = await firstValueFrom(this.service.getDetail(this.courseId()));
      this.casualCourse.set(detail);
      if (detail.courseType === CourseType.ExternalInternational) {
        const items = await firstValueFrom(
          this.financialItemService.getList({ maxResultCount: 500, isActive: true }),
        );
        this.financialItems.set(items.items ?? []);
      } else {
        this.financialItems.set([]);
      }

      if (detail.courseType === CourseType.ExternalLocal) {
        this.fScenario.set(FundingScenario.FundingSourceCoversAll);
      } else if (
        detail.fundingScenario === FundingScenario.FundingSourceCoversAll
        || detail.fundingScenario === FundingScenario.FundingSourceCoversCourse
      ) {
        this.fScenario.set(detail.fundingScenario);
      } else {
        this.fScenario.set(null);
      }
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
    } finally {
      this.loading.set(false);
    }
  }

  async onScenarioSelect(scenario: FundingScenario): Promise<void> {
    if (!this.isInternational() || !this.isReviewable() || this.isReadOnly()) return;
    if (this.fScenario() === scenario) return;
    this.fScenario.set(scenario);

    this.actionBusy.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(
        this.service.assignScenario(this.courseId(), {
          fundingScenario: scenario,
          adjustments: [],
          commit: false,
        }),
      );
      await this.loadAll();
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
      await this.loadAll();
    } finally {
      this.actionBusy.set(false);
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
    const scenario = this.isLocal()
      ? FundingScenario.FundingSourceCoversAll
      : this.fScenario();
    if (scenario === null) {
      this.error.set('اختر طريقة تغطية تكاليف السفر قبل المتابعة.');
      return;
    }
    if (this.courseCost() <= 0) {
      this.error.set('تكلفة الدورة غير مسجلة أو تساوي صفرًا.');
      return;
    }
    if (this.isInternational() && this.missingTravelVoteCodes() > 0) {
      this.error.set('توجد بنود سفر بدون رمز بند مالي. أكمل إعدادات البنود قبل الاعتماد.');
      return;
    }
    this.actionBusy.set(true);
    this.error.set(null);
    try {
      const body: AssignScenarioDto = {
        fundingScenario: scenario,
        adjustments: [],
        commit,
      };
      await firstValueFrom(this.service.assignScenario(this.courseId(), body));
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
