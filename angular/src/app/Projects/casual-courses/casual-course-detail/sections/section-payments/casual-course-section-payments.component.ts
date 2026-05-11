import { Component, computed, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LocalizationPipe } from '@abp/ng.core';

import {
  CasualCourseStatus,
  CourseType,
  TrainingLocalizationHelper,
} from '../../../../shared';
import type { CasualCourseDetailDto } from 'src/app/proxy/training/casual-courses/dtos/models';
import type { TravelInstructionDto } from 'src/app/proxy/training/execution/dtos/models';
import type {
  TravelAllowancePaymentDto,
  CoursePaymentDto,
  BudgetReallocationDto,
} from 'src/app/proxy/training/payments/dtos/models';
import { PaymentStatus } from 'src/app/proxy/training/enums/payment-status.enum';
import { ReallocationStatus } from 'src/app/proxy/training/enums/reallocation-status.enum';
import { FundingScenario } from 'src/app/proxy/training/enums/funding-scenario.enum';

import type { SectionState } from '../section-details/casual-course-section-details.component';

/**
 * Section 5 — Payments & Reallocation (Phase 4B-β).
 *
 * State matrix:
 *   locked    — TI not issued (external) or course not THApproved (internal)
 *   active    — preconditions met, not all payments confirmed
 *   collapsed — all payments confirmed (terminal — historical view)
 *
 * Internal courses skip the travel-allowance card entirely (no TI, no per-nominee
 * travel payments). The "all confirmed" check therefore varies by course type.
 *
 * Three child cards (in document order): Travel Allowance summary, Course Payment
 * summary, Reallocation list. Each carries a shortcut link to its dedicated page
 * (PAGE 4.5 / 4.6 / 4.7) — clicked actions navigate away rather than mutating
 * here, so this section is read-only with respect to the shell's course state.
 */
@Component({
  standalone: true,
  selector: 'app-casual-course-section-payments',
  templateUrl: './casual-course-section-payments.component.html',
  styleUrls: [
    './casual-course-section-payments.component.scss',
    '../../../../shared/gtms-design.scss',
  ],
  imports: [CommonModule, LocalizationPipe],
})
export class CasualCourseSectionPaymentsComponent {
  l = inject(TrainingLocalizationHelper);
  private router = inject(Router);

  CasualCourseStatus = CasualCourseStatus;
  CourseType = CourseType;
  PaymentStatus = PaymentStatus;
  ReallocationStatus = ReallocationStatus;
  FundingScenario = FundingScenario;

  state = input.required<SectionState>();
  course = input<CasualCourseDetailDto | null>(null);
  travelInstruction = input<TravelInstructionDto | null>(null);
  travelAllowancePayments = input<TravelAllowancePaymentDto[]>([]);
  coursePayment = input<CoursePaymentDto | null>(null);
  reallocations = input<BudgetReallocationDto[]>([]);
  /** Active exchange rate (OMR → USD) for live conversion. */
  exchangeRate = input<number>(2.6);
  /** Why the section is locked (shown in locked-preview state). */
  lockReason = input<string>('');

  toggle = output<void>();

  // ── Course-type guards ──
  isInternal = computed(() => this.course()?.courseType === CourseType.Internal);
  showTravelCard = computed(() => !this.isInternal());

