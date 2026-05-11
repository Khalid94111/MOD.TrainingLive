import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter, firstValueFrom } from 'rxjs';

import { CasualCourseService } from 'src/app/proxy/training/casual-courses';
import type { CasualCourseDetailDto } from 'src/app/proxy/training/casual-courses/dtos/models';

import { CasualCourseStatus } from '../../shared';
import { CasualCourseRequestComponent } from '../casual-course-request/casual-course-request.component';
import { CasualCourseReviewComponent } from '../casual-course-review/casual-course-review.component';
import { CasualCourseApprovalComponent } from '../casual-course-approval/casual-course-approval.component';

type Variant = 'request' | 'review' | 'approval' | 'unknown';

/**
 * Dispatcher for the "Details" tab inside the casual-course shell.
 *
 * The same URL — /training/casual-courses/:id/details — serves every status.
 * This component loads the course once, then renders one of three existing
 * components inline based on the workflow stage:
 *
 *   request  → Draft, ReturnedToCreator           (UTM edits + submits)
 *   approval → Submitted, StaffReviewed, TDApproved (UGM/TD/TH approve)
 *   review   → UGMApproved, UnderReview, THApproved, Rejected (Staff/read-only)
 *
 * Each inner component runs in embedded mode (no own page-toolbar — the shell's
 * header serves the whole detail page).
 */
@Component({
  standalone: true,
  selector: 'app-casual-course-details-tab',
  templateUrl: './casual-course-details-tab.component.html',
  styleUrls: ['./casual-course-details-tab.component.scss'],
  imports: [
    CommonModule,
    CasualCourseRequestComponent,
    CasualCourseReviewComponent,
    CasualCourseApprovalComponent,
  ],
})
export class CasualCourseDetailsTabComponent implements OnInit {
  private courseService = inject(CasualCourseService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  CasualCourseStatus = CasualCourseStatus;

  id = signal<string>('');
  course = signal<CasualCourseDetailDto | null>(null);
  loading = signal<boolean>(false);

  variant = computed<Variant>(() => {
    const s = this.course()?.status;
    if (s === undefined) return 'unknown';
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

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.id.set(id);
    void this.loadCourse();

    this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => {
        const currentId = this.route.snapshot.paramMap.get('id') ?? '';
        if (currentId && currentId === this.id()) void this.loadCourse();
      });
  }

  private async loadCourse(): Promise<void> {
    if (!this.id()) return;
    this.loading.set(true);
    try {
      const detail = await firstValueFrom(this.courseService.getDetail(this.id()));
      this.course.set(detail);
    } catch {
      this.course.set(null);
    } finally {
      this.loading.set(false);
    }
  }
}
