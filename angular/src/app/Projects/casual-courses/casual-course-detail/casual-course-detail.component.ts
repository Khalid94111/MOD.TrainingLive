import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { PermissionService } from '@abp/ng.core';
import { filter, firstValueFrom } from 'rxjs';

import { CasualCourseService } from 'src/app/proxy/training/casual-courses';
import type { CasualCourseDetailDto } from 'src/app/proxy/training/casual-courses/dtos/models';
import { PriceQuoteService } from 'src/app/proxy/training/finance';
import type { PriceQuoteDto } from 'src/app/proxy/training/finance/dtos/models';
import { TravelInstructionService } from 'src/app/proxy/training/execution/travel-instruction.service';
import type { TravelInstructionDto } from 'src/app/proxy/training/execution/dtos/models';
import { TravelInstructionStatus } from 'src/app/proxy/training/enums/travel-instruction-status.enum';
import { TravelAllowancePaymentService } from 'src/app/proxy/training/payments/travel-allowance-payment.service';
import { CoursePaymentService } from 'src/app/proxy/training/payments/course-payment.service';
import { BudgetReallocationService } from 'src/app/proxy/training/payments/budget-reallocation.service';
import type {
  TravelAllowancePaymentDto,
  CoursePaymentDto,
  BudgetReallocationDto,
} from 'src/app/proxy/training/payments/dtos/models';
import { PaymentStatus } from 'src/app/proxy/training/enums/payment-status.enum';
import { ReallocationStatus } from 'src/app/proxy/training/enums/reallocation-status.enum';
import { ExchangeRateService } from '../../shared/services/finance-proxy.service';

import {
  CASUAL_COURSE_STATUS_OPTIONS,
  CasualCourseStatus,
  CourseInfoBarComponent,
  type CourseInfoBarData,
  CourseType,
  TrainingLocalizationHelper,
} from '../../shared';
import { CasualCourseDetailRefreshService } from './casual-course-detail-refresh.service';
import {
  CasualCourseStatusPipelineComponent,
  type PipelineNode,
} from './status-pipeline/casual-course-status-pipeline.component';
import {
  CasualCourseSectionDetailsComponent,
  type SectionState,
} from './sections/section-details/casual-course-section-details.component';
import { CasualCourseSectionFinancialsComponent } from './sections/section-financials/casual-course-section-financials.component';
import { CasualCourseSectionQuotesComponent } from './sections/section-quotes/casual-course-section-quotes.component';
import { CasualCourseSectionTravelComponent } from './sections/section-travel/casual-course-section-travel.component';
import { CasualCourseSectionPaymentsComponent } from './sections/section-payments/casual-course-section-payments.component';
import { CasualCourseHeaderActionBarComponent } from './header-action-bar/casual-course-header-action-bar.component';

type SectionKey = 'details' | 'financials' | 'quotes' | 'travel' | 'payments';

/**
 * Casual Course Detail — stage-based progressive disclosure shell.
 *
 * Authoritative spec: docs/GTMS-Phase4B-Alpha-Frontend-Layout.md
 *
 * Layout: sticky header + sticky 8-node status pipeline + 4 accordion sections
 * (Course Details, Financials, Price Quotes, Travel Instructions). Each section
 * computes its own state — collapsed-summary, expanded-active, or locked-preview —
 * from `(course.status × current user role × section conditions)`.
 *
 * NO router-outlet. NO child routes. Single URL `/training/casual-courses/:id`,
 * with hash anchors `#financials`, `#quotes`, `#travel` for deep linking.
 *
 * This iteration ships the shell + Section 1 (Course Details). Sections 2-4
 * render as inline locked-preview stubs pending their dedicated components.
 */
