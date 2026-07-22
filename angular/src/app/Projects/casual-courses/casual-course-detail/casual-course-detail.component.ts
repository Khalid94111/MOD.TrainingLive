import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { PermissionService } from '@abp/ng.core';
import { filter, firstValueFrom } from 'rxjs';

import { CasualCourseService } from 'src/app/proxy/training/casual-courses';
import type { CasualCourseDetailDto } from 'src/app/proxy/training/casual-courses/dtos/models';
import { PriceQuoteService } from 'src/app/proxy/training/finance';
import type { PriceQuoteDto } from 'src/app/proxy/training/finance/dtos/models';
import { BudgetReallocationService } from 'src/app/proxy/training/payments/budget-reallocation.service';
import { CoursePaymentService } from 'src/app/proxy/training/payments/course-payment.service';
import { TravelAllowancePaymentService } from 'src/app/proxy/training/payments/travel-allowance-payment.service';
import type {
  BudgetReallocationDto,
  CoursePaymentDto,
  TravelAllowancePaymentDto,
} from 'src/app/proxy/training/payments/dtos/models';
import { PaymentStatus } from 'src/app/proxy/training/enums/payment-status.enum';
import { ReallocationStatus } from 'src/app/proxy/training/enums/reallocation-status.enum';

import {
  CASUAL_COURSE_STATUS_OPTIONS,
  CasualCourseStatus,
  CourseType,
  TrainingLocalizationHelper,
} from '../../shared';
import { CasualCourseDetailRefreshService } from './casual-course-detail-refresh.service';
import { CasualCourseHeaderActionBarComponent } from './header-action-bar/casual-course-header-action-bar.component';
import { CasualCourseSectionDetailsComponent } from './sections/section-details/casual-course-section-details.component';
import { CasualCourseSectionFinancialsComponent } from './sections/section-financials/casual-course-section-financials.component';
import { CasualCourseSectionPaymentsComponent } from './sections/section-payments/casual-course-section-payments.component';
import { CasualCourseSectionQuotesComponent } from './sections/section-quotes/casual-course-section-quotes.component';
import {
  CasualCourseTravelDto,
  CasualCourseTravelService,
} from './casual-course-travel.service';

type StageKey = 'details' | 'financials' | 'quotes' | 'travel' | 'payments' | 'execution';
type StageState = 'complete' | 'attention' | 'available' | 'locked';

interface CasualCourseStage {
  key: StageKey;
  title: string;
  description: string;
  iconClass: string;
  state: StageState;
  lockReason?: string;
}