  // ── Travel Allowance aggregation ──
  travelConfirmedCount = computed(() =>
    this.travelAllowancePayments().filter(p => p.status === PaymentStatus.Confirmed).length,
  );
  travelExpectedCount = computed(() =>
    this.course()?.nomineesCount ?? this.course()?.nominations?.length ?? 0,
  );
  travelTotalOMR = computed(() =>
    this.travelAllowancePayments().reduce((s, p) => s + (p.totalOMR ?? 0), 0),
  );
  allTravelConfirmed = computed(() => {
    const expected = this.travelExpectedCount();
    if (expected === 0) return false;
    return this.travelConfirmedCount() === expected;
  });
  travelStatusBadge = computed(() => {
    if (this.isInternal()) return { text: this.l.t('::Training.Payments.Section5.TravelBadgeNotRequired'), css: 'badge-muted' };
    const confirmed = this.travelConfirmedCount();
    const expected = this.travelExpectedCount();
    const confirmedSuffix = this.l.t('::Training.Payments.Section5.SummaryConfirmedSuffix');
    if (expected === 0) return { text: this.l.t('::Training.Payments.Section5.TravelBadgeNoNominees'), css: 'badge-muted' };
    if (confirmed === expected && expected > 0) return { text: `${confirmed}/${expected} ${confirmedSuffix}`, css: 'badge-confirmed' };
    if (confirmed > 0) return { text: `${confirmed}/${expected} ${confirmedSuffix}`, css: 'badge-pending' };
    return { text: `0/${expected} ${this.l.t('::Training.Payments.Section5.TravelBadgeNotRecorded')}`, css: 'badge-empty' };
  });

  // ── Course Payment ──
  coursePaymentStatus = computed(() => this.coursePayment()?.status);
  coursePaymentVarianceOMR = computed(() => {
    const cp = this.coursePayment();
    if (!cp) return 0;
    return (cp.nebrasAmountOMR ?? 0) - (cp.invoiceAmountOMR ?? 0);
  });
  coursePaymentBadge = computed(() => {
    const cp = this.coursePayment();
    if (!cp) return { text: this.l.t('::Training.Payments.Section5.CourseEmptyBadge'), css: 'badge-empty' };
    if (cp.status === PaymentStatus.Confirmed) return { text: this.l.t('::Training.Payments.Section5.CourseConfirmedBadge'), css: 'badge-confirmed' };
    if (cp.status === PaymentStatus.Cancelled) return { text: this.l.t('::Training.Payments.Section5.CourseCancelledBadge'), css: 'badge-cancelled' };
    return { text: this.l.t('::Training.Payments.Section5.CourseDraftBadge'), css: 'badge-pending' };
  });

  pendingBadgeText = computed(() =>
    this.l.t('::Training.Payments.Section5.ReallocationPendingBadge').replace('{0}', String(this.reallocationsPending().length)),
  );
  approvedBadgeText = computed(() =>
    this.l.t('::Training.Payments.Section5.ReallocationApprovedBadge').replace('{0}', String(this.reallocationsApproved().length)),
  );

  // ── Reallocations aggregation ──
  reallocationsPending = computed(() =>
    this.reallocations().filter(r => r.status === ReallocationStatus.Pending),
  );
  reallocationsApproved = computed(() =>
    this.reallocations().filter(r => r.status === ReallocationStatus.Approved),
  );
  reallocationsTotalOMR = computed(() =>
    this.reallocations().reduce((s, r) => s + (r.amountOMR ?? 0), 0),
  );
  allReallocationsApproved = computed(() => {
    const all = this.reallocations();
    if (all.length === 0) return true;   // none generated → vacuously approved
    return all.every(r => r.status === ReallocationStatus.Approved);
  });

  scenarioPillCss = computed(() => {
    switch (this.course()?.fundingScenario) {
      case FundingScenario.FundingSourceCoversAll:    return 'scenario-pill scenario-1';
      case FundingScenario.FundingSourceCoversCourse: return 'scenario-pill scenario-2';
      case FundingScenario.FinancialItemsCoverAll:    return 'scenario-pill scenario-3';
      default: return 'scenario-pill';
    }
  });

  scenarioLabel = computed(() => {
    switch (this.course()?.fundingScenario) {
      case FundingScenario.FundingSourceCoversAll:    return this.l.t('::Training.Payments.Reallocation.Scenario1');
      case FundingScenario.FundingSourceCoversCourse: return this.l.t('::Training.Payments.Reallocation.Scenario2');
      case FundingScenario.FinancialItemsCoverAll:    return this.l.t('::Training.Payments.Reallocation.Scenario3');
      default: return '—';
    }
  });

