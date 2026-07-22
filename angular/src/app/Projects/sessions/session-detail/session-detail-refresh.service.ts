import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

// Phase 4C-α (v4.10.0) — mirrors CasualCourseDetailRefreshService but scoped to the
// session-detail shell. Embedded child components (price quotes, Travel integration,
// the slim section-payments display) fire .refresh() after any state-mutating action
// so the shell reloads the session detail + recomputes section states.
//
// Subject (not BehaviorSubject) — no replay semantics needed since the shell always
// loads the session once on init and only the embedded children push refresh events.
@Injectable({ providedIn: 'root' })
export class SessionDetailRefreshService {
  private readonly trigger$ = new Subject<void>();
  readonly events = this.trigger$.asObservable();

  refresh(): void {
    this.trigger$.next();
  }
}
