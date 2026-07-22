import { Component, input } from '@angular/core';

import type { CasualCourseDetailDto } from 'src/app/proxy/training/casual-courses/dtos/models';
import type { PriceQuoteDto } from 'src/app/proxy/training/finance/dtos/models';
import type {
  CoursePaymentDto,
  TravelAllowancePaymentDto,
} from 'src/app/proxy/training/payments/dtos/models';

import { SessionSectionPaymentsComponent } from '../../../../sessions/session-detail/sections/section-payments/session-section-payments.component';
import type { SectionState } from '../section-details/casual-course-section-details.component';

/**
 * Casual-course adapter for the shared execution-payment experience.
 * The payment component remains polymorphic at the API boundary: it sends a
 * CasualCourseId here and a SessionId when hosted by an annual-plan session.
 */
@Component({
  standalone: true,
  selector: 'app-casual-course-section-payments',
  templateUrl: './casual-course-section-payments.component.html',
  styleUrls: ['./casual-course-section-payments.component.scss'],
  imports: [SessionSectionPaymentsComponent],
})
export class CasualCourseSectionPaymentsComponent {
  readonly state = input.required<SectionState>();
  readonly course = input<CasualCourseDetailDto | null>(null);
  readonly selectedQuote = input<PriceQuoteDto | null>(null);
  readonly travelAllowancePayments = input<TravelAllowancePaymentDto[]>([]);
  readonly coursePayment = input<CoursePaymentDto | null>(null);
  readonly lockReason = input<string>('');
}
