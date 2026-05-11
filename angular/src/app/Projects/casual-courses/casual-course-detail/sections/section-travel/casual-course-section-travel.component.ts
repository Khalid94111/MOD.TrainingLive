import { Component, computed, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { CasualCourseStatus, TrainingLocalizationHelper } from '../../../../shared';
import type { CasualCourseDetailDto } from 'src/app/proxy/training/casual-courses/dtos/models';
import type { TravelInstructionDto } from 'src/app/proxy/training/execution/dtos/models';
import { TravelInstructionStatus } from 'src/app/proxy/training/enums/travel-instruction-status.enum';

import { CasualCourseTravelInstructionsComponent } from '../../../casual-course-travel-instructions/casual-course-travel-instructions.component';

import type { SectionState } from '../section-details/casual-course-section-details.component';

/**
 * Section 4 — Travel Instructions.
 *
 * Active body embeds the existing casual-course-travel-instructions component.
 * Hidden entirely for `CourseType.Internal` — handled by the shell.
 */
@Component({
  standalone: true,
  selector: 'app-casual-course-section-travel',
  templateUrl: './casual-course-section-travel.component.html',
  styleUrls: [
    './casual-course-section-travel.component.scss',
    '../../../../shared/gtms-design.scss',
  ],
  imports: [
    CommonModule,
    CasualCourseTravelInstructionsComponent,
  ],
})
export class CasualCourseSectionTravelComponent {
  l = inject(TrainingLocalizationHelper);

  CasualCourseStatus = CasualCourseStatus;
  TravelInstructionStatus = TravelInstructionStatus;

  state = input.required<SectionState>();
  course = input<CasualCourseDetailDto | null>(null);
  instruction = input<TravelInstructionDto | null>(null);
  lockReason = input<string>('');

  toggle = output<void>();

  statusLabel = computed(() => {
    const s = this.instruction()?.status;
    switch (s) {
      case TravelInstructionStatus.Issued:    return 'مُصدَرة';
      case TravelInstructionStatus.Cancelled: return 'ملغاة';
      case TravelInstructionStatus.Draft:     return 'مسودة';
      default: return 'لم تُنشأ بعد';
    }
  });

  /** "مُصدَرة · 7 أيام سفر · ✓ التذاكر محجوزة" */
  summaryLine = computed(() => {
    const ti = this.instruction();
    const parts: string[] = [];
    parts.push(this.statusLabel());
    if (ti) {
      const days = ti.effectiveTravelDays ?? 0;
      if (days > 0) parts.push(`${days} ${days === 1 ? 'يوم سفر' : 'أيام سفر'}`);
      if (ti.ticketsBooked) parts.push('✓ التذاكر محجوزة');
      else if (ti.status === TravelInstructionStatus.Issued) parts.push('— التذاكر غير محجوزة');
    }
    return parts.join(' · ');
  });

  isExpandable = computed(() => this.state() !== 'locked');

  onHeaderClick(): void {
    if (this.state() === 'locked') return;
    this.toggle.emit();
  }
}