  // ── Lifecycle complete banner ──
  allConfirmed = computed(() => {
    const courseConfirmed = this.coursePayment()?.status === PaymentStatus.Confirmed;
    const travelOk = this.isInternal() || this.allTravelConfirmed();
    return courseConfirmed && travelOk;
  });

  lifecycleComplete = computed(() => this.allConfirmed() && this.allReallocationsApproved());

  totalLifecycleOMR = computed(() =>
    this.travelTotalOMR() + (this.coursePayment()?.invoiceAmountOMR ?? 0),
  );

  // ── Summary line for collapsed state ──
  summaryLine = computed(() => {
    const parts: string[] = [];
    const cp = this.coursePayment();
    if (cp) {
      if (cp.status === PaymentStatus.Confirmed) {
        parts.push(this.l.t('::Training.Payments.Section5.SummaryCourseConfirmed'));
      } else {
        parts.push(`${this.l.t('::Training.Payments.Section5.SummaryCoursePrefix')} ${this.coursePaymentBadge().text}`);
      }
    }
    if (this.showTravelCard()) {
      const c = this.travelConfirmedCount();
      const e = this.travelExpectedCount();
      if (e > 0) {
        parts.push(`${this.l.t('::Training.Payments.Section5.SummaryTravelPrefix')} ${c}/${e} ${this.l.t('::Training.Payments.Section5.SummaryConfirmedSuffix')}`);
      }
    }
    const reallocCount = this.reallocations().length;
    if (reallocCount > 0) {
      const approved = this.reallocationsApproved().length;
      parts.push(`${this.l.t('::Training.Payments.Section5.SummaryReallocPrefix')} ${approved}/${reallocCount} ${this.l.t('::Training.Payments.Section5.SummaryApprovedSuffix')}`);
    }
    parts.push(`${this.l.t('::Training.Payments.Section5.SummaryTotalPrefix')} ${this.formatOMR(this.totalLifecycleOMR())} ر.ع`);
    return parts.join(' · ');
  });

  isExpandable = computed(() => this.state() !== 'locked');

  // ── Display helpers ──
  formatOMR(value: number | null | undefined): string {
    return (value ?? 0).toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 });
  }

  toUSD(omr: number | null | undefined): string {
    return ((omr ?? 0) * this.exchangeRate()).toLocaleString('en-US', { maximumFractionDigits: 0 });
  }

  varianceText(v: number): string {
    if (Math.abs(v) < 0.001) return this.l.t('::Training.Payments.CoursePayment.VarianceEqual');
    const sign = v > 0 ? '+' : '−';
    return `${sign}${this.formatOMR(Math.abs(v))} ر.ع`;
  }

  varianceCss(v: number): string {
    if (Math.abs(v) < 0.001) return 'variance-equal';
    if (v < 0) return 'variance-down';
    return 'variance-up';
  }

  paymentStatusText(s: PaymentStatus | undefined): string {
    switch (s) {
      case PaymentStatus.Confirmed: return '✓';
      case PaymentStatus.Cancelled: return '✕';
      default: return '📝';
    }
  }

  reallocationStatusBadge(s: ReallocationStatus | undefined): { text: string; css: string } {
    if (s === ReallocationStatus.Approved) return { text: this.l.t('::Training.Payments.Reallocation.StatusApproved'), css: 'badge-approved' };
    return { text: this.l.t('::Training.Payments.Reallocation.StatusPending'), css: 'badge-pending' };
  }

  // ── Header click ──
  onHeaderClick(): void {
    if (this.state() === 'locked') return;
    this.toggle.emit();
  }

  // ── Shortcut navigation ──
  goTravelAllowances(event: Event): void {
    event.stopPropagation();
    void this.router.navigate(['/training/payments/travel-allowances']);
  }

  goCoursePayments(event: Event): void {
    event.stopPropagation();
    void this.router.navigate(['/training/payments/courses']);
  }

  goReallocations(event: Event): void {
    event.stopPropagation();
    void this.router.navigate(['/training/payments/reallocations']);
  }
}
