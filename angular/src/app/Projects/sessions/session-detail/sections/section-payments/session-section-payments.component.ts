import { Component, computed, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { Router } from '@angular/router';

import type { CourseSessionDetailDto } from 'src/app/proxy/training/plans/dtos/models';
import type {
  TravelAllowancePaymentDto,
  CoursePaymentDto,
} from 'src/app/proxy/training/payments/dtos/models';
import { CourseType } from 'src/app/proxy/training/enums/course-type.enum';
import { PaymentStatus } from 'src/app/proxy/training/enums/payment-status.enum';

import { TrainingLocalizationHelper } from '../../../../shared';

// Phase 4C-α (v4.10.0) — Section 5 (Payments) for session detail.
//
// Mirrors the casual-course Section 5 layout but **without the reallocation card**:
// sessions never generate BudgetReallocations (§8 non-regression rule). Two summary
// cards: Travel Allowance aggregate + Course Payment summary. Both delegate the
// actual workflow to the existing /training/payments/* list pages via shortcut buttons.
@Component({
  selector: 'app-session-section-payments',
  standalone: true,
  templateUrl: './session-section-payments.component.html',
  styleUrls: ['../../../../shared/gtms-design.scss', './session-section-payments.component.scss'],
  imports: [CommonModule, LocalizationPipe],
})
export class SessionSectionPaymentsComponent {
  l = inject(TrainingLocalizationHelper);
  private router = inject(Router);

  CourseType = CourseType;
  PaymentStatus = PaymentStatus;

  session = input<CourseSessionDetailDto | null>(null);
  travelAllowancePayments = input<TravelAllowancePaymentDto[]>([]);
  coursePayment = input<CoursePaymentDto | null>(null);

  // ── Course-type guards ──
  isInternal = computed(() => this.session()?.courseType === CourseType.Internal);
  // Patch 5 (v4.10.5) — travel allowances apply only to ExternalInternational courses.
  // Internal and ExternalLocal have no travel instruction → no allowances to pay.
  showTravelCard = computed(
    () => this.session()?.courseType === CourseType.ExternalInternational,
  );

  // ── Travel allowances rollup ──
  expectedNomineeCount = computed(() => {
    // Detail DTO exposes nominations list directly.
    const noms = this.session()?.nominations;
    return noms?.length ?? 0;
  });
  travelConfirmedCount = computed(() =>
    this.travelAllowancePayments().filter(p => p.status === PaymentStatus.Confirmed).length,
  );
  travelTotalOMR = computed(() =>
    this.travelAllowancePayments().reduce((s, p) => s + (p.totalOMR ?? 0), 0),
  );
  travelStatusBadge = computed(() => {
    if (this.isInternal()) {
      return { textKey: '::Training.Payments.Section5.TravelBadgeNotRequired', css: 'badge-muted' };
    }
    const confirmed = this.travelConfirmedCount();
    const expected = this.expectedNomineeCount();
    if (expected === 0) {
      return { textKey: '::Training.Payments.Section5.TravelBadgeNoNominees', css: 'badge-muted' };
    }
    if (confirmed === expected && expected > 0) {
      return { textLiteral: `${confirmed}/${expected} ✓`, css: 'badge-confirmed' };
    }
    if (confirmed > 0) {
      return { textLiteral: `${confirmed}/${expected}`, css: 'badge-pending' };
    }
    return { textLiteral: `0/${expected}`, css: 'badge-empty' };
  });

  // ── Course payment summary ──
  coursePaymentBadge = computed(() => {
    const cp = this.coursePayment();
    if (!cp) return { textKey: '::Training.Payments.Section5.CourseEmptyBadge', css: 'badge-empty' };
    if (cp.status === PaymentStatus.Confirmed) return { textKey: '::Training.Payments.Section5.CourseConfirmedBadge', css: 'badge-confirmed' };
    if (cp.status === PaymentStatus.Cancelled) return { textKey: '::Training.Payments.Section5.CourseCancelledBadge', css: 'badge-cancelled' };
    return { textKey: '::Training.Payments.Section5.CourseDraftBadge', css: 'badge-pending' };
  });

  // ── Aggregate "lifecycle complete" ──
  // Travel is "vacuously confirmed" when the card is hidden (Internal + ExternalLocal).
  allTravelConfirmed = computed(() => {
    if (!this.showTravelCard()) return true;
    const expected = this.expectedNomineeCount();
    return expected > 0 && this.travelConfirmedCount() === expected;
  });
  coursePaymentConfirmed = computed(() => this.coursePayment()?.status === PaymentStatus.Confirmed);
  lifecycleComplete = computed(() => this.allTravelConfirmed() && this.coursePaymentConfirmed());

  totalLifecycleOMR = computed(() =>
    this.travelTotalOMR() + (this.coursePayment()?.invoiceAmountOMR ?? 0),
  );

  formatOMR(value: number | null | undefined): string {
    return (value ?? 0).toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 });
  }

  goTravelAllowances(event: Event): void {
    event.stopPropagation();
    void this.router.navigate(['/training/payments/travel-allowances']);
  }

  goCoursePayments(event: Event): void {
    event.stopPropagation();
    void this.router.navigate(['/training/payments/courses']);
  }
}