@Component({
  standalone: true,
  selector: 'app-casual-course-detail',
  templateUrl: './casual-course-detail.component.html',
  styleUrls: ['./casual-course-detail.component.scss', '../../shared/gtms-design.scss'],
  imports: [
    CommonModule,
    CasualCourseStatusPipelineComponent,
    CasualCourseHeaderActionBarComponent,
    CourseInfoBarComponent,
    CasualCourseSectionDetailsComponent,
    CasualCourseSectionFinancialsComponent,
    CasualCourseSectionQuotesComponent,
    CasualCourseSectionTravelComponent,
    CasualCourseSectionPaymentsComponent,
  ],
})
export class CasualCourseDetailComponent implements OnInit {
  private courseService = inject(CasualCourseService);
  private quoteService = inject(PriceQuoteService);
  private travelService = inject(TravelInstructionService);
  private travelPaymentService = inject(TravelAllowancePaymentService);
  private coursePaymentService = inject(CoursePaymentService);
  private reallocationService = inject(BudgetReallocationService);
  private exchangeService = inject(ExchangeRateService);
  private permissions = inject(PermissionService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private refresh = inject(CasualCourseDetailRefreshService);
  l = inject(TrainingLocalizationHelper);

  CourseType = CourseType;
  CasualCourseStatus = CasualCourseStatus;

  id = signal<string>('');
  course = signal<CasualCourseDetailDto | null>(null);
  quoteCount = signal<number>(0);
  selectedQuote = signal<PriceQuoteDto | null>(null);
  travelInstruction = signal<TravelInstructionDto | null>(null);
  travelAllowancePayments = signal<TravelAllowancePaymentDto[]>([]);
  coursePayment = signal<CoursePaymentDto | null>(null);
  reallocations = signal<BudgetReallocationDto[]>([]);
  exchangeRate = signal<number>(2.6);
  loading = signal<boolean>(false);

  // Phase 4C-α Patch 1 — feeds <app-course-info-bar variant="casual">.
  // Null until the course loads; the template skips rendering the bar in that case.
  infoBarData = computed<CourseInfoBarData | null>(() => {
    const c = this.course();
    if (!c) return null;
    return {
      courseName: c.courseNameAr || '',
      unitName: c.unitName || null,
      durationDays: c.durationDays ?? 0,
      nomineesCount: c.nomineesCount ?? 0,
      officersCount: c.officersCount ?? 0,
      enlistedCount: c.enlistedCount ?? 0,
      approvedCostOMR: c.estimatedTotalCost ?? 0,
      fundingScenarioLabel: c.fundingScenarioLabel ?? null,
    };
  });

  /** Section a user explicitly expanded (overrides default-active). */
  private userExpanded = signal<SectionKey | null>(null);
  /** Sections the user explicitly collapsed even though they would default to active. */
  private userCollapsed = signal<Set<SectionKey>>(new Set());

  // ── Permissions ────────────────────────────────────────────────────
  canEditDraft = computed(() =>
    this.permissions.getGrantedPolicy('Training.CasualCourses.Edit'),
  );
  canApproveUgm = computed(() =>
    this.permissions.getGrantedPolicy('Training.CasualCourses.Approve'),
  );
  canReview = computed(() =>
    this.permissions.getGrantedPolicy('Training.CasualCourses.Review'),
  );
  canTdApprove = computed(() =>
    this.permissions.getGrantedPolicy('Training.CasualCourses.TDApprove'),
  );
  canHeadApprove = computed(() =>
    this.permissions.getGrantedPolicy('Training.CasualCourses.HeadApprove'),
  );

  // ── Course summary computeds ───────────────────────────────────────
  isInternal = computed(() => this.course()?.courseType === CourseType.Internal);
  // Patch 4 (v4.10.4) — ExternalLocal: provider is in-country, so Section 4 is hidden
  // and Section 5 doesn't gate on TravelInstruction.
  isLocal = computed(() => this.course()?.courseType === CourseType.ExternalLocal);
  hasSelectedQuote = computed(() => !!this.course()?.selectedPriceQuoteId);
  status = computed(() => this.course()?.status);

  /** /new mode — no course saved yet. Treated as "Draft for UTM" by the
   *  default-state matrix (Section 1 active, Sections 2-4 locked). */
  isNewMode = computed(() => !this.id());

  hasNominees = computed(() => {
    const c = this.course();
    return (c?.nomineesCount ?? c?.nominations?.length ?? 0) > 0;
  });

  statusBadge = computed(() => {
    const s = this.status();
    return CASUAL_COURSE_STATUS_OPTIONS.find(o => o.value === s);
  });

  // ── Section state matrix (per spec §"Section state matrix") ────────
  /** Default state computed from status × role; user toggles override below. */
  private detailsDefaultState = computed<SectionState>(() => {
    const s = this.status();
    if (s === undefined) return 'active';
    if (s === CasualCourseStatus.Draft || s === CasualCourseStatus.ReturnedToCreator) {
      return this.canEditDraft() ? 'active' : 'collapsed';
    }
    if (s === CasualCourseStatus.Submitted) {
      return this.canApproveUgm() ? 'active' : 'collapsed';
    }
    return 'collapsed';
  });

  detailsState = computed<SectionState>(() => {
    const def = this.detailsDefaultState();
    return this.applyUserToggle('details', def);
  });

  /** Per spec §"Section state matrix" + Create-mode addendum.
   *  Default-active so the review/approval component is mounted and ready
   *  to receive header-bar actions (Start Review, Save Progress, etc).
   *  Sections without an active default for the current role stay
   *  collapsed; the user can still expand them manually. */
  private financialsDefaultState = computed<SectionState>(() => {
    const s = this.status();
    if (s === undefined) return 'locked'; // /new before autosave
    if (s === CasualCourseStatus.Draft || s === CasualCourseStatus.ReturnedToCreator) {
      return this.hasNominees() ? 'collapsed' : 'locked';
    }
    if (s === CasualCourseStatus.Submitted) return 'collapsed';
    // UGMApproved + Staff → active so the review component subscribes and
    // the header-bar "Start Review" button has a live handler.
    if (s === CasualCourseStatus.UGMApproved && this.canReview()) return 'active';
    if (s === CasualCourseStatus.UnderReview && this.canReview()) return 'active';
    if (s === CasualCourseStatus.StaffReviewed && this.canTdApprove()) return 'active';
    if (s === CasualCourseStatus.TDApproved && this.canHeadApprove()) return 'active';
    return 'collapsed';
  });

  financialsState = computed<SectionState>(() => {
    const def = this.financialsDefaultState();
    return this.applyUserToggle('financials', def);
  });

  financialsLockReason = computed(() => {
    const s = this.status();
    if (s === undefined) return 'متاح بعد حفظ الدورة بمرشحين.';
    if (
      (s === CasualCourseStatus.Draft || s === CasualCourseStatus.ReturnedToCreator) &&
      !this.hasNominees()
    ) {
      return 'متاح بعد إضافة المرشحين.';
    }
    return '';
  });

  /** Per spec §"Section state matrix" — Section 3 — Price Quotes. */
  private quotesDefaultState = computed<SectionState>(() => {
    const s = this.status();
    if (s === undefined) return 'locked';
    if (s !== CasualCourseStatus.THApproved) return 'locked';
    if (this.hasSelectedQuote()) return 'collapsed';
    return this.canReview() ? 'active' : 'collapsed';
  });

  quotesState = computed<SectionState>(() => {
    if (this.quotesHidden()) return 'locked'; // ignored — section not rendered
    const def = this.quotesDefaultState();
    return this.applyUserToggle('quotes', def);
  });

  quotesLockReason = computed(() => 'متاح بعد اعتماد رئيس التدريب.');

  /** Per spec §"Section state matrix" — Section 4 — Travel Instructions. */
  hasTravelInstruction = computed(() => {
    const s = this.travelInstruction()?.status;
    return s === TravelInstructionStatus.Issued || s === TravelInstructionStatus.Draft;
  });

  private travelDefaultState = computed<SectionState>(() => {
    const s = this.status();
    if (s !== CasualCourseStatus.THApproved) return 'locked';
    if (!this.hasSelectedQuote()) return 'locked';
    if (this.hasTravelInstruction()) return 'collapsed';
    return this.canReview() ? 'active' : 'collapsed';
  });

  travelState = computed<SectionState>(() => {
    if (this.travelHidden()) return 'locked'; // ignored — section not rendered
    const def = this.travelDefaultState();
    return this.applyUserToggle('travel', def);
  });

  travelLockReason = computed(() => {
    if (this.status() !== CasualCourseStatus.THApproved) {
      return 'متاح بعد اعتماد رئيس التدريب.';
    }
    if (!this.hasSelectedQuote()) return 'متاح بعد اختيار العرض الفائز.';
    return '';
  });

  quotesHidden = computed(() => this.isInternal());

  // Hidden for Internal AND ExternalLocal (no travel for in-country providers).
  travelHidden = computed(() => this.isInternal() || this.isLocal());

  /** Section 5 — Payments & Reallocation. Always visible after THApproved (per
   *  prompt §"Section state matrix"). External courses also require an Issued
   *  TravelInstruction; internal courses unlock directly on THApproval since
   *  there's no TI to wait on. */
  travelInstructionIssued = computed(() =>
    this.travelInstruction()?.status === TravelInstructionStatus.Issued,
  );

  allTravelPaymentsConfirmed = computed(() => {
    const expected = this.course()?.nomineesCount ?? this.course()?.nominations?.length ?? 0;
    // Internal and Local both skip travel-allowance payments (no TI, no travel).
    if (this.isInternal() || this.isLocal()) return true;
    if (expected === 0) return false;
    const confirmed = this.travelAllowancePayments()
      .filter(p => p.status === PaymentStatus.Confirmed).length;
    return confirmed === expected;
  });

  allCoursePaymentsConfirmed = computed(() =>
    this.coursePayment()?.status === PaymentStatus.Confirmed,
  );

  allReallocationsApproved = computed(() => {
    const all = this.reallocations();
    if (all.length === 0) return true;
    return all.every(r => r.status === ReallocationStatus.Approved);
  });

  allPaymentsLifecycleComplete = computed(() =>
    this.allTravelPaymentsConfirmed()
    && this.allCoursePaymentsConfirmed()
    && this.allReallocationsApproved(),
  );

  private paymentsDefaultState = computed<SectionState>(() => {
    if (this.status() !== CasualCourseStatus.THApproved) return 'locked';
    // Only ExternalInternational gates Section 5 on an Issued TI.
    if (!this.isInternal() && !this.isLocal() && !this.travelInstructionIssued()) return 'locked';
    if (this.allPaymentsLifecycleComplete()) return 'collapsed';
    return 'active';
  });

  paymentsState = computed<SectionState>(() => {
    const def = this.paymentsDefaultState();
    return this.applyUserToggle('payments', def);
  });

  /** Section 5 has cleared its preconditions — fed to the status pipeline. */
  paymentsCanStart = computed<boolean>(() => this.paymentsDefaultState() !== 'locked');

  paymentsLockReason = computed(() => {
    if (this.status() !== CasualCourseStatus.THApproved) {
      return this.l.t('::Training.Payments.Section5.LockReasonNoTH');
    }
    if (!this.isInternal() && !this.isLocal() && !this.travelInstructionIssued()) {
      return this.l.t('::Training.Payments.Section5.LockReasonNoTI');
    }
    return '';
  });

  // ── Lifecycle ──────────────────────────────────────────────────────
  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.id.set(id);

    // Starting `/new`? Reset the attach BehaviorSubject so the previous
    // session's id can't leak into the request component's ngOnInit and
    // make Save Draft target a stale (deleted/cross-tenant) course id.
    if (!id) this.refresh.clearAttachment();

    void this.loadAll();

    this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => {
        const currentId = this.route.snapshot.paramMap.get('id') ?? '';
        if (currentId && currentId !== this.id()) {
          this.id.set(currentId);
          void this.loadAll();
        }
        this.applyHashAnchor();
      });

