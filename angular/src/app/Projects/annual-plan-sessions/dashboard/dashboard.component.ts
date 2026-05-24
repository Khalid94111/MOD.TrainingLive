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
  AnnualPlanProgressDto,
  OverdueAlertDto,
  QuarterProgressDto,
  UnitProgressDto,
} from 'src/app/proxy/training/annual-plan-sessions/dtos/models';

import { TrainingLocalizationHelper } from '../../shared';

// Phase 4C-α (v4.10.0) — PAGE E: Annual Plan progress dashboard.
//
// Reads AnnualPlanSessionService.getProgressDashboard(year) and visualises:
//   - 5 stat cards (Total Items, Planned, Scheduled, Completed, Overdue)
//   - Overall progress bar
//   - Per-quarter progress (4 bars)
//   - Per-unit progress (one bar per OU)
//   - Two alert streams (overdue plan items + stuck-Planned sessions)
//
// Unit names are resolved client-side via OrganizationUnitService because the OU lookup
// lives outside the Training module and the backend ships UnitName=null intentionally.
@Component({
  selector: 'app-annual-plan-dashboard',
  standalone: true,
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, LocalizationPipe],
})
export class AnnualPlanDashboardComponent implements OnInit {
  private readonly api = inject(AnnualPlanSessionService);
  private readonly orgUnitService = inject(OrganizationUnitService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly l = inject(TrainingLocalizationHelper);

  // ── Year selector ──
  readonly currentYear = new Date().getFullYear();
  readonly fYear = signal<number>(this.currentYear);

  // ── Data ──
  readonly progress = signal<AnnualPlanProgressDto | null>(null);
  readonly units = signal<OrganizationUnitWithDetailsDto[]>([]);
  readonly loading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);

  // ── Derived ──
  readonly stats = computed(() => {
    const p = this.progress();
    if (!p) {
      return {
        total: 0, planned: 0, scheduled: 0, inProgress: 0, completed: 0, cancelled: 0, overdue: 0,
        percent: 0,
      };
    }
    return {
      total:      p.totalPlanItems         ?? 0,
      planned:    p.plannedSessionCount    ?? 0,
      scheduled:  p.scheduledSessionCount  ?? 0,
      inProgress: p.inProgressSessionCount ?? 0,
      completed:  p.completedSessionCount  ?? 0,
      cancelled:  p.cancelledSessionCount  ?? 0,
      overdue:    p.overdueCount           ?? 0,
      percent:    p.overallProgressPercent ?? 0,
    };
  });

  readonly quarterBars = computed<QuarterProgressDto[]>(() =>
    this.progress()?.progressByQuarter ?? []);

  readonly unitBars = computed<UnitProgressDto[]>(() =>
    this.progress()?.progressByUnit ?? []);

  readonly alerts = computed<OverdueAlertDto[]>(() =>
    this.progress()?.alerts ?? []);

  ngOnInit(): void {
    this.loadUnits();
    this.reload();
  }

  private async loadUnits(): Promise<void> {
    try {
      const result = await firstValueFrom(
        this.orgUnitService.getList({ maxResultCount: 1000 })
          .pipe(takeUntilDestroyed(this.destroyRef)),
      );
      this.units.set(result.items ?? []);
    } catch {
      this.units.set([]);
    }
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set(null);
    try {
      const result = await firstValueFrom(
        this.api.getProgressDashboard(this.fYear())
          .pipe(takeUntilDestroyed(this.destroyRef)),
      );
      this.progress.set(result ?? null);
    } catch (err) {
      this.errorMessage.set(this.extractError(err));
      this.progress.set(null);
    } finally {
      this.loading.set(false);
    }
  }

  onYearChange(event: Event): void {
    const v = (event.target as HTMLInputElement).value;
    const parsed = v ? Number(v) : this.currentYear;
    this.fYear.set(Number.isFinite(parsed) ? parsed : this.currentYear);
    this.reload();
  }

  // ── Display helpers ──
  quarterPercent(q: QuarterProgressDto): number {
    if (!q.total) return 0;
    return Math.round(100 * (q.completed ?? 0) / q.total);
  }

  unitPercent(u: UnitProgressDto): number {
    if (!u.total) return 0;
    return Math.round(100 * (u.completed ?? 0) / u.total);
  }

  unitName(u: UnitProgressDto): string {
    if (!u.unitId) return this.l.t('::Training.AnnualPlan.Dashboard.NoUnit');
    const match = this.units().find(x => x.id === u.unitId);
    return match?.displayName ?? match?.code ?? `Unit ${u.unitId.substring(0, 8)}`;
  }

  alertCss(alert: OverdueAlertDto): string {
    return alert.type === 'StuckPlanned'
      ? 'alert-row alert-stuck'
      : 'alert-row alert-overdue';
  }

  alertTypeLabelKey(alert: OverdueAlertDto): string {
    return alert.type === 'StuckPlanned'
      ? '::Training.AnnualPlan.Dashboard.Alert.StuckPlanned'
      : '::Training.AnnualPlan.Dashboard.Alert.OverduePlanItem';
  }

  onAlertClick(alert: OverdueAlertDto): void {
    if (alert.type === 'StuckPlanned' && alert.entityId) {
      void this.router.navigate(['/training/sessions', alert.entityId]);
    } else if (alert.entityId) {
      // Plan-item overdue → navigate to the queue with that item highlighted (queue
      // doesn't have a per-row deep link yet; just open the queue).
      void this.router.navigate(['/training/annual-plan/sessions-queue']);
    }
  }

  goQueue(): void {
    void this.router.navigate(['/training/annual-plan/sessions-queue']);
  }
  goSessions(): void {
    void this.router.navigate(['/training/sessions']);
  }

  private extractError(err: unknown): string {
    if (err && typeof err === 'object') {
      const anyErr = err as { error?: { error?: { message?: string } }; message?: string };
      return anyErr.error?.error?.message ?? anyErr.message ?? this.l.t('::Training.AnnualPlan.Dashboard.GenericError');
    }
    return this.l.t('::Training.AnnualPlan.Dashboard.GenericError');
  }
}
