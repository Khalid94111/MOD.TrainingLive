import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import { firstValueFrom } from 'rxjs';

import { CoursePaymentService } from 'src/app/proxy/training/payments/course-payment.service';
import { TravelAllowancePaymentService } from 'src/app/proxy/training/payments/travel-allowance-payment.service';
import type {
  CoursePaymentDto,
  TravelAllowancePaymentDto,
} from 'src/app/proxy/training/payments/dtos/models';
import { CourseSessionService } from 'src/app/proxy/training/plans/course-session.service';
import type { CourseSessionDetailDto } from 'src/app/proxy/training/plans/dtos/models';
import { CourseType } from 'src/app/proxy/training/enums/course-type.enum';
import { PaymentStatus } from 'src/app/proxy/training/enums/payment-status.enum';
import { SessionStatus } from 'src/app/proxy/training/enums/session-status.enum';

import { TrainingLocalizationHelper } from '../../shared';
import { PriceQuotesComponent } from '../../execution/price-quotes/price-quotes.component';
import { SessionDetailRefreshService } from './session-detail-refresh.service';
import { SessionSectionPaymentsComponent } from './sections/section-payments/session-section-payments.component';
import { SessionTravelDto, SessionTravelService } from './session-travel.service';

type StageKey = 'overview' | 'nominees' | 'quotes' | 'travel' | 'payments' | 'execution';
type StageState = 'complete' | 'attention' | 'available' | 'locked';

interface SessionStage {
  key: StageKey;
  titleKey: string;
  descriptionKey: string;
  iconClass: string;
  state: StageState;
  lockReason?: string;
}

