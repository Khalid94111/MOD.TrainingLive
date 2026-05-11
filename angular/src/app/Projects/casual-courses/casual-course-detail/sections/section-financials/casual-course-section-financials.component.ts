import { Component, computed, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import {
  CasualCourseStatus,
  FUNDING_SCENARIO_OPTIONS,
  TrainingLocalizationHelper,
} from '../../../../shared';
import type { CasualCourseDetailDto } from 'src/app/proxy/training/casual-courses/dtos/models';

import { CasualCourseRequestComponent } from '../../../casual-course-request/casual-course-request.component';
import { CasualCourseApprovalComponent } from '../../../casual-course-approval/casual-course-approval.component';
import { CasualCourseReviewComponent } from '../../../casual-course-review/casual-course-review.component';

import type { SectionState } from '../section-details/casual-course-section-details.component';

type ActiveVariant = 'request' | 'approval' | 'review' | 'unknown';

/**
 * Section 2 — Financials.
 *
 * Active body dispatches by status to the same three components used in
 * Section 1, but with `[mode]="'financials'"` so only the calculator
 * preview / scenario picker / financial items table render.
 *
 * Collapsed summary line varies by status:
 *   pre-UGMApproved  : informational ("متاح للحساب · n مرشح · d يوم")
 *   UGMApproved+     : "السيناريو N · totalOMR ر.ع · n بنود"
 */
@Component({
  standalone: true,
  selector: 'app-casual-course-section-financials',
  templateUrl: './casual-course-section-financials.component.html',
  styleUrls: [
    './casual-course-section-financials.component.scss',
    '../../../../shared/gtms-design.scss',
  ],
  imports: [
    CommonModule,
    CasualCourseRequestComponent,
    CasualCourseApprovalComponent,
    CasualCourseReviewComponent,
  ],
})
export class CasualCourseSectionFinancialsComponent {
  l = inject(TrainingLocalizationHelper);

  CasualCourseStatus = CasualCourseStatus;

  state = input.required<SectionState>();
  course = input<CasualCourseDetailDto | null>(null);
  /** Why the section is locked (shown in locked-preview state). */
  lockReason = input<string>('');

  toggle = output<void>();

  variant = computed<ActiveVariant>(() => {
    const s = this.course()?.status;
    if (s === undefined) return 'request';
    switch (s) {
      case CasualCourseStatus.Draft:
      case CasualCourseStatus.ReturnedToCreator:
        return 'request';
      case CasualCourseStatus.Submitted:
      case CasualCourseStatus.StaffReviewed:
      case CasualCourseStatus.TDApproved:
        return 'approval';
      case CasualCourseStatus.UGMApproved:
      case CasualCourseStatus.UnderReview:
      case CasualCourseStatus.THApproved:
      case CasualCourseStatus.Rejected:
        return 'review';
      default:
        return 'unknown';
    }
  });

  scenarioLabel = computed(() => {
    const s = this.course()?.fundingScenario;
    if (s === null || s === undefined) return '';
    const opt = FUNDING_SCENARIO_OPTIONS.find(o => o.value === s);
    return opt ? this.l.t(opt.key) : '';
  });

  scenarioNum = computed(() => {
    const s = this.course()?.fundingScenario;
    return s === null || s === undefined ? null : Number(s);
  });

  /** "السيناريو 2 · 5,580 ر.ع · 4 بنود" or "احتساب مبدئي" pre-scenario. */
  summaryLine = computed(() => {
    const c = this.course();
    if (!c) return '';
    const s = c.status;
    const parts: string[] = [];

    const isPostUgm = s !== undefined &&
      s !== CasualCourseStatus.Draft &&
      s !== CasualCourseStatus.Submitted &&
      s !== CasualCourseStatus.ReturnedToCreator;

    if (!isPostUgm) {
      parts.push('احتساب مبدئي');
      if ((c.nomineesCount ?? 0) > 0) parts.push(`${c.nomineesCount} مرشح`);
      if ((c.durationDays ?? 0) > 0) parts.push(`${c.durationDays} يوم`);
      return parts.join(' · ');
    }

    const num = this.scenarioNum();
    if (num !== null) {
      parts.push(`السيناريو ${num}`);
    } else {
      parts.push('بدون سيناريو');
    }
    const total = c.estimatedTotalCost ?? 0;
    if (total > 0) parts.push(`${this.formatNumber(total)} ر.ع`);
    const n = c.financialItemsCount ?? 0;
    if (n > 0) parts.push(`${n} بند`);
    if (s === CasualCourseStatus.THApproved) parts.push('✓ معتمد');
    return parts.join(' · ');
  });

  isExpandable = computed(() => this.state() !== 'locked');

  onHeaderClick(): void {
    if (this.state() === 'locked') return;
    this.toggle.emit();
  }

  private formatNumber(n: number): string {
    return n.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 3 });
  }
}
