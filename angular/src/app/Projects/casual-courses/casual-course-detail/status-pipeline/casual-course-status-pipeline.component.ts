import { Component, computed, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { CasualCourseStatus, TrainingLocalizationHelper } from '../../../shared';

export type PipelineNodeKey =
  | 'draft'
  | 'submit'
  | 'ugm'
  | 'staff'
  | 'th'
  | 'quote'
  | 'travel'
  | 'payment'
  | 'execution';

export type PipelineNodeState = 'completed' | 'active' | 'pending';

export interface PipelineNode {
  key: PipelineNodeKey;
  label: string;
  state: PipelineNodeState;
  /** Hash anchor or section the node maps to (empty for header-only nodes). */
  anchor: string;
  /** Phase 5 placeholder — node visible but inert. */
  placeholder?: boolean;
}

@Component({
  standalone: true,
  selector: 'app-casual-course-status-pipeline',
  templateUrl: './casual-course-status-pipeline.component.html',
  styleUrls: ['./casual-course-status-pipeline.component.scss'],
  imports: [CommonModule],
})
export class CasualCourseStatusPipelineComponent {
  private l = inject(TrainingLocalizationHelper);

  status = input<CasualCourseStatus | undefined>(undefined);
  hasSelectedQuote = input<boolean>(false);
  hasTravelInstruction = input<boolean>(false);
  /** Section 5 unlocked (TI Issued for external courses, or THApproved for internal). */
  paymentsCanStart = input<boolean>(false);
  /** All payments confirmed AND all reallocations approved. */
  paymentsComplete = input<boolean>(false);

  nodeClick = output<PipelineNode>();

  nodes = computed<PipelineNode[]>(() => {
    const s = this.status();
    const completed = (threshold: CasualCourseStatus): boolean =>
      s !== undefined && s >= threshold && s !== CasualCourseStatus.Rejected;

    const draftDone = s !== undefined;
    const submitDone = completed(CasualCourseStatus.Submitted);
    const ugmDone = completed(CasualCourseStatus.UGMApproved);
    const staffDone = completed(CasualCourseStatus.StaffReviewed);
    const tdDone = completed(CasualCourseStatus.TDApproved);
    const thDone = completed(CasualCourseStatus.THApproved);
    const quoteDone = thDone && this.hasSelectedQuote();
    const travelDone = quoteDone && this.hasTravelInstruction();
    const paymentDone = this.paymentsComplete();

    const stateOf = (done: boolean, isCurrent: boolean): PipelineNodeState =>
      done ? 'completed' : isCurrent ? 'active' : 'pending';

    return [
      {
        key: 'draft',
        label: 'مسودة',
        state: stateOf(draftDone, false),
        anchor: 'details',
      },
      {
        key: 'submit',
        label: 'تقديم',
        state: stateOf(submitDone, s === CasualCourseStatus.Draft || s === CasualCourseStatus.ReturnedToCreator),
        anchor: 'details',
      },
      {
        key: 'ugm',
        label: 'مدير الوحدة',
        state: stateOf(ugmDone, s === CasualCourseStatus.Submitted),
        anchor: 'details',
      },
      {
        key: 'staff',
        label: 'مراجعة التدريب',
        state: stateOf(
          staffDone,
          s === CasualCourseStatus.UGMApproved || s === CasualCourseStatus.UnderReview,
        ),
        anchor: 'financials',
      },
      {
        key: 'th',
        label: 'اعتماد رئيس التدريب',
        state: stateOf(thDone, s === CasualCourseStatus.StaffReviewed || s === CasualCourseStatus.TDApproved),
        anchor: 'financials',
      },
      {
        key: 'quote',
        label: 'اختيار العرض',
        state: stateOf(quoteDone, thDone && !this.hasSelectedQuote()),
        anchor: 'quotes',
      },
      {
        key: 'travel',
        label: 'تعليمات السفر',
        state: stateOf(
          travelDone,
          quoteDone && !this.hasTravelInstruction(),
        ),
        anchor: 'travel',
      },
      {
        key: 'payment',
        label: this.l.t('::Training.Payments.PipelineNode'),
        state: stateOf(
          paymentDone,
          this.paymentsCanStart() && !paymentDone,
        ),
        anchor: 'payments',
      },
      {
        key: 'execution',
        label: 'التنفيذ',
        state: 'pending',
        anchor: '',
        placeholder: true,
      },
    ];
  });

  isRejected = computed(() => this.status() === CasualCourseStatus.Rejected);
  isReturned = computed(() => this.status() === CasualCourseStatus.ReturnedToCreator);

  onNodeClick(node: PipelineNode): void {
    if (node.placeholder || !node.anchor) return;
    this.nodeClick.emit(node);
  }

  iconFor(state: PipelineNodeState): string {
    switch (state) {
      case 'completed': return '✓';
      case 'active':    return '●';
      default:          return '○';
    }
  }

  trackNode = (_: number, n: PipelineNode): string => n.key;
}
