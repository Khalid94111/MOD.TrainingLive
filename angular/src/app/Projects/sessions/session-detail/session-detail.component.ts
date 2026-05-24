import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';

import { CourseSessionService } from 'src/app/proxy/training/plans/course-session.service';
import type { CourseSessionDetailDto } from 'src/app/proxy/training/plans/dtos/models';
import { PriceQuoteService } from 'src/app/proxy/training/finance';
import { TravelInstructionService } from 'src/app/proxy/training/execution/travel-instruction.service';
import type { TravelInstructionDto } from 'src/app/proxy/training/execution/dtos/models';
import { TravelInstructionStatus } from 'src/app/proxy/training/enums/travel-instruction-status.enum';
import { TravelAllowancePaymentService } from 'src/app/proxy/training/payments/travel-allowance-payment.service';
import { CoursePaymentService } from 'src/app/proxy/training/payments/course-payment.service';
import type {
  TravelAllowancePaymentDto,
  CoursePaymentDto,
} from 'src/app/proxy/training/payments/dtos/models';
import { CourseType } from 'src/app/proxy/training/enums/course-type.enum';
import { SessionStatus } from 'src/app/proxy/training/enums/session-status.enum';

import {
  CourseInfoBarComponent,
  type CourseInfoBarData,
  TrainingLocalizationHelper,
} from '../../shared';
import { SessionDetailRefreshService } from './session-detail-refresh.service';
import { PriceQuotesComponent } from '../../execution/price-quotes/price-quotes.component';
import { TravelInstructionsComponent } from '../../execution/travel-instructions/travel-instructions.component';
import {
  SessionSectionPaymentsComponent,
  type SectionState,
} from './sections/section-payments/session-section-payments.component';

type SectionKey = 'details' | 'nominees' | 'quotes' | 'travel' | 'payments';

interface PipelineNode {
  key: string;
  labelKey: string;
  state: 'completed' | 'active' | 'pending';
  anchor?: SectionKey;
  skipped?: boolean;
}

// Phase 4C-α (v4.10.0) — PAGE D: Session Detail shell.
// Patch 4 (v4.10.4) — Section 4 (Travel) and the Travel pipeline node are skipped for
// ExternalLocal as well as Internal; Section 5 no longer gates on TI for Local courses.
//
// Stage-based progressive disclosure mirroring the casual-course pattern. Five sections:
//   1) Details          — session context + plan-item ref + dates
//   2) Nominees         — read-only list with substitution markers
//   3) Price Quotes     — delegates to <app-price-quotes> (parentArm: 'session')
//   4) Travel Instruction — delegates to <app-travel-instructions> (parentArm: 'session')
//   5) Payments         — slim summary; reallocations card omitted (sessions don't reallocate)
//
// Sections 3 and 4 are HIDDEN (not just locked) for Internal courses per §5.4 state matrix.
// Header action bar buttons are computed from (status × user permissions).
@Component({
  selector: 'app-session-detail',
  standalone: true,
  templateUrl: './session-detail.component.html',
  styleUrls: ['./session-detail.component.scss', '../../shared/gtms-design.scss'],
  imports: [
    CommonModule,
    LocalizationPipe,
    CourseInfoBarComponent,
    PriceQuotesComponent,
    TravelInstructionsComponent,
    SessionSectionPaymentsComponent,
  ],
})
export class SessionDetailComponent implements OnInit {
  private readonly sessionService = inject(CourseSessionService);
  private readonly quoteService = inject(PriceQuoteService);
  private readonly travelService = inject(TravelInstructionService);
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

  // ── Identity + data signals ────────────────────────────────────────
  readonly id = signal<string>('');
  readonly session = signal<CourseSessionDetailDto | null>(null);
  readonly travelInstruction = signal<TravelInstructionDto | null>(null);
  readonly travelAllowancePayments = signal<TravelAllowancePaymentDto[]>([]);
  readonly coursePayment = signal<CoursePaymentDto | null>(null);
  readonly loading = signal<boolean>(false);
  readonly actionError = signal<string | null>(null);

  // ── User-toggle overrides ──
  private readonly userExpanded = signal<SectionKey | null>(null);
  private readonly userCollapsed = signal<Set<SectionKey>>(new Set());

  // ── Cancel dialog state ──
  readonly cancelDialogOpen = signal<boolean>(false);
  readonly cancelReason = signal<string>('');

