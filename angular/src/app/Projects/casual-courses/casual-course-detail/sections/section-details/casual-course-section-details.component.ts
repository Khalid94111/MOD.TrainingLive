import { Component, computed, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { CasualCourseStatus, CourseType, TrainingLocalizationHelper } from '../../../../shared';
import type { CasualCourseDetailDto } from 'src/app/proxy/training/casual-courses/dtos/models';

import { CasualCourseRequestComponent } from '../../../casual-course-request/casual-course-request.component';
import { CasualCourseApprovalComponent } from '../../../casual-course-approval/casual-course-approval.component';
import { CasualCourseReviewComponent } from '../../../casual-course-review/casual-course-review.component';

export type SectionState = 'collapsed' | 'active' | 'locked';

type ActiveVariant = 'request' | 'approval' | 'review' | 'unknown';

/**
 * Section 1 — Course Details.
 *
 * Three visual states:
 *   collapsed: header-only with one-line summary
 *   active:    full content; dispatches to request/approval/review by status
 *   locked:    greyed placeholder + reason text
 *
 * The active variant reuses the existing standalone components by inlining
 * them. Embedded mode is signalled via the parent route's
 * `data: { embedded: true }`, which the inner components inherit through
 * their injected ActivatedRoute.
 */
@Component({
  standalone: true,
  selector: 'app-casual-course-section-details',
  templateUrl: './casual-course-section-details.component.html',
  styleUrls: [
    './casual-course-section-details.component.scss',
    '../../../../shared/gtms-design.scss',
  ],
  imports: [
    CommonModule,
    CasualCourseRequestComponent,
    CasualCourseApprovalComponent,
    CasualCourseReviewComponent,
  ],
})
export class CasualCourseSectionDetailsComponent {
  l = inject(TrainingLocalizationHelper);

  CourseType = CourseType;
  CasualCourseStatus = CasualCourseStatus;

  state = input.required<SectionState>();
  course = input<CasualCourseDetailDto | null>(null);
  /** Why the section is locked (shown in locked-preview state). */
  lockReason = input<string>('');

  toggle = output<void>();
  shortcutClick = output<string>();

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

  courseTypeLabel = computed(() => {
    switch (this.course()?.courseType) {
      case CourseType.Internal:              return 'داخلية';
      case CourseType.ExternalLocal:         return 'خارجية محلية';
      case CourseType.ExternalInternational: return 'خارجية دولية';
      default:                               return '';
    }
  });

  /** "PMP · External · 5 days · Apr 15-19 · 2 nominees" */
  summaryLine = computed(() => {
    const c = this.course();
    if (!c) return '';
    const parts: string[] = [];
    if (c.courseNameAr) parts.push(c.courseNameAr);
    const ct = this.courseTypeLabel();
    if (ct) parts.push(ct);
    const days = c.durationDays ?? 0;
    if (days > 0) parts.push(`${days} يوم`);
    const range = this.formatDateRange(c.estimatedDateFrom, c.estimatedDateTo);
    if (range) parts.push(range);
    const n = c.nomineesCount ?? c.nominations?.length ?? 0;
    if (n > 0) parts.push(`${n} مرشح`);
    return parts.join(' · ');
  });

  actualDatesLine = computed(() => {
    const c = this.course();
    if (!c?.actualStartDate || !c.actualEndDate) return '';
    const range = this.formatDateRange(c.actualStartDate, c.actualEndDate);
    return range ? `التواريخ الفعلية: ${range}` : '';
  });

  isExpandable = computed(() => this.state() !== 'locked');

  onHeaderClick(): void {
    if (this.state() === 'locked') return;
    this.toggle.emit();
  }

  onShortcut(anchor: string, event: Event): void {
    event.stopPropagation();
    this.shortcutClick.emit(anchor);
  }

  private formatDateRange(from?: string | null, to?: string | null): string {
    if (!from || !to) return '';
    const f = from.substring(0, 10);
    const t = to.substring(0, 10);
    return f === t ? f : `${f} → ${t}`;
  }
}
