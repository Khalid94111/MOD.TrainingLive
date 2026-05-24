import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LocalizationPipe } from '@abp/ng.core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';

import { CourseSessionService } from 'src/app/proxy/training/plans/course-session.service';
import type {
  CourseSessionDto,
  CourseSessionGetListInput,
} from 'src/app/proxy/training/plans/dtos/models';
import { CourseType } from 'src/app/proxy/training/enums/course-type.enum';
import { SessionStatus } from 'src/app/proxy/training/enums/session-status.enum';
import { SessionExecutionStage } from 'src/app/proxy/training/enums/session-execution-stage.enum';

import { TrainingLocalizationHelper } from '../../shared';

// Phase 4C-α (v4.10.0) — PAGE C: Sessions List.
//
// Source-agnostic — shows sessions from any TrainingPlanItem (Phase 4C-α) or future
// TrainingCenterPlanItem (Phase 4C-β) source. Three filter dropdowns + Year filter.
// Each row drills into PAGE D (/training/sessions/:id) via the View action.
//
// "Dates" column shows the actual confirmed window for Scheduled/InProgress/Completed
// rows, and "—" with a quarter hint (Q1 2026 etc.) for Planned external sessions whose
// dates haven't been set yet.
@Component({
  selector: 'app-sessions-list',
  standalone: true,
  templateUrl: './sessions-list.component.html',
  styleUrls: ['./sessions-list.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, LocalizationPipe],
})
export class SessionsListComponent implements OnInit {
  private readonly sessionService = inject(CourseSessionService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly l = inject(TrainingLocalizationHelper);

  readonly CourseType = CourseType;
  readonly SessionStatus = SessionStatus;
  readonly SessionExecutionStage = SessionExecutionStage;

  // ── Filters ──
  readonly currentYear = new Date().getFullYear();
  readonly fYear = signal<number | null>(this.currentYear);
  readonly fStatus = signal<SessionStatus | null>(null);
  readonly fCourseType = signal<CourseType | null>(null);
  readonly fExecutionStage = signal<SessionExecutionStage | null>(null);

  // ── Data ──
  readonly items = signal<CourseSessionDto[]>([]);
  readonly totalCount = signal<number>(0);
  readonly loading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  // Header summary chips.
  readonly plannedCount = computed(() =>
    this.items().filter(s => s.status === SessionStatus.Planned).length);
  readonly inProgressCount = computed(() =>
    this.items().filter(s => s.status === SessionStatus.InProgress).length);
  readonly completedCount = computed(() =>
    this.items().filter(s =>
      s.status === SessionStatus.Completed || s.status === SessionStatus.FinanciallyClosed).length);

  ngOnInit(): void {
    this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set(null);
    try {
      const input: CourseSessionGetListInput = {
        planYear: this.fYear() ?? undefined,
        status: this.fStatus() ?? undefined,
        courseType: this.fCourseType() ?? undefined,
        executionStage: this.fExecutionStage() ?? undefined,
        maxResultCount: 200,
        skipCount: 0,
      };
      const result = await firstValueFrom(
        this.sessionService.getList(input).pipe(takeUntilDestroyed(this.destroyRef)),
      );
      this.items.set(result.items ?? []);
      this.totalCount.set(result.totalCount ?? 0);
    } catch (err) {
      this.errorMessage.set(this.extractError(err));
      this.items.set([]);
      this.totalCount.set(0);
    } finally {
      this.loading.set(false);
    }
  }

  // ── Filter handlers ──
  onYearChange(event: Event): void {
    const v = (event.target as HTMLInputElement).value;
    this.fYear.set(v ? Number(v) : null);
    this.reload();
  }
  onStatusChange(event: Event): void {
    const v = (event.target as HTMLSelectElement).value;
    this.fStatus.set(v === '' ? null : Number(v) as SessionStatus);
    this.reload();
  }
  onCourseTypeChange(event: Event): void {
    const v = (event.target as HTMLSelectElement).value;
    this.fCourseType.set(v === '' ? null : Number(v) as CourseType);
    this.reload();
  }
  onStageChange(event: Event): void {
    const v = (event.target as HTMLSelectElement).value;
    this.fExecutionStage.set(v === '' ? null : Number(v) as SessionExecutionStage);
    this.reload();
  }
  resetFilters(): void {
    this.fYear.set(this.currentYear);
    this.fStatus.set(null);
    this.fCourseType.set(null);
    this.fExecutionStage.set(null);
    this.reload();
  }

  // ── Navigation ──
  goDetail(row: CourseSessionDto): void {
    if (row.id) this.router.navigate(['/training/sessions', row.id]);
  }

  // ── Display helpers ──
  statusBadge(s: SessionStatus | undefined): { key: string; css: string } {
    switch (s) {
      case SessionStatus.Planned:           return { key: '::Training.Session.Status.Planned',           css: 'status-badge status-planned' };
      case SessionStatus.Scheduled:         return { key: '::Training.Session.Status.Scheduled',         css: 'status-badge status-scheduled' };
      case SessionStatus.InProgress:        return { key: '::Training.Session.Status.InProgress',        css: 'status-badge status-inprogress' };
      case SessionStatus.Completed:         return { key: '::Training.Session.Status.Completed',         css: 'status-badge status-completed' };
      case SessionStatus.Cancelled:         return { key: '::Training.Session.Status.Cancelled',         css: 'status-badge status-cancelled' };
      case SessionStatus.FinanciallyClosed: return { key: '::Training.Session.Status.FinanciallyClosed', css: 'status-badge status-completed' };
      default: return { key: '', css: 'status-badge' };
    }
  }

  stageBadge(stage: SessionExecutionStage | undefined): { key: string; css: string } {
    switch (stage) {
      case SessionExecutionStage.AwaitingQuoteSelection:    return { key: '::Training.SessionExecutionStage.AwaitingQuoteSelection',    css: 'stage-badge stage-pending' };
      case SessionExecutionStage.AwaitingTravelInstruction: return { key: '::Training.SessionExecutionStage.AwaitingTravelInstruction', css: 'stage-badge stage-pending' };
      case SessionExecutionStage.AwaitingTravelAllowances:  return { key: '::Training.SessionExecutionStage.AwaitingTravelAllowances',  css: 'stage-badge stage-progress' };
      case SessionExecutionStage.AwaitingCoursePayment:     return { key: '::Training.SessionExecutionStage.AwaitingCoursePayment',     css: 'stage-badge stage-pending' };
      case SessionExecutionStage.AwaitingCompletion:        return { key: '::Training.SessionExecutionStage.AwaitingCompletion',        css: 'stage-badge stage-progress' };
      case SessionExecutionStage.FinanciallyComplete:       return { key: '::Training.SessionExecutionStage.FinanciallyComplete',       css: 'stage-badge stage-complete' };
      case SessionExecutionStage.NoExecutionPending:        return { key: '::Training.SessionExecutionStage.NoExecutionPending',        css: 'stage-badge stage-muted' };
      default: return { key: '', css: 'stage-badge' };
    }
  }

  courseTypeLabelKey(courseType: CourseType | undefined): string {
    switch (courseType) {
      case CourseType.Internal:              return '::Training.AnnualPlan.SessionsQueue.Type.Internal';
      case CourseType.ExternalLocal:         return '::Training.AnnualPlan.SessionsQueue.Type.ExternalLocal';
      case CourseType.ExternalInternational: return '::Training.AnnualPlan.SessionsQueue.Type.ExternalInternational';
      default: return '';
    }
  }
  courseTypeCss(courseType: CourseType | undefined): string {
    switch (courseType) {
      case CourseType.Internal:              return 'type-pill type-internal';
      case CourseType.ExternalLocal:         return 'type-pill type-local';
      case CourseType.ExternalInternational: return 'type-pill type-international';
      default: return 'type-pill';
    }
  }

  // Returns a (start → end) string for Scheduled+ sessions, or a quarter hint for Planned.
  datesCell(row: CourseSessionDto): { primary: string; hint: string | null } {
    if (row.actualStartDate && row.actualEndDate) {
      return {
        primary: `${this.fmt(row.actualStartDate)} → ${this.fmt(row.actualEndDate)}`,
        hint: null,
      };
    }
    const quarter = row.preferredQuarter ? `Q${row.preferredQuarter}` : '';
    const year = row.planYear ?? '';
    return {
      primary: '—',
      hint: quarter && year ? `${quarter} ${year}` : null,
    };
  }

  private fmt(iso: string | null | undefined): string {
    if (!iso) return '';
    return iso.substring(0, 10);
  }

  private extractError(err: unknown): string {
    if (err && typeof err === 'object') {
      const anyErr = err as { error?: { error?: { message?: string } }; message?: string };
      return anyErr.error?.error?.message ?? anyErr.message ?? this.l.t('::Training.Sessions.List.GenericError');
    }
    return this.l.t('::Training.Sessions.List.GenericError');
  }
}