    this.refresh.events.subscribe(() => void this.loadAll());

    // /new → /:id transition: request component fires this after autosave.
    // No router navigation; URL was already updated via history.replaceState.
    this.refresh.attachEvents.subscribe(newId => {
      if (!newId || this.id() === newId) return;
      this.id.set(newId);
      void this.loadAll();
    });

    // Apply any initial hash on first paint.
    queueMicrotask(() => this.applyHashAnchor());
  }

  // ── Data loading ───────────────────────────────────────────────────
  private async loadAll(): Promise<void> {
    if (!this.id()) return;
    this.loading.set(true);
    try {
      await Promise.all([
        this.loadCourse(),
        this.loadQuoteCount(),
        this.loadTravelInstruction(),
        this.loadPaymentsBundle(),
        this.loadExchangeRate(),
      ]);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadCourse(): Promise<void> {
    try {
      const detail = await firstValueFrom(this.courseService.getDetail(this.id()));
      this.course.set(detail);
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
      const items = result.items ?? [];
      this.selectedQuote.set(items.find(q => q.isSelected) ?? null);
    } catch {
      this.quoteCount.set(0);
      this.selectedQuote.set(null);
    }
  }

  private async loadTravelInstruction(): Promise<void> {
    try {
      const ti = await firstValueFrom(this.travelService.getByParent(this.id(), ''));
      this.travelInstruction.set(ti ?? null);
    } catch {
      this.travelInstruction.set(null);
    }
  }

  private async loadPaymentsBundle(): Promise<void> {
    const courseId = this.id();
    if (!courseId) return;
    const [travelRes, courseRes, reallocRes] = await Promise.all([
      firstValueFrom(this.travelPaymentService.getList({ casualCourseId: courseId, maxResultCount: 200 }))
        .catch(() => null),
      firstValueFrom(this.coursePaymentService.getList({ casualCourseId: courseId, maxResultCount: 10 }))
        .catch(() => null),
      firstValueFrom(this.reallocationService.getList({ casualCourseId: courseId, maxResultCount: 200 }))
        .catch(() => null),
    ]);
    this.travelAllowancePayments.set(travelRes?.items ?? []);
    // Polymorphic payment is 1:1 with course in practice; pick the most recent if many.
    const cpItems = courseRes?.items ?? [];
    this.coursePayment.set(cpItems[0] ?? null);
    this.reallocations.set(reallocRes?.items ?? []);
  }

  private async loadExchangeRate(): Promise<void> {
    try {
      const r = await this.exchangeService.getActive();
      if (r?.rate && r.rate > 0) this.exchangeRate.set(r.rate);
    } catch { /* keep default */ }
  }

  // ── Section toggle handlers ────────────────────────────────────────
  private applyUserToggle(key: SectionKey, def: SectionState): SectionState {
    if (def === 'locked') return 'locked';
    const expanded = this.userExpanded();
    if (expanded === key) return 'active';
    if (expanded && expanded !== key) return def === 'active' ? 'collapsed' : def;
    if (this.userCollapsed().has(key)) return 'collapsed';
    return def;
  }

  onDetailsToggle(): void {
    if (this.detailsState() === 'active') {
      this.collapseSection('details');
    } else {
      this.expandSection('details');
    }
  }

  onFinancialsToggle(): void {
    if (this.financialsState() === 'active') {
      this.collapseSection('financials');
    } else {
      this.expandSection('financials');
    }
  }

  onQuotesToggle(): void {
    if (this.quotesState() === 'active') {
      this.collapseSection('quotes');
    } else {
      this.expandSection('quotes');
    }
  }

  onTravelToggle(): void {
    if (this.travelState() === 'active') {
      this.collapseSection('travel');
    } else {
      this.expandSection('travel');
    }
  }

  onPaymentsToggle(): void {
    if (this.paymentsState() === 'active') {
      this.collapseSection('payments');
    } else {
      this.expandSection('payments');
    }
  }

  private expandSection(key: SectionKey): void {
    this.userExpanded.set(key);
    this.userCollapsed.update(s => {
      const next = new Set(s);
      next.delete(key);
      return next;
    });
    this.updateUrlHash(key);
  }

  private collapseSection(key: SectionKey): void {
    if (this.userExpanded() === key) this.userExpanded.set(null);
    this.userCollapsed.update(s => {
      const next = new Set(s);
      next.add(key);
      return next;
    });
  }

  // ── Hash-anchor deep linking ───────────────────────────────────────
  private applyHashAnchor(): void {
    const hash = (typeof window !== 'undefined' && window.location.hash || '').replace(/^#/, '');
    if (!hash) return;
    const map: Record<string, SectionKey> = {
      details: 'details',
      financials: 'financials',
      quotes: 'quotes',
      travel: 'travel',
      payments: 'payments',
    };
    const key = map[hash];
    if (!key) return;
    this.expandSection(key);
    queueMicrotask(() => {
      const el = document.getElementById(`section-${key}`);
      if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  }

  private updateUrlHash(key: SectionKey): void {
    if (typeof window === 'undefined') return;
    const url = new URL(window.location.href);
    url.hash = `#${key}`;
    window.history.replaceState(null, '', url.toString());
  }

  // ── Pipeline node click ────────────────────────────────────────────
  onPipelineNodeClick(node: PipelineNode): void {
    const anchor = node.anchor as SectionKey;
    if (!anchor) return;
    this.expandSection(anchor);
    queueMicrotask(() => {
      const el = document.getElementById(`section-${anchor}`);
      if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  }

  goBack(): void {
    void this.router.navigate(['/training/casual-courses']);
  }
}
