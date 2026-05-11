import { Component, computed, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';

import { CasualCourseStatus } from '../../../shared';
import {
  CasualCourseActionService,
  type CasualCourseActionId,
} from '../casual-course-action.service';

interface ActionButton {
  id: CasualCourseActionId;
  label: string;
  cssClass: string;     // gtms-design.scss button class
  disabled?: boolean;
}

/**
 * Page-level action bar — lives in the shell header so workflow actions
 * are always one click away regardless of scroll position. Pure UI: each
 * click calls into CasualCourseActionService, which the dispatched inner
 * component listens to and runs its own local handler.
 */
@Component({
  standalone: true,
  selector: 'app-casual-course-header-action-bar',
  templateUrl: './casual-course-header-action-bar.component.html',
  styleUrls: ['./casual-course-header-action-bar.component.scss'],
  imports: [CommonModule],
})
export class CasualCourseHeaderActionBarComponent {
  private actions = inject(CasualCourseActionService);

  status = input<CasualCourseStatus | undefined>(undefined);

  // Permissions — passed in from the shell so this component stays a pure UI dispatcher.
  canEditDraft   = input<boolean>(false);
  canApproveUgm  = input<boolean>(false);
  canReview      = input<boolean>(false);
  canTdApprove   = input<boolean>(false);
  canHeadApprove = input<boolean>(false);

  buttons = computed<ActionButton[]>(() => {
    const s = this.status();
    // /new — no course saved yet. Treat as Draft for UTM.
    if (s === undefined) {
      if (!this.canEditDraft()) return [];
      return [
        { id: 'saveDraft', label: '💾 حفظ كمسودة', cssClass: 'btn btn-warning' },
        { id: 'submit',    label: '📤 إرسال الطلب', cssClass: 'btn btn-primary' },
      ];
    }

    switch (s) {
      case CasualCourseStatus.Draft:
        if (!this.canEditDraft()) return [];
        return [
          { id: 'saveDraft', label: '💾 حفظ كمسودة', cssClass: 'btn btn-warning' },
          { id: 'submit',    label: '📤 إرسال الطلب', cssClass: 'btn btn-primary' },
        ];

      case CasualCourseStatus.ReturnedToCreator:
        if (!this.canEditDraft()) return [];
        return [
          { id: 'saveDraft', label: '💾 حفظ كمسودة', cssClass: 'btn btn-warning' },
          { id: 'submit',    label: '🔁 إعادة الإرسال', cssClass: 'btn btn-primary' },
        ];

      case CasualCourseStatus.Submitted:
        if (!this.canApproveUgm()) return [];
        return [
          { id: 'reject',  label: '❌ رفض',            cssClass: 'btn btn-danger' },
          { id: 'return',  label: '🔁 إعادة',          cssClass: 'btn btn-warning' },
          { id: 'approve', label: '✅ اعتماد UGM',     cssClass: 'btn btn-success' },
        ];

      case CasualCourseStatus.UGMApproved:
        if (!this.canReview()) return [];
        return [
          { id: 'startReview', label: '▶️ بدء المراجعة', cssClass: 'btn btn-primary' },
        ];

      case CasualCourseStatus.UnderReview:
        if (!this.canReview()) return [];
        return [
          { id: 'reject',         label: '❌ رفض',                 cssClass: 'btn btn-danger' },
          { id: 'return',         label: '🔁 إعادة',               cssClass: 'btn btn-warning' },
          { id: 'saveProgress',   label: '💾 حفظ التقدم',          cssClass: 'btn btn-outline' },
          { id: 'finalizeReview', label: '📤 إنهاء المراجعة',      cssClass: 'btn btn-primary' },
        ];

      case CasualCourseStatus.StaffReviewed:
        if (!this.canTdApprove()) return [];
        return [
          { id: 'reject',  label: '❌ رفض',         cssClass: 'btn btn-danger' },
          { id: 'return',  label: '🔁 إعادة',       cssClass: 'btn btn-warning' },
          { id: 'approve', label: '✅ اعتماد TD',   cssClass: 'btn btn-success' },
        ];

      case CasualCourseStatus.TDApproved:
        if (!this.canHeadApprove()) return [];
        return [
          { id: 'reject',  label: '❌ رفض',                cssClass: 'btn btn-danger' },
          { id: 'return',  label: '🔁 إعادة',              cssClass: 'btn btn-warning' },
          { id: 'approve', label: '✅ اعتماد نهائي TH',     cssClass: 'btn btn-success' },
        ];

      case CasualCourseStatus.THApproved:
        return [
          { id: 'print',     label: '🖨️ طباعة',     cssClass: 'btn btn-ghost' },
          { id: 'exportPdf', label: '📄 تصدير PDF', cssClass: 'btn btn-ghost' },
        ];

      case CasualCourseStatus.Rejected:
        return [];

      default:
        return [];
    }
  });

  visible = computed(() => this.buttons().length > 0);

  onClick(btn: ActionButton): void {
    if (btn.disabled) return;
    if (btn.id === 'print') {
      // Native print — content stays on screen, browser handles the dialog.
      window.print();
      return;
    }
    if (btn.id === 'exportPdf') {
      // TODO Phase 4B-β — wire to a real PDF export endpoint.
      alert('تصدير PDF — قيد التطوير في Phase 4B-β.');
      return;
    }
    this.actions.dispatch(btn.id);
  }

  trackBtn = (_: number, b: ActionButton): string => b.id;
}
