import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { LocalizationPipe } from '@abp/ng.core';
import { firstValueFrom } from 'rxjs';

import { AnnualPlanSessionService } from 'src/app/proxy/training/annual-plan-sessions/annual-plan-session.service';
import type {
  AnnualPlanProgressDto,
  OverdueAlertDto,
  QuarterProgressDto,
  UnitProgressDto,
} from 'src/app/proxy/training/annual-plan-sessions/dtos/models';
import { CourseType } from 'src/app/proxy/training/enums/course-type.enum';

import { TrainingLocalizationHelper } from '../../shared';

@Component({
  selector: 'app-annual-plan-dashboard',
  standalone: true,
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, LocalizationPipe],
})
export class AnnualPlanDashboardComponent implements OnInit {
  private readonly api = inject(AnnualPlanSessionService);
  private readonly router = inject(Router);
  readonly l = inject(TrainingLocalizationHelper);

  readonly currentYear = new Date().getFullYear();
  readonly selectedYear = signal(this.currentYear);
  readonly progress = signal<AnnualPlanProgressDto | null>(null);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  private loadSequence = 0;

  readonly years = computed(() => {
    const available = this.progress()?.availableYears ?? [];
    return [...new Set([this.selectedYear(), ...available])].sort((a, b) => b - a);
  });

  readonly stats = computed(() => {
    const item = this.progress();
    return {
      total: item?.totalPlanItems ?? 0,
      awaiting: item?.awaitingSessionCount ?? 0,
      planned: item?.plannedSessionCount ?? 0,
      scheduled: item?.scheduledSessionCount ?? 0,
      inProgress: item?.inProgressSessionCount ?? 0,
      completed: item?.completedSessionCount ?? 0,
      cancelled: item?.cancelledSessionCount ?? 0,
      attention: item?.attentionCount ?? item?.alerts?.length ?? 0,
      completion: item?.overallProgressPercent ?? 0,
    };
  });

  readonly quarters = computed(() =>
    (this.progress()?.progressByQuarter ?? []).filter(item => (item.total ?? 0) > 0),
  );
  readonly units = computed(() => this.progress()?.progressByUnit ?? []);
  readonly alerts = computed(() => this.progress()?.alerts ?? []);
  readonly hasData = computed(() => this.stats().total > 0);

  ngOnInit(): void {
    void this.reload(true);
  }

  async reload(selectAvailableYear = false): Promise<void> {
    const sequence = ++this.loadSequence;
    this.loading.set(true);
    this.errorMessage.set(null);

    try {
      const result = await firstValueFrom(
        this.api.getProgressDashboard(this.selectedYear()),
      );
      if (sequence !== this.loadSequence) return;

      const availableYears = result?.availableYears ?? [];
      if (
        selectAvailableYear &&
        (result?.totalPlanItems ?? 0) === 0 &&
        availableYears.length > 0 &&
        !availableYears.includes(this.selectedYear())
      ) {
        this.progress.set(result ?? null);
        this.selectedYear.set(availableYears[0]);
        await this.reload(false);
        return;
      }

      this.progress.set(result ?? null);
    } catch (error) {
      if (sequence !== this.loadSequence) return;
      this.errorMessage.set(this.extractError(error));
      this.progress.set(null);
    } finally {
      if (sequence === this.loadSequence) {
        this.loading.set(false);
      }
    }
  }

  onYearChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    if (!Number.isFinite(value) || value === this.selectedYear()) return;
    this.selectedYear.set(value);
    void this.reload(false);
  }

  stagePercent(value?: number | null, total?: number | null): number {
    if (!total || !value) return 0;
    return Math.max(0, Math.min(100, value / total * 100));
  }

  quarterCompletion(item: QuarterProgressDto): number {
    return this.stagePercent(item.completed, item.total);
  }

  unitCompletion(item: UnitProgressDto): number {
    return this.stagePercent(item.completed, item.total);
  }

  quarterLabel(quarter?: number): string {
    return this.l.t(`::Training.AnnualPlan.Dashboard.Quarter.Q${quarter ?? 1}`);
  }

  unitName(item: UnitProgressDto): string {
    return item.unitName || this.l.t('::Training.AnnualPlan.Dashboard.NoUnit');
  }

  courseTypeLabel(type?: CourseType): string {
    switch (type) {
      case CourseType.Internal:
        return this.l.t('::Training.CourseType.Internal');
      case CourseType.ExternalLocal:
        return this.l.t('::Training.CourseType.ExternalLocal');
      case CourseType.ExternalInternational:
        return this.l.t('::Training.CourseType.ExternalInternational');
      default:
        return '—';
    }
  }

  alertTitle(alert: OverdueAlertDto): string {
    return this.l.t(`::Training.AnnualPlan.Dashboard.Alert.${alert.type ?? 'OverduePlanItem'}`);
  }

  alertDescription(alert: OverdueAlertDto): string {
    const days = alert.daysOverdue ?? 0;
    switch (alert.type) {
      case 'OverduePlanItem':
        return this.l.t(
          '::Training.AnnualPlan.Dashboard.AlertText.OverduePlanItem',
          this.quarterLabel(alert.preferredQuarter),
          days,
        );
      case 'StuckPlanned':
        return this.l.t('::Training.AnnualPlan.Dashboard.AlertText.StuckPlanned', days);
      case 'ScheduledAfterQuarter':
        return this.l.t(
          '::Training.AnnualPlan.Dashboard.AlertText.ScheduledAfterQuarter',
          this.quarterLabel(alert.preferredQuarter),
          days,
        );
      case 'ExecutionNotStarted':
        return this.l.t('::Training.AnnualPlan.Dashboard.AlertText.ExecutionNotStarted', days);
      case 'CompletionOverdue':
        return this.l.t('::Training.AnnualPlan.Dashboard.AlertText.CompletionOverdue', days);
      case 'ScheduleMissingDates':
        return this.l.t('::Training.AnnualPlan.Dashboard.AlertText.ScheduleMissingDates');
      default:
        return alert.message || '—';
    }
  }

  alertActionLabel(alert: OverdueAlertDto): string {
    return alert.type === 'OverduePlanItem'
      ? this.l.t('::Training.AnnualPlan.Dashboard.Action.CreateSession')
      : alert.type === 'StuckPlanned'
        ? this.l.t('::Training.AnnualPlan.Dashboard.Action.CompleteQuotes')
        : this.l.t('::Training.AnnualPlan.Dashboard.Action.OpenExecution');
  }

  alertTone(alert: OverdueAlertDto): string {
    if (alert.type === 'CompletionOverdue' || alert.type === 'OverduePlanItem') {
      return 'critical';
    }
    return 'warning';
  }

  alertIcon(alert: OverdueAlertDto): string {
    switch (alert.type) {
      case 'OverduePlanItem': return 'bi-calendar-x';
      case 'StuckPlanned': return 'bi-hourglass-split';
      case 'ScheduledAfterQuarter': return 'bi-calendar2-week';
      case 'ExecutionNotStarted': return 'bi-play-circle';
      case 'CompletionOverdue': return 'bi-exclamation-octagon';
      default: return 'bi-calendar2-x';
    }
  }

  formatDate(value?: string | null): string {
    if (!value) return '';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    return date.toLocaleDateString('en-GB');
  }

  openAlert(alert: OverdueAlertDto): void {
    if (!alert.entityId) return;

    if (alert.type === 'OverduePlanItem') {
      const mode = alert.courseType === CourseType.Internal ? 'internal' : 'external';
      void this.router.navigate([
        '/training/annual-plan/create-session',
        mode,
        alert.entityId,
      ]);
      return;
    }

    const stage = alert.type === 'StuckPlanned' ? 'quotes' : 'execution';
    void this.router.navigate(
      ['/training/sessions', alert.entityId],
      { queryParams: { stage } },
    );
  }

  goQueue(): void {
    void this.router.navigate(
      ['/training/annual-plan/sessions-queue'],
      { queryParams: { year: this.selectedYear() } },
    );
  }

  goSessions(): void {
    void this.router.navigate(
      ['/training/sessions'],
      { queryParams: { year: this.selectedYear() } },
    );
  }

  trackQuarter(_: number, item: QuarterProgressDto): number {
    return item.quarter ?? 0;
  }

  trackUnit(index: number, item: UnitProgressDto): string {
    return item.unitId ?? `no-unit-${index}`;
  }

  trackAlert(index: number, item: OverdueAlertDto): string {
    return `${item.type}-${item.entityId}-${index}`;
  }

  private extractError(error: unknown): string {
    if (error && typeof error === 'object') {
      const value = error as { error?: { error?: { message?: string } }; message?: string };
      return value.error?.error?.message
        ?? value.message
        ?? this.l.t('::Training.AnnualPlan.Dashboard.GenericError');
    }
    return this.l.t('::Training.AnnualPlan.Dashboard.GenericError');
  }
}