  // ── Permissions ──
  readonly canSelectQuote = computed(() =>
    this.permissions.getGrantedPolicy('Training.CourseSession.SelectQuote'));
  readonly canMarkInProgress = computed(() =>
    this.permissions.getGrantedPolicy('Training.CourseSession.MarkInProgress'));
  readonly canMarkCompleted = computed(() =>
    this.permissions.getGrantedPolicy('Training.CourseSession.MarkCompleted'));
  readonly canCancel = computed(() =>
    this.permissions.getGrantedPolicy('Training.CourseSession.Cancel'));

  // Phase 4C-α Patch 1 — feeds <app-course-info-bar variant="session">. Returns null
  // until session loads; the template guards on the null and skips rendering.
  readonly infoBarData = computed<CourseInfoBarData | null>(() => {
    const s = this.session();
    if (!s) return null;
    return {
      courseName: s.tenantCourseNameAr || s.tenantCourseNameEn || '',
      unitName: s.unitName || null,
      durationDays: s.durationDays ?? 0,
      nomineesCount: s.nomineesCount ?? 0,
      officersCount: s.officersCount ?? 0,
      enlistedCount: s.enlistedCount ?? 0,
      approvedCostOMR: s.approvedCostOMR ?? 0,
      unitTotalOMR: s.unitTotalOMR ?? 0,
    };
  });