@Component({
  selector: 'app-session-detail',
  standalone: true,
  templateUrl: './session-detail.component.html',
  styleUrls: ['../../shared/gtms-design.scss', './session-detail.component.scss'],
  imports: [
    CommonModule,
    LocalizationPipe,
    PriceQuotesComponent,
    SessionSectionPaymentsComponent,
  ],
})
export class SessionDetailComponent implements OnInit {
  private readonly sessionService = inject(CourseSessionService);
  private readonly travelService = inject(SessionTravelService);
  private readonly travelPaymentService = inject(TravelAllowancePaymentService);
  private readonly coursePaymentService = inject(CoursePaymentService);
  private readonly permissions = inject(PermissionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refresh = inject(SessionDetailRefreshService);
  readonly l = inject(TrainingLocalizationHelper);

  readonly CourseType = CourseType;
  readonly SessionStatus = SessionStatus;

  readonly id = signal<string>('');
  readonly session = signal<CourseSessionDetailDto | null>(null);
  readonly travelProcess = signal<SessionTravelDto | null>(null);
  readonly travelAllowancePayments = signal<TravelAllowancePaymentDto[]>([]);
  readonly coursePayment = signal<CoursePaymentDto | null>(null);
  readonly loading = signal<boolean>(false);
  readonly actionRunning = signal<boolean>(false);
  readonly actionError = signal<string | null>(null);
  readonly travelActionRunning = signal<boolean>(false);
  readonly travelActionError = signal<string | null>(null);

  readonly activeStage = signal<StageKey>('overview');
  private readonly requestedStage = signal<StageKey | null>(null);

  readonly cancelDialogOpen = signal<boolean>(false);
  readonly cancelReason = signal<string>('');
  readonly transitionAction = signal<'inProgress' | 'completed' | null>(null);

  readonly canMarkInProgress = computed(() =>
    this.permissions.getGrantedPolicy('Training.CourseSession.MarkInProgress'));
  readonly canMarkCompleted = computed(() =>
    this.permissions.getGrantedPolicy('Training.CourseSession.MarkCompleted'));
  readonly canCancel = computed(() =>
    this.permissions.getGrantedPolicy('Training.CourseSession.Cancel'));
  readonly canSendTravel = computed(() =>
    this.permissions.getGrantedPolicy('TrainingExecution.TravelRequests.Send'));

  readonly isInternal = computed(() => this.session()?.courseType === CourseType.Internal);
  readonly isLocal = computed(() => this.session()?.courseType === CourseType.ExternalLocal);
  readonly isInternational = computed(() =>
    this.session()?.courseType === CourseType.ExternalInternational);
  readonly status = computed(() => this.session()?.status);
  readonly hasSelectedQuote = computed(() => !!this.session()?.selectedPriceQuoteId);
  readonly travelComplete = computed(() => this.travelProcess()?.isCompleted === true);
  readonly travelStatusLabel = computed(() => {
    const status = this.travelProcess()?.statusCode;
    return status
      ? this.l.t(`::Training.Sessions.Detail.Travel.Status.${status}`)
      : '—';
  });
  readonly coursePaymentConfirmed = computed(() =>
    this.coursePayment()?.status === PaymentStatus.Confirmed);
  readonly travelPaymentsComplete = computed(() => {
    if (!this.isInternational()) return true;
    const expected = this.session()?.nominations?.length ?? 0;
    if (expected === 0) return false;
    const confirmed = this.travelAllowancePayments()
      .filter(payment => payment.status === PaymentStatus.Confirmed).length;
    return confirmed === expected;
  });
  readonly paymentsComplete = computed(() =>
    this.isInternal() || (this.coursePaymentConfirmed() && this.travelPaymentsComplete()));

  readonly courseTypeKey = computed(() => {
    switch (this.session()?.courseType) {
      case CourseType.Internal:              return '::Training.CourseType.Internal';
      case CourseType.ExternalLocal:         return '::Training.CourseType.ExternalLocal';
      case CourseType.ExternalInternational: return '::Training.CourseType.ExternalInternational';
      default: return '';
    }
  });
  readonly courseTypeCss = computed(() => {
    switch (this.session()?.courseType) {
      case CourseType.Internal:              return 'type-pill type-internal';
      case CourseType.ExternalLocal:         return 'type-pill type-local';
      case CourseType.ExternalInternational: return 'type-pill type-international';
      default: return 'type-pill';
    }
  });
  readonly statusBadgeKey = computed(() => {
    switch (this.status()) {
      case SessionStatus.Planned:           return '::Training.Session.Status.Planned';
      case SessionStatus.Scheduled:         return '::Training.Session.Status.Scheduled';
      case SessionStatus.InProgress:        return '::Training.Session.Status.InProgress';
      case SessionStatus.Completed:         return '::Training.Session.Status.Completed';
      case SessionStatus.Cancelled:         return '::Training.Session.Status.Cancelled';
      case SessionStatus.FinanciallyClosed: return '::Training.Session.Status.FinanciallyClosed';
      default: return '';
    }
  });
  readonly statusBadgeCss = computed(() => {
    switch (this.status()) {
      case SessionStatus.Planned:    return 'status-pill status-planned';
      case SessionStatus.Scheduled:  return 'status-pill status-scheduled';
      case SessionStatus.InProgress: return 'status-pill status-inprogress';
      case SessionStatus.Completed:
      case SessionStatus.FinanciallyClosed: return 'status-pill status-completed';
      case SessionStatus.Cancelled:  return 'status-pill status-cancelled';
      default: return 'status-pill';
    }
  });

  readonly travelLockReason = computed(() =>
    this.hasSelectedQuote()
      ? ''
      : this.l.t('::Training.Sessions.Detail.TravelLockedNoQuote'));
  readonly paymentsLocked = computed(() => {
    if (this.status() === SessionStatus.Planned || this.status() === SessionStatus.Cancelled) {
      return true;
    }
    return this.isInternational() && !this.travelComplete();
  });
  readonly paymentsLockReason = computed(() => {
    if (this.status() === SessionStatus.Cancelled) {
      return this.l.t('::Training.Sessions.Detail.Workspace.CancelledLock');
    }
    if (this.status() === SessionStatus.Planned) {
      return this.l.t('::Training.Sessions.Detail.PaymentsLockedPlanned');
    }
    if (this.isInternational() && !this.travelComplete()) {
      return this.l.t('::Training.Sessions.Detail.PaymentsLockedNoTI');
    }
    return '';
  });

  readonly recommendedStageKey = computed<StageKey>(() => {
    const status = this.status();
    if (status === SessionStatus.Cancelled
      || status === SessionStatus.InProgress
      || status === SessionStatus.Completed
      || status === SessionStatus.FinanciallyClosed) {
      return 'execution';
    }
    if (status === SessionStatus.Planned && this.isInternal()) return 'overview';
    if (!this.isInternal() && !this.hasSelectedQuote()) return 'quotes';
    if (this.isInternational() && !this.travelComplete()) return 'travel';
    if (!this.paymentsComplete()) return 'payments';
    if (status === SessionStatus.Scheduled) return 'execution';
    return 'overview';
  });

  readonly visibleStages = computed<SessionStage[]>(() => {
    const status = this.status();
    const stages: SessionStage[] = [
      {
        key: 'overview',
        titleKey: '::Training.Sessions.Detail.Section1.Title',
        descriptionKey: '::Training.Sessions.Detail.Workspace.OverviewDescription',
        iconClass: 'bi bi-grid-1x2',
        state: 'complete',
      },
      {
        key: 'nominees',
        titleKey: '::Training.Sessions.Detail.Section2.Title',
        descriptionKey: '::Training.Sessions.Detail.Workspace.NomineesDescription',
        iconClass: 'bi bi-people',
        state: 'complete',
      },
    ];

    if (!this.isInternal()) {
      stages.push({
        key: 'quotes',
        titleKey: '::Training.Sessions.Detail.Section3.Title',
        descriptionKey: '::Training.Sessions.Detail.Workspace.QuotesDescription',
        iconClass: 'bi bi-receipt',
        state: this.hasSelectedQuote() ? 'complete' : 'attention',
      });
    }

    if (this.isInternational()) {
      stages.push({
        key: 'travel',
        titleKey: '::Training.Sessions.Detail.Section4.Title',
        descriptionKey: '::Training.Sessions.Detail.Workspace.TravelDescription',
        iconClass: 'bi bi-airplane',
        state: this.travelComplete()
          ? 'complete'
          : this.hasSelectedQuote() ? 'attention' : 'locked',
        lockReason: this.travelLockReason(),
      });
    }

    if (!this.isInternal()) {
      stages.push({
        key: 'payments',
        titleKey: '::Training.Sessions.Detail.Section5.Title',
        descriptionKey: '::Training.Sessions.Detail.Workspace.PaymentsDescription',
        iconClass: 'bi bi-wallet2',
        state: this.paymentsComplete()
          ? 'complete'
          : this.paymentsLocked() ? 'locked' : 'attention',
        lockReason: this.paymentsLockReason(),
      });
    }

    stages.push({
      key: 'execution',
      titleKey: '::Training.Sessions.Detail.Workspace.ExecutionTitle',
      descriptionKey: '::Training.Sessions.Detail.Workspace.ExecutionDescription',
      iconClass: 'bi bi-play-circle',
      state: status === SessionStatus.Completed || status === SessionStatus.FinanciallyClosed
        ? 'complete'
        : status === SessionStatus.InProgress
          ? 'attention'
          : status === SessionStatus.Cancelled
            ? 'available'
            : !this.paymentsComplete()
              ? 'locked'
              : status === SessionStatus.Scheduled ? 'attention' : 'available',
      lockReason: !this.paymentsComplete()
        ? this.l.t('::Training.Sessions.Detail.Workspace.CompletePreviousStage')
        : '',
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
    this.activeStageIndex() >= 0 && this.activeStageIndex() < this.visibleStages().length - 1);
  readonly unrestrictedHistory = computed(() =>
    this.status() === SessionStatus.InProgress
    || this.status() === SessionStatus.Completed
    || this.status() === SessionStatus.Cancelled
    || this.status() === SessionStatus.FinanciallyClosed);
  readonly unlockedThroughIndex = computed(() => {
    if (this.unrestrictedHistory()) return this.visibleStages().length - 1;
    const firstIncomplete = this.visibleStages().findIndex(stage => stage.state !== 'complete');
    return firstIncomplete === -1 ? this.visibleStages().length - 1 : firstIncomplete;
  });
  readonly canGoNextStage = computed(() => {
    if (!this.hasNextStage()) return false;
    return this.activeStageIndex() + 1 <= this.unlockedThroughIndex();
  });

  readonly showCancelButton = computed(() =>
    this.canCancel()
    && (this.status() === SessionStatus.Planned || this.status() === SessionStatus.Scheduled));
  readonly showMarkInProgressButton = computed(() =>
    this.canMarkInProgress() && this.status() === SessionStatus.Scheduled);
  readonly showMarkCompletedButton = computed(() =>
    this.canMarkCompleted() && this.status() === SessionStatus.InProgress);
  readonly executionMessageKey = computed(() => {
    switch (this.status()) {
      case SessionStatus.Planned:           return '::Training.Sessions.Detail.Workspace.ExecutionWaiting';
      case SessionStatus.Scheduled:         return '::Training.Sessions.Detail.Workspace.ExecutionReady';
      case SessionStatus.InProgress:        return '::Training.Sessions.Detail.Workspace.ExecutionRunning';
      case SessionStatus.Completed:
      case SessionStatus.FinanciallyClosed: return '::Training.Sessions.Detail.Workspace.ExecutionDone';
      case SessionStatus.Cancelled:         return '::Training.Sessions.Detail.Workspace.ExecutionCancelled';
      default: return '';
    }
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.id.set(id);
    if (!id) return;

    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        const stage = this.parseStage(params.get('stage'));
        this.requestedStage.set(stage);
        if (stage) this.activeStage.set(stage);
      });

    void this.loadAll();
    this.refresh.events
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => void this.loadAll());
  }

  private async loadAll(): Promise<void> {
    if (!this.id()) return;
    this.loading.set(true);
    try {
      await this.loadSession();

      if (this.isInternational()) {
        // Travel completion imports confirmed finance records server-side; load it before payments
        // so the payment list always reflects the latest completed result.
        await this.loadTravelProcess();
      } else {
        this.travelProcess.set(null);
      }

      if (this.isInternal()) {
        this.travelAllowancePayments.set([]);
        this.coursePayment.set(null);
      } else {
        await this.loadPayments();
      }
      this.normalizeStageSelection();
    } finally {
      this.loading.set(false);
    }
  }

  private async loadSession(): Promise<void> {
    try {
      this.session.set(await firstValueFrom(this.sessionService.get(this.id())));
    } catch {
      this.session.set(null);
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

  async sendToTravel(): Promise<void> {
    if (!this.canSendTravel() || !this.travelProcess()?.canSend) return;
    this.travelActionError.set(null);
    this.travelActionRunning.set(true);
    try {
      this.travelProcess.set(await firstValueFrom(this.travelService.send(this.id())));
      await this.loadPayments();
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
      await this.loadPayments();
      this.normalizeStageSelection();
    } finally {
      this.travelActionRunning.set(false);
    }
  }

  openTravelRequest(): void {
    const requestId = this.travelProcess()?.travelRequestId;
    if (requestId) void this.router.navigate(['/travel/requests', requestId]);
  }

  private async loadPayments(): Promise<void> {
    const sessionId = this.id();
    const [travel, course] = await Promise.all([
      firstValueFrom(this.travelPaymentService.getList({ sessionId, maxResultCount: 200 }))
        .catch(() => null),
      firstValueFrom(this.coursePaymentService.getList({ sessionId, maxResultCount: 10 }))
        .catch(() => null),
    ]);
    this.travelAllowancePayments.set(travel?.items ?? []);
    this.coursePayment.set(course?.items?.[0] ?? null);
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

  stageStateKey(state: StageState): string {
    switch (state) {
      case 'complete':  return '::Training.Sessions.Detail.Workspace.StateComplete';
      case 'attention': return '::Training.Sessions.Detail.Workspace.StateAttention';
      case 'locked':    return '::Training.Sessions.Detail.Workspace.StateLocked';
      default:          return '::Training.Sessions.Detail.Workspace.StateAvailable';
    }
  }

  canAccessStage(stage: StageKey): boolean {
    const index = this.visibleStages().findIndex(item => item.key === stage);
    return index >= 0 && index <= this.unlockedThroughIndex();
  }

  requestTransition(action: 'inProgress' | 'completed'): void {
    this.actionError.set(null);
    this.transitionAction.set(action);
  }

  cancelTransition(): void {
    if (!this.actionRunning()) this.transitionAction.set(null);
  }

  async confirmTransition(): Promise<void> {
    const action = this.transitionAction();
    if (!action) return;
    const ok = action === 'inProgress'
      ? await this.runAction(() => this.sessionService.markInProgress(this.id()))
      : await this.runAction(() => this.sessionService.markCompleted(this.id()));
    if (ok) this.transitionAction.set(null);
  }

  openCancelDialog(): void {
    this.cancelReason.set('');
    this.actionError.set(null);
    this.cancelDialogOpen.set(true);
  }

  closeCancelDialog(): void {
    if (!this.actionRunning()) this.cancelDialogOpen.set(false);
  }

  onCancelReasonChange(event: Event): void {
    this.cancelReason.set((event.target as HTMLTextAreaElement).value);
  }

  async confirmCancel(): Promise<void> {
    const reason = this.cancelReason().trim();
    if (!reason) {
      this.actionError.set(this.l.t('::Training.Sessions.Detail.CancelReasonRequired'));
      return;
    }
    const ok = await this.runAction(() => this.sessionService.cancel(this.id(), { reason }));
    if (ok) this.cancelDialogOpen.set(false);
  }

  private async runAction(
    call: () => ReturnType<CourseSessionService['markInProgress']>,
  ): Promise<boolean> {
    this.actionError.set(null);
    this.actionRunning.set(true);
    try {
      await firstValueFrom(call());
      await this.loadAll();
      return true;
    } catch (error) {
      this.actionError.set(this.extractError(error));
      return false;
    } finally {
      this.actionRunning.set(false);
    }
  }

  fmt(iso: string | null | undefined): string {
    return iso ? iso.substring(0, 10) : '';
  }

  goBack(): void {
    void this.router.navigate(['/training/sessions']);
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
    const stages: StageKey[] = ['overview', 'nominees', 'quotes', 'travel', 'payments', 'execution'];
    return stages.includes(value as StageKey) ? value as StageKey : null;
  }

  private scrollWorkspaceIntoView(): void {
    queueMicrotask(() => {
      document.getElementById('session-workspace')
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
