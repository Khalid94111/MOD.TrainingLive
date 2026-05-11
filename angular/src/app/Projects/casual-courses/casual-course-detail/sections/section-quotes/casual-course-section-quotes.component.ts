import { Component, computed, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { CasualCourseStatus, TrainingLocalizationHelper } from '../../../../shared';
import type { CasualCourseDetailDto } from 'src/app/proxy/training/casual-courses/dtos/models';
import type { PriceQuoteDto } from 'src/app/proxy/training/finance/dtos/models';

import { CasualCoursePriceQuotesComponent } from '../../../casual-course-price-quotes/casual-course-price-quotes.component';

import type { SectionState } from '../section-details/casual-course-section-details.component';

/**
 * Section 3 — Price Quotes.
 *
 * Active body embeds the existing casual-course-price-quotes component
 * (full quote workflow: list, add/edit, pick winner). Inputs come from
 * route data inheritance — `embedded: true` on the shell route, parent id
 * from the parent route's paramMap.
 *
 * Hidden entirely for `CourseType.Internal` — handled by the shell, this
 * component is simply not rendered.
 */
@Component({
  standalone: true,
  selector: 'app-casual-course-section-quotes',
  templateUrl: './casual-course-section-quotes.component.html',
  styleUrls: [
    './casual-course-section-quotes.component.scss',
    '../../../../shared/gtms-design.scss',
  ],
  imports: [
    CommonModule,
    CasualCoursePriceQuotesComponent,
  ],
})
export class CasualCourseSectionQuotesComponent {
  l = inject(TrainingLocalizationHelper);

  CasualCourseStatus = CasualCourseStatus;

  state = input.required<SectionState>();
  course = input<CasualCourseDetailDto | null>(null);
  quoteCount = input<number>(0);
  selectedQuote = input<PriceQuoteDto | null>(null);
  lockReason = input<string>('');

  toggle = output<void>();

  hasWinner = computed(() => !!this.course()?.selectedPriceQuoteId);

  /** "3 عروض · المعهد الدولي · 5,400 ر.ع · ✓ الفائز" or "3 عروض · لم يُختر بعد". */
  summaryLine = computed(() => {
    const c = this.course();
    if (!c) return '';
    const parts: string[] = [];
    const n = this.quoteCount();
    if (n > 0) parts.push(`${n} ${n === 1 ? 'عرض' : 'عروض'}`);
    else parts.push('لا توجد عروض بعد');

    const winner = this.selectedQuote();
    if (winner) {
      if (winner.providerName) parts.push(winner.providerName);
      const price = winner.quotedPriceOMR;
      if (price !== null && price !== undefined && price > 0) {
        parts.push(`${this.formatNumber(price)} ر.ع`);
      }
      parts.push('✓ الفائز');
    } else if (n > 0) {
      parts.push('لم يُختر عرض بعد');
    }
    return parts.join(' · ');
  });

  /** Supplementary line shown under the summary when a winner is locked
   *  in: "تواريخ الانعقاد: 2026-05-15 → 2026-05-19". */
  actualDatesLine = computed(() => {
    const c = this.course();
    if (!c?.actualStartDate || !c.actualEndDate) return '';
    const f = c.actualStartDate.substring(0, 10);
    const t = c.actualEndDate.substring(0, 10);
    return f === t ? `تواريخ الانعقاد: ${f}` : `تواريخ الانعقاد: ${f} → ${t}`;
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