  // ── Derived state ──
  readonly isInternal = computed(() => this.session()?.courseType === CourseType.Internal);
  // Patch 4 (v4.10.4) — Local provider is in-country, so Section 4 (travel) is hidden.
  readonly isLocal = computed(() => this.session()?.courseType === CourseType.ExternalLocal);
  readonly status = computed(() => this.session()?.status);
  readonly hasSelectedQuote = computed(() => !!this.session()?.selectedPriceQuoteId);
  readonly travelIssued = computed(() =>
    this.travelInstruction()?.status === TravelInstructionStatus.Issued);
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
      case SessionStatus.Planned:    return 'status-badge status-planned';
      case SessionStatus.Scheduled:  return 'status-badge status-scheduled';
      case SessionStatus.InProgress: return 'status-badge status-inprogress';
      case SessionStatus.Completed:
      case SessionStatus.FinanciallyClosed: return 'status-badge status-completed';
      case SessionStatus.Cancelled:  return 'status-badge status-cancelled';
      default: return 'status-badge';
    }
  });

  // ── Section state matrix (per §5.4) ──
  readonly detailsState = computed<SectionState>(() =>
    this.applyToggle('details', 'collapsed'));
  readonly nomineesState = computed<SectionState>(() =>
    this.applyToggle('nominees', 'collapsed'));

  // Section 3: hidden for Internal entirely. For External, active when Planned + canSelectQuote.
  readonly quotesHidden = computed(() => this.isInternal());
  readonly quotesState = computed<SectionState>(() => {
    if (this.quotesHidden()) return 'locked'; // ignored (not rendered)
    if (this.status() === SessionStatus.Planned && this.canSelectQuote()) {
      return this.applyToggle('quotes', 'active');
    }
    return this.applyToggle('quotes', 'collapsed');
  });
  readonly quotesLockReason = computed(() => '');

  // Section 4: hidden for Internal AND ExternalLocal (no travel for in-country providers).
  // For ExternalInternational, active when Scheduled (post-quote) without an issued TI.
  readonly travelHidden = computed(() => this.isInternal() || this.isLocal());
  readonly travelState = computed<SectionState>(() => {
    if (this.travelHidden()) return 'locked';
    if (!this.hasSelectedQuote()) return 'locked';
    if (!this.travelIssued() && this.status() === SessionStatus.Scheduled) {
      return this.applyToggle('travel', 'active');
    }
    return this.applyToggle('travel', 'collapsed');
  });
  readonly travelLockReason = computed(() => {
    if (!this.hasSelectedQuote()) return this.l.t('::Training.Sessions.Detail.TravelLockedNoQuote');
    return '';
  });

  // Section 5: unlocked when Intl-with-TI-issued, Local-and-Scheduled+, or Internal-and-Scheduled+.
  // Patch 4 (v4.10.4) — Local courses skip the TI gate (no travel for in-country providers).
  readonly paymentsLocked = computed(() => {
    if (this.status() === SessionStatus.Planned) return true;
    if (this.status() === SessionStatus.Cancelled) return true;
    if (!this.isInternal() && !this.isLocal() && !this.travelIssued()) return true;
    return false;
  });
  readonly paymentsState = computed<SectionState>(() => {
    if (this.paymentsLocked()) return 'locked';
    if (this.status() === SessionStatus.Scheduled
        && (this.isInternal() || this.isLocal() || this.travelIssued())) {
      return this.applyToggle('payments', 'active');
    }
    return this.applyToggle('payments', 'collapsed');
  });
  readonly paymentsLockReason = computed(() => {
    if (this.status() === SessionStatus.Planned) {
      return this.l.t('::Training.Sessions.Detail.PaymentsLockedPlanned');
    }
    if (!this.isInternal() && !this.isLocal() && !this.travelIssued()) {
      return this.l.t('::Training.Sessions.Detail.PaymentsLockedNoTI');
    }
    return '';
  });

  // ── Pipeline (8 nodes per §5.4) ──
  readonly pipelineNodes = computed<PipelineNode[]>(() => {
    const s = this.status();
    const isInt = this.isInternal();
    // Patch 4 (v4.10.4) — Local courses also skip the Travel node.
    const isLoc = this.isLocal();
    const noTravel = isInt || isLoc;
    const planDone = true;            // node 1 always done (plan was THApproved)
    const createdDone = !!s;          // node 2 always done if session exists
    const quoteDone = isInt || this.hasSelectedQuote();
    const datesDone = !!this.session()?.actualStartDate;
    const travelDone = noTravel || this.travelIssued();
    const paymentsDone = this.coursePayment()?.status === 1; // PaymentStatus.Confirmed
    const inProgressDone = s !== undefined &&
      (s === SessionStatus.InProgress || s === SessionStatus.Completed || s === SessionStatus.FinanciallyClosed);
    const completedDone = s === SessionStatus.Completed || s === SessionStatus.FinanciallyClosed;

    const stateOf = (done: boolean, current: boolean): PipelineNode['state'] =>
      done ? 'completed' : current ? 'active' : 'pending';

    return [
      { key: 'plan',       labelKey: '::Training.Sessions.Detail.Pipeline.ByPlan',         state: stateOf(planDone, false),         anchor: undefined },
      { key: 'created',    labelKey: '::Training.Sessions.Detail.Pipeline.Created',        state: stateOf(createdDone, false),      anchor: 'details' },
      { key: 'quote',      labelKey: '::Training.Sessions.Detail.Pipeline.Quote',          state: stateOf(quoteDone, s === SessionStatus.Planned && !isInt), anchor: 'quotes', skipped: isInt },
      { key: 'dates',      labelKey: '::Training.Sessions.Detail.Pipeline.Dates',          state: stateOf(datesDone, false),        anchor: 'details' },
      { key: 'travel',     labelKey: '::Training.Sessions.Detail.Pipeline.Travel',         state: stateOf(travelDone, s === SessionStatus.Scheduled && !noTravel && !this.travelIssued()), anchor: 'travel', skipped: noTravel },
      { key: 'payments',   labelKey: '::Training.Sessions.Detail.Pipeline.Payments',       state: stateOf(paymentsDone, s === SessionStatus.Scheduled && (noTravel || this.travelIssued())), anchor: 'payments' },
      { key: 'inProgress', labelKey: '::Training.Sessions.Detail.Pipeline.InProgress',     state: stateOf(inProgressDone, s === SessionStatus.Scheduled) },
      { key: 'completed',  labelKey: '::Training.Sessions.Detail.Pipeline.Completed',      state: stateOf(completedDone, s === SessionStatus.InProgress) },
    ];
  });

  // ── Action bar (status × role → buttons) per §5.4 ──
  readonly showCancelButton = computed(() =>
    this.canCancel() && (this.status() === SessionStatus.Planned || this.status() === SessionStatus.Scheduled));
  readonly showMarkInProgressButton = computed(() =>
    this.canMarkInProgress() && this.status() === SessionStatus.Scheduled);
  readonly showMarkCompletedButton = computed(() =>
    this.canMarkCompleted() && this.status() === SessionStatus.InProgress);

  // ── Lifecycle ──
  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.id.set(id);
    if (!id) return;

    void this.loadAll();

    this.refresh.events
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => void this.loadAll());
  }

  // ── Data loading ──
  private async loadAll(): Promise<void> {
    if (!this.id()) return;
    this.loading.set(true);
    try {
      await Promise.all([
        this.loadSession(),
        this.loadTravelInstruction(),
        this.loadPayments(),
      ]);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadSession(): Promise<void> {
    try {
      const detail = await firstValueFrom(this.sessionService.get(this.id()));
      this.session.set(detail);
    } catch {
      this.session.set(null);
    }
  }

  private async loadTravelInstruction(): Promise<void> {
    try {
      const ti = await firstValueFrom(this.travelService.getByParent('', this.id()));
      this.travelInstruction.set(ti ?? null);
    } catch {
      this.travelInstruction.set(null);
    }
  }

  private async loadPayments(): Promise<void> {
    const sessionId = this.id();
    if (!sessionId) return;
    const [travel, course] = await Promise.all([
      firstValueFrom(this.travelPaymentService.getList({ sessionId, maxResultCount: 200 })).catch(() => null),
      firstValueFrom(this.coursePaymentService.getList({ sessionId, maxResultCount: 10 })).catch(() => null),
    ]);
    this.travelAllowancePayments.set(travel?.items ?? []);
    this.coursePayment.set(course?.items?.[0] ?? null);
  }

  // ── Section toggles ──
  private applyToggle(key: SectionKey, def: SectionState): SectionState {
    if (def === 'locked') return 'locked';
    const expanded = this.userExpanded();
    if (expanded === key) return 'active';
    if (expanded && expanded !== key) return def === 'active' ? 'collapsed' : def;
    if (this.userCollapsed().has(key)) return 'collapsed';
    return def;
  }

  onSectionToggle(key: SectionKey): void {
    const currentState = this.stateFor(key);
    if (currentState === 'active') {
      this.userCollapsed.update(s => {
        const next = new Set(s);
        next.add(key);
        return next;
      });
      if (this.userExpanded() === key) this.userExpanded.set(null);
    } else if (currentState !== 'locked') {
      this.userExpanded.set(key);
      this.userCollapsed.update(s => {
        const next = new Set(s);
        next.delete(key);
        return next;
      });
    }
  }

  private stateFor(key: SectionKey): SectionState {
    switch (key) {
      case 'details':  return this.detailsState();
      case 'nominees': return this.nomineesState();
      case 'quotes':   return this.quotesState();
      case 'travel':   return this.travelState();
      case 'payments': return this.paymentsState();
    }
  }

  onPipelineNodeClick(node: PipelineNode): void {
    if (!node.anchor || node.skipped) return;
    this.userExpanded.set(node.anchor);
    this.userCollapsed.update(s => {
      const next = new Set(s);
      next.delete(node.anchor!);
      return next;
    });
    queueMicrotask(() => {
      const el = document.getElementById(`section-${node.anchor}`);
      if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  }

  // ── Actions ──
  async onMarkInProgress(): Promise<void> {
    if (!confirm(this.l.t('::Training.Sessions.Detail.ConfirmMarkInProgress'))) return;
    await this.runAction(() => this.sessionService.markInProgress(this.id()));
  }

  async onMarkCompleted(): Promise<void> {
    if (!confirm(this.l.t('::Training.Sessions.Detail.ConfirmMarkCompleted'))) return;
    await this.runAction(() => this.sessionService.markCompleted(this.id()));
  }

  openCancelDialog(): void {
    this.cancelReason.set('');
    this.actionError.set(null);
    this.cancelDialogOpen.set(true);
  }
  closeCancelDialog(): void {
    this.cancelDialogOpen.set(false);
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

  private async runAction(call: () => any): Promise<boolean> {
    this.actionError.set(null);
    try {
      const updated = await firstValueFrom(call());
      if (updated) this.session.set(updated);
      await this.loadAll();
      return true;
    } catch (err) {
      this.actionError.set(this.extractError(err));
      return false;
    }
  }

  // ── Display helpers ──
  fmt(iso: string | null | undefined): string {
    return iso ? iso.substring(0, 10) : '';
  }

  goBack(): void {
    void this.router.navigate(['/training/sessions']);
  }

  private extractError(err: unknown): string {
    if (err && typeof err === 'object') {
      const anyErr = err as { error?: { error?: { message?: string } }; message?: string };
      return anyErr.error?.error?.message ?? anyErr.message ?? this.l.t('::Training.Sessions.Detail.GenericError');
    }
    return this.l.t('::Training.Sessions.Detail.GenericError');
  }
}