@Component({
  standalone: true,
  selector: 'app-casual-course-detail',
  templateUrl: './casual-course-detail.component.html',
  styleUrls: [
    '../../sessions/session-detail/session-detail.component.scss',
    './casual-course-detail.component.scss',
    '../../shared/gtms-design.scss',
  ],
  imports: [
    CommonModule,
    CasualCourseHeaderActionBarComponent,
    CasualCourseSectionDetailsComponent,
    CasualCourseSectionFinancialsComponent,
    CasualCourseSectionQuotesComponent,
    CasualCourseSectionPaymentsComponent,
  ],
})
export class CasualCourseDetailComponent implements OnInit {
  private readonly courseService = inject(CasualCourseService);
  private readonly quoteService = inject(PriceQuoteService);
  private readonly travelService = inject(CasualCourseTravelService);
  private readonly travelPaymentService = inject(TravelAllowancePaymentService);
  private readonly coursePaymentService = inject(CoursePaymentService);
  private readonly reallocationService = inject(BudgetReallocationService);
  private readonly permissions = inject(PermissionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly refresh = inject(CasualCourseDetailRefreshService);
  private readonly destroyRef = inject(DestroyRef);
  readonly l = inject(TrainingLocalizationHelper);

  readonly CourseType = CourseType;
  readonly CasualCourseStatus = CasualCourseStatus;

  readonly id = signal<string>('');
  readonly course = signal<CasualCourseDetailDto | null>(null);
  readonly quoteCount = signal<number>(0);
  readonly selectedQuote = signal<PriceQuoteDto | null>(null);
  readonly travelProcess = signal<CasualCourseTravelDto | null>(null);
  readonly travelAllowancePayments = signal<TravelAllowancePaymentDto[]>([]);
  readonly coursePayment = signal<CoursePaymentDto | null>(null);
  readonly reallocations = signal<BudgetReallocationDto[]>([]);
  readonly loading = signal<boolean>(false);
  readonly travelActionRunning = signal<boolean>(false);
  readonly travelActionError = signal<string | null>(null);

  readonly activeStage = signal<StageKey>('details');
  private readonly requestedStage = signal<StageKey | null>(null);

  readonly canEditDraft = computed(() =>
    this.permissions.getGrantedPolicy('Training.CasualCourses.Edit'));
  readonly canApproveUgm = computed(() =>
    this.permissions.getGrantedPolicy('Training.CasualCourses.Approve'));
  readonly canReview = computed(() =>
    this.permissions.getGrantedPolicy('Training.CasualCourses.Review'));
  readonly canTdApprove = computed(() =>
    this.permissions.getGrantedPolicy('Training.CasualCourses.TDApprove'));
  readonly canHeadApprove = computed(() =>
    this.permissions.getGrantedPolicy('Training.CasualCourses.HeadApprove'));
  readonly canSendTravel = computed(() =>
    this.permissions.getGrantedPolicy('TrainingExecution.TravelRequests.Send'));

  readonly isNewMode = computed(() => !this.id());
  readonly status = computed(() => this.course()?.status);
  readonly isInternal = computed(() => this.course()?.courseType === CourseType.Internal);
  readonly isLocal = computed(() => this.course()?.courseType === CourseType.ExternalLocal);
  readonly isInternational = computed(() =>
    this.course()?.courseType === CourseType.ExternalInternational);
  readonly hasSelectedQuote = computed(() => !!this.course()?.selectedPriceQuoteId);
  readonly travelComplete = computed(() => this.travelProcess()?.isCompleted === true);

  readonly statusBadge = computed(() => {
    const status = this.status();
    return CASUAL_COURSE_STATUS_OPTIONS.find(option => option.value === status);
  });

  readonly travelStatusLabel = computed(() => {
    const status = this.travelProcess()?.statusCode;
    return status ? this.l.t(`::Training.Sessions.Detail.Travel.Status.${status}`) : '—';
  });

  readonly travelPaymentsComplete = computed(() => {
    if (!this.isInternational()) return true;
    const expected = this.course()?.nomineesCount ?? this.course()?.nominations?.length ?? 0;
    if (expected === 0 || !this.travelComplete()) return false;
    const confirmed = this.travelAllowancePayments()
      .filter(payment => payment.status === PaymentStatus.Confirmed).length;
    return confirmed === expected;
  });

  readonly coursePaymentComplete = computed(() =>
    this.coursePayment()?.status === PaymentStatus.Confirmed);

  readonly reallocationsComplete = computed(() => {
    const rows = this.reallocations();
    return rows.length === 0
      || rows.every(row => row.status === ReallocationStatus.Approved);
  });

  readonly paymentsComplete = computed(() => {
    if (this.isInternal()) return true;
    return this.travelPaymentsComplete()
      && this.coursePaymentComplete()
      && this.reallocationsComplete();
  });

  readonly detailsComplete = computed(() => {
    const status = this.status();
    return status === CasualCourseStatus.UGMApproved
      || status === CasualCourseStatus.UnderReview
      || status === CasualCourseStatus.StaffReviewed
      || status === CasualCourseStatus.TDApproved
      || status === CasualCourseStatus.THApproved;
  });

  readonly approvalsComplete = computed(() =>
    this.status() === CasualCourseStatus.THApproved);

  readonly recommendedStageKey = computed<StageKey>(() => {
    const status = this.status();
    if (status === undefined
      || status === CasualCourseStatus.Draft
      || status === CasualCourseStatus.ReturnedToCreator
      || status === CasualCourseStatus.Submitted
      || status === CasualCourseStatus.Rejected) {
      return 'details';
    }
    if (!this.approvalsComplete()) return 'financials';
    if (this.isInternal()) return 'execution';
    if (!this.hasSelectedQuote()) return 'quotes';
    if (this.isInternational() && !this.travelComplete()) return 'travel';
    if (!this.paymentsComplete()) return 'payments';
    return 'execution';
  });

  readonly visibleStages = computed<CasualCourseStage[]>(() => {
    const status = this.status();
    const detailsState: StageState = this.detailsComplete() ? 'complete' : 'attention';
    const stages: CasualCourseStage[] = [{
      key: 'details',
      title: 'الطلب والمرشحون',
      description: 'بيانات الدورة والمرشحون واعتماد مدير الوحدة',
      iconClass: 'bi bi-file-earmark-person',
      state: status === CasualCourseStatus.Rejected ? 'complete' : detailsState,
    }];

    if (!this.course()) return stages;

    let financialState: StageState = 'locked';
    if (status === CasualCourseStatus.UGMApproved
      || status === CasualCourseStatus.UnderReview
      || status === CasualCourseStatus.StaffReviewed
      || status === CasualCourseStatus.TDApproved) {
      financialState = 'attention';
    } else if (status === CasualCourseStatus.THApproved) {
      financialState = 'complete';
    } else if (status === CasualCourseStatus.Rejected) {
      financialState = 'available';
    }
    stages.push({
      key: 'financials',
      title: 'المراجعة والاعتماد',
      description: 'مراجعة البنود المالية واستكمال اعتمادات التدريب',
      iconClass: 'bi bi-clipboard2-check',
      state: financialState,
      lockReason: 'تتاح بعد اعتماد مدير الوحدة.',
    });

    if (status === CasualCourseStatus.Rejected) return stages;

    if (!this.isInternal()) {
      stages.push({
        key: 'quotes',
        title: 'عروض الأسعار',
        description: 'إدارة العروض واختيار العرض الفائز وتأكيد التواريخ',
        iconClass: 'bi bi-receipt',
        state: !this.approvalsComplete()
          ? 'locked'
          : this.hasSelectedQuote() ? 'complete' : 'attention',
        lockReason: 'تتاح بعد الاعتماد النهائي للدورة.',
      });
    }

    if (this.isInternational()) {
      stages.push({
        key: 'travel',
        title: 'إجراءات السفر',
        description: 'إرسال الطلب ومتابعة اكتماله في منظومة السفر',
        iconClass: 'bi bi-airplane',
        state: !this.hasSelectedQuote()
          ? 'locked'
          : this.travelComplete() ? 'complete' : 'attention',
        lockReason: 'تتاح بعد اختيار العرض الفائز وتأكيد التواريخ.',
      });
    }

    if (!this.isInternal()) {
      const paymentsLocked = !this.approvalsComplete()
        || !this.hasSelectedQuote()
        || (this.isInternational() && !this.travelComplete());
      stages.push({
        key: 'payments',
        title: 'المدفوعات',
        description: 'دفعة الدورة وإعادة التخصيص والنتائج المعتمدة من السفر',
        iconClass: 'bi bi-wallet2',
        state: paymentsLocked
          ? 'locked'
          : this.paymentsComplete() ? 'complete' : 'attention',
        lockReason: this.isInternational()
          ? 'تتاح بعد اكتمال إجراءات السفر.'
          : 'تتاح بعد اختيار العرض الفائز.',
      });
    }

    const prerequisitesComplete = this.approvalsComplete()
      && (this.isInternal() || this.paymentsComplete());
    stages.push({
      key: 'execution',
      title: 'التنفيذ',
      description: 'متابعة جاهزية الدورة وبدء تنفيذها',
      iconClass: 'bi bi-play-circle',
      state: prerequisitesComplete ? 'attention' : 'locked',
      lockReason: 'أكمل المراحل السابقة أولًا.',
    });

    return stages;
  });

  readonly activeStageMeta = computed(() =>
    this.visibleStages().find(stage => stage.key === this.activeStage())
      ?? this.visibleStages()[0]);
  readonly activeStageIndex = computed(() =>
    this.visibleStages().findIndex(stage => stage.key === this.activeStage()));
  readonly hasPreviousStage = computed(() => this.activeStageIndex() > 0);
  readonly hasNextStage = computed(() =>
    this.activeStageIndex() >= 0
    && this.activeStageIndex() < this.visibleStages().length - 1);
  readonly unlockedThroughIndex = computed(() => {
    const firstIncomplete = this.visibleStages().findIndex(stage => stage.state !== 'complete');
    return firstIncomplete === -1 ? this.visibleStages().length - 1 : firstIncomplete;
  });
  readonly canGoNextStage = computed(() =>
    this.hasNextStage()
    && this.activeStageIndex() + 1 <= this.unlockedThroughIndex());

  readonly showWorkflowActionBar = computed(() => {
    const status = this.status();
    if (status === CasualCourseStatus.THApproved) return true;
    if (status === undefined
      || status === CasualCourseStatus.Draft
      || status === CasualCourseStatus.ReturnedToCreator
      || status === CasualCourseStatus.Submitted) {
      return this.activeStage() === 'details';
    }
    return this.activeStage() === 'financials';
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.id.set(id);
    if (!id) this.refresh.clearAttachment();

    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        const stage = this.parseStage(params.get('stage'));
        this.requestedStage.set(stage);
        if (stage) this.activeStage.set(stage);
      });

    void this.loadAll();

    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        const currentId = this.route.snapshot.paramMap.get('id') ?? '';
        if (currentId && currentId !== this.id()) {
          this.id.set(currentId);
          void this.loadAll();
        }
      });

    this.refresh.events
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => void this.loadAll());

    this.refresh.attachEvents
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(newId => {
        if (!newId || this.id() === newId) return;
        this.id.set(newId);
        void this.loadAll();
      });
  }

  private async loadAll(): Promise<void> {
    if (!this.id()) {
      this.activeStage.set('details');
      return;
    }
    this.loading.set(true);
    try {
      await this.loadCourse();
      if (!this.course()) return;

      if (!this.isInternal()) await this.loadQuoteCount();
      else {
        this.quoteCount.set(0);
        this.selectedQuote.set(null);
      }

      if (this.isInternational()) await this.loadTravelProcess();
      else this.travelProcess.set(null);

      if (this.isInternal()) {
        this.travelAllowancePayments.set([]);
        this.coursePayment.set(null);
        this.reallocations.set([]);
      } else {
        await this.loadPaymentsBundle();
      }
      this.normalizeStageSelection();
    } finally {
      this.loading.set(false);
    }
  }

  private async loadCourse(): Promise<void> {
    try {
      this.course.set(await firstValueFrom(this.courseService.getDetail(this.id())));
    } catch {
      this.course.set(null);
    }
  }

  private async loadQuoteCount(): Promise<void> {
    try {
      const result = await firstValueFrom(
        this.quoteService.getList({ casualCourseId: this.id(), maxResultCount: 200 }),
      );
      this.quoteCount.set(result.totalCount ?? 0);
      this.selectedQuote.set((result.items ?? []).find(quote => quote.isSelected) ?? null);
    } catch {
      this.quoteCount.set(0);
      this.selectedQuote.set(null);
    }
  }

  private async loadTravelProcess(): Promise<void> {
    try {
      this.travelProcess.set(await firstValueFrom(this.travelService.refresh(this.id())));
    } catch (error) {
      this.travelProcess.set(null);
      this.travelActionError.set(this.extractError(error));
    }
  }

  private async loadPaymentsBundle(): Promise<void> {
    const courseId = this.id();
    const [travel, course, reallocations] = await Promise.all([
      firstValueFrom(this.travelPaymentService.getList({ casualCourseId: courseId, maxResultCount: 200 }))
        .catch(() => null),
      firstValueFrom(this.coursePaymentService.getList({ casualCourseId: courseId, maxResultCount: 10 }))
        .catch(() => null),
      firstValueFrom(this.reallocationService.getList({ casualCourseId: courseId, maxResultCount: 200 }))
        .catch(() => null),
    ]);
    this.travelAllowancePayments.set(travel?.items ?? []);
    this.coursePayment.set(course?.items?.[0] ?? null);
    this.reallocations.set(reallocations?.items ?? []);
  }

  async sendToTravel(): Promise<void> {
    if (!this.canSendTravel() || !this.travelProcess()?.canSend) return;
    this.travelActionError.set(null);
    this.travelActionRunning.set(true);
    try {
      this.travelProcess.set(await firstValueFrom(this.travelService.send(this.id())));
      await this.loadPaymentsBundle();
      this.normalizeStageSelection();
    } catch (error) {
      this.travelActionError.set(this.extractError(error));
    } finally {
      this.travelActionRunning.set(false);
    }
  }

  async refreshTravel(): Promise<void> {
    this.travelActionError.set(null);
    this.travelActionRunning.set(true);
    try {
      await this.loadTravelProcess();
      await this.loadPaymentsBundle();
      this.normalizeStageSelection();
    } finally {
      this.travelActionRunning.set(false);
    }
  }

  openTravelRequest(): void {
    const requestId = this.travelProcess()?.travelRequestId;
    if (requestId) void this.router.navigate(['/travel/requests', requestId]);
  }

  selectStage(stage: StageKey): void {
    if (!this.canAccessStage(stage)) return;
    this.activeStage.set(stage);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { stage },
      queryParamsHandling: 'merge',
    });
    this.scrollWorkspaceIntoView();
  }

  goPreviousStage(): void {
    if (!this.hasPreviousStage()) return;
    this.selectStage(this.visibleStages()[this.activeStageIndex() - 1].key);
  }

  goNextStage(): void {
    if (!this.canGoNextStage()) return;
    this.selectStage(this.visibleStages()[this.activeStageIndex() + 1].key);
  }

  goRecommendedStage(): void {
    this.selectStage(this.recommendedStageKey());
  }

  canAccessStage(stage: StageKey): boolean {
    const index = this.visibleStages().findIndex(item => item.key === stage);
    return index >= 0 && index <= this.unlockedThroughIndex();
  }

  stageStateLabel(state: StageState): string {
    switch (state) {
      case 'complete': return 'مكتملة';
      case 'attention': return 'مطلوب إجراء';
      case 'locked': return 'مقفلة';
      default: return 'متاحة';
    }
  }

  courseTypeLabel(): string {
    switch (this.course()?.courseType) {
      case CourseType.Internal: return 'داخلية';
      case CourseType.ExternalLocal: return 'خارجية محلية';
      case CourseType.ExternalInternational: return 'خارجية دولية';
      default: return '—';
    }
  }

  courseTypeCss(): string {
    switch (this.course()?.courseType) {
      case CourseType.Internal: return 'type-pill type-internal';
      case CourseType.ExternalLocal: return 'type-pill type-local';
      case CourseType.ExternalInternational: return 'type-pill type-international';
      default: return 'type-pill';
    }
  }

  fmt(value: string | null | undefined): string {
    return value ? value.substring(0, 10) : '';
  }

  goBack(): void {
    void this.router.navigate(['/training/casual-courses']);
  }

  private normalizeStageSelection(): void {
    const visible = this.visibleStages();
    if (visible.length === 0) return;
    const requested = this.requestedStage();
    const selected = requested && this.canAccessStage(requested)
      ? requested
      : this.recommendedStageKey();
    this.activeStage.set(selected);
    if (requested !== selected) {
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { stage: selected },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      });
    }
  }

  private parseStage(value: string | null): StageKey | null {
    const stages: StageKey[] = ['details', 'financials', 'quotes', 'travel', 'payments', 'execution'];
    return stages.includes(value as StageKey) ? value as StageKey : null;
  }

  private scrollWorkspaceIntoView(): void {
    queueMicrotask(() => {
      document.getElementById('casual-course-workspace')
        ?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  }

  private extractError(error: unknown): string {
    if (error && typeof error === 'object') {
      const apiError = error as { error?: { error?: { message?: string } }; message?: string };
      return apiError.error?.error?.message
        ?? apiError.message
        ?? this.l.t('::Training.Sessions.Detail.GenericError');
    }
    return this.l.t('::Training.Sessions.Detail.GenericError');
  }
}
