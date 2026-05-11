import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

/**
 * IDs for every action the page-level header bar can dispatch.
 * The dispatched inner component (request / approval / review) subscribes
 * to the matching events and runs its existing local handler — no logic
 * is duplicated in the bar, only routed.
 */
export type CasualCourseActionId =
  | 'saveDraft'
  | 'submit'           // covers Submit AND Resubmit (handler detects wasReturned)
  | 'approve'          // UGM / TD / TH (handler detects which turn)
  | 'return'
  | 'reject'
  | 'startReview'
  | 'saveProgress'
  | 'finalizeReview'
  | 'print'
  | 'exportPdf';

/**
 * Shared dispatcher for the header action bar. Provided in root so the
 * bar (in the shell) and the inner component (rendered inside a section)
 * see the same instance.
 */
@Injectable({ providedIn: 'root' })
export class CasualCourseActionService {
  private readonly trigger$ = new Subject<CasualCourseActionId>();
  readonly events = this.trigger$.asObservable();

  dispatch(action: CasualCourseActionId): void {
    this.trigger$.next(action);
  }
}
