import { Injectable } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';

/**
 * Lets tab components signal the parent shell that the course state changed
 * (e.g. a winning quote was picked, a quote was added/deleted) so the shell
 * can refresh its course detail + quote count and re-evaluate tab gates.
 *
 * Provided in root so the same instance is shared between shell and child tabs.
 */
@Injectable({ providedIn: 'root' })
export class CasualCourseDetailRefreshService {
  private readonly trigger$ = new Subject<void>();
  readonly events = this.trigger$.asObservable();

  /** Fired by the request component when autosave creates a Draft course on /new.
   *  Carries the newly-created id so the shell can attach without re-instantiating.
   *  BehaviorSubject so late subscribers (Sections that mount AFTER the attach
   *  event fires — e.g., Section 2 transitioning from locked to active) still
   *  receive the latest id and can hydrate correctly. */
  private readonly attach$ = new BehaviorSubject<string | null>(null);
  readonly attachEvents = this.attach$.asObservable();

  refresh(): void {
    this.trigger$.next();
  }

  attachNewCourse(id: string): void {
    this.attach$.next(id);
  }

  /** Clear the attached id. Called by the shell when it loads `/new` so the
   *  BehaviorSubject doesn't leak the previous course's id into the next
   *  session. Without this, the request component's ngOnInit would pick up
   *  a stale id and try service.update against it. */
  clearAttachment(): void {
    this.attach$.next(null);
  }

  /** Read the current attached id synchronously (used by components that
   *  need to hydrate at construction time). */
  currentAttachedId(): string | null {
    return this.attach$.value;
  }
}
