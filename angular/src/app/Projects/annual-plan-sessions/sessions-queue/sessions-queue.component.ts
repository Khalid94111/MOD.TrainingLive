import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LocalizationPipe } from '@abp/ng.core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import { OrganizationUnitService } from '@volo/abp.ng.identity/proxy';
import type { OrganizationUnitWithDetailsDto } from '@volo/abp.ng.identity/proxy';

import { AnnualPlanSessionService } from 'src/app/proxy/training/annual-plan-sessions/annual-plan-session.service';
import type {
  PlanItemQueueItemDto,
  PlanItemQueueGetListInput,
} from 'src/app/proxy/training/annual-plan-sessions/dtos/models';
import { CourseType } from 'src/app/proxy/training/enums/course-type.enum';
import { PreferredQuarter } from 'src/app/proxy/training/enums/preferred-quarter.enum';

import { TrainingLocalizationHelper } from '../../shared';

// Phase 4C-α (v4.10.0) — PAGE A.
//
// Staff queue: TH-approved plan items still awaiting session creation, sorted by
// Priority DESC → overdue first → PreferredQuarter ASC → CreationTime ASC (sorted server-side).
// Click "Create" routes to PAGE B-1 (Internal courses) or B-2 (External courses) based on
// the row's CourseType.
@Component({
  selector: 'app-sessions-queue',
  standalone: true,
  templateUrl: './sessions-queue.component.html',
  styleUrls: ['./sessions-queue.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, LocalizationPipe],
})
export class SessionsQueueComponent implements OnInit {
  private readonly queueService = inject(AnnualPlanSessionService);
  private readonly orgUnitService = inject(OrganizationUnitService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly l = inject(TrainingLocalizationHelper);

  // Re-export enums for template comparisons.
  readonly CourseType = CourseType;
  readonly PreferredQuarter = PreferredQuarter;

  // ── Filters ──
  readonly currentYear = new Date().getFullYear();
  readonly fYear = signal<number>(this.currentYear);
  readonly fQuarter = signal<PreferredQuarter | null>(null);
  readonly fPriority = signal<number | null>(null);
  readonly fCourseType = signal<CourseType | null>(null);
  readonly fUnitId = signal<string | null>(null);

  // ── Data ──
  readonly items = signal<PlanItemQueueItemDto[]>([]);
  readonly units = signal<OrganizationUnitWithDetailsDto[]>([]);
  readonly totalCount = signal<number>(0);
  readonly loading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  // Derived counts for the header summary chips.
  readonly overdueCount = computed(() => this.items().filter(i => !!i.isOverdue).length);
  readonly highPriorityCount = computed(() => this.items().filter(i => (i.priority ?? 0) >= 3).length);

  ngOnInit(): void {
    this.loadUnits();
    this.reload();
  }

  private async loadUnits(): Promise<void> {
    try {
      const result = await firstValueFrom(
        this.orgUnitService.getList({ maxResultCount: 1000 }).pipe(takeUntilDestroyed(this.destroyRef)),
      );
      this.units.set(result.items ?? []);
    } catch {
      // Unit dropdown is a convenience filter; failure is non-fatal.
      this.units.set([]);
    }
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set(null);
    try {
      const input: PlanItemQueueGetListInput = {
        year: this.fYear(),
        quarter: this.fQuarter(),
        priority: this.fPriority(),
        courseType: this.fCourseType(),
        unitId: this.fUnitId(),
        maxResultCount: 200,
        skipCount: 0,
      };
      const result = await firstValueFrom(
        this.queueService.getPlanItemsQueue(input).pipe(takeUntilDestroyed(this.destroyRef)),
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

  // ── Filter event handlers (raw value parsers) ──
  onYearChange(event: Event): void {
    const v = (event.target as HTMLInputElement | HTMLSelectElement).value;
    const parsed = v ? Number(v) : this.currentYear;
    this.fYear.set(Number.isFinite(parsed) ? parsed : this.currentYear);
    this.reload();
  }
  onQuarterChange(event: Event): void {
    const v = (event.target as HTMLInputElement | HTMLSelectElement).value;
    this.fQuarter.set(v ? Number(v) as PreferredQuarter : null);
    this.reload();
  }
  onPriorityChange(event: Event): void {
    const v = (event.target as HTMLInputElement | HTMLSelectElement).value;
    this.fPriority.set(v ? Number(v) : null);
    this.reload();
  }
  onCourseTypeChange(event: Event): void {
    const v = (event.target as HTMLInputElement | HTMLSelectElement).value;
    this.fCourseType.set(v ? Number(v) as CourseType : null);
    this.reload();
  }
  onUnitChange(event: Event): void {
    const v = (event.target as HTMLInputElement | HTMLSelectElement).value;
    this.fUnitId.set(v || null);
    this.reload();
  }
  resetFilters(): void {
    this.fYear.set(this.currentYear);
    this.fQuarter.set(null);
    this.fPriority.set(null);
    this.fCourseType.set(null);
    this.fUnitId.set(null);
    this.reload();
  }

  // ── Routing ──
  onCreateClick(row: PlanItemQueueItemDto): void {
    if (!row.id) return;
    const arm = row.courseType === CourseType.Internal ? 'internal' : 'external';
    this.router.navigate(['/training/annual-plan/create-session', arm, row.id]);
  }

  // ── Display helpers ──
  priorityLabelKey(priority: number | undefined): string {
    if ((priority ?? 0) >= 3) return '::Training.AnnualPlan.SessionsQueue.Priority.High';
    if (priority === 2) return '::Training.AnnualPlan.SessionsQueue.Priority.Medium';
    return '::Training.AnnualPlan.SessionsQueue.Priority.Low';
  }

  priorityCss(priority: number | undefined): string {
    if ((priority ?? 0) >= 3) return 'priority-pill priority-high';
    if (priority === 2) return 'priority-pill priority-medium';
    return 'priority-pill priority-low';
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

  quarterLabel(quarter: PreferredQuarter | undefined): string {
    return quarter ? `Q${quarter}` : '';
  }

  unitName(unitId: string | null | undefined): string {
    if (!unitId) return '';
    const match = this.units().find(u => u.id === unitId);
    return match?.displayName ?? match?.code ?? '';
  }

  private extractError(err: unknown): string {
    if (err && typeof err === 'object') {
      const anyErr = err as { error?: { error?: { message?: string } }; message?: string };
      return anyErr.error?.error?.message ?? anyErr.message ?? this.l.t('::Training.AnnualPlan.SessionsQueue.GenericError');
    }
    return this.l.t('::Training.AnnualPlan.SessionsQueue.GenericError');
  }
}
