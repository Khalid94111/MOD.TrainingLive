import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { PriceQuoteService } from 'src/app/proxy/training/finance';
import type { PriceQuoteDto } from 'src/app/proxy/training/finance/dtos/models';
import { CourseType } from 'src/app/proxy/training/enums/course-type.enum';
import { SessionStatus } from 'src/app/proxy/training/enums/session-status.enum';
import { CourseSessionService } from 'src/app/proxy/training/plans';
import type { CourseSessionDto } from 'src/app/proxy/training/plans/dtos/models';

import { TrainingLocalizationHelper } from '../../shared';

type QuoteWorkflowState = 'noQuotes' | 'awaitingWinner' | 'selected' | 'cancelled';
type QuoteWorkflowFilter = 'all' | QuoteWorkflowState;

interface QuoteSessionRow {
  session: CourseSessionDto;
  quoteCount: number;
  selectedQuote: PriceQuoteDto | null;
  state: QuoteWorkflowState;
}

@Component({
  standalone: true,
  selector: 'app-price-quote-list',
  templateUrl: './price-quote-list.component.html',
  styleUrl: './price-quote-list.component.scss',
  imports: [CommonModule, LocalizationPipe],
})
export class PriceQuoteListComponent implements OnInit {
  private readonly quoteService = inject(PriceQuoteService);
  private readonly sessionService = inject(CourseSessionService);
  private readonly router = inject(Router);
  private readonly l = inject(TrainingLocalizationHelper);

  readonly CourseType = CourseType;

  readonly sessions = signal<CourseSessionDto[]>([]);
  readonly quotes = signal<PriceQuoteDto[]>([]);
  readonly isLoading = signal(false);
  readonly loadError = signal<string | null>(null);

  readonly searchText = signal('');
  readonly workflowFilter = signal<QuoteWorkflowFilter>('all');
  readonly courseTypeFilter = signal<CourseType | null>(null);
  readonly yearFilter = signal<number | null>(null);

  readonly rows = computed<QuoteSessionRow[]>(() => {
    const quotesBySession = new Map<string, PriceQuoteDto[]>();
    for (const quote of this.quotes()) {
      if (!quote.sessionId) continue;
      const current = quotesBySession.get(quote.sessionId) ?? [];
      current.push(quote);
      quotesBySession.set(quote.sessionId, current);
    }

    return this.sessions()
      .filter(session => session.courseType === CourseType.ExternalLocal
        || session.courseType === CourseType.ExternalInternational)
      .map(session => {
        const sessionQuotes = quotesBySession.get(session.id) ?? [];
        const selectedQuote = sessionQuotes.find(quote =>
          quote.id === session.selectedPriceQuoteId || quote.isSelected) ?? null;
        const state: QuoteWorkflowState = session.status === SessionStatus.Cancelled
          ? 'cancelled'
          : session.selectedPriceQuoteId || selectedQuote
            ? 'selected'
            : sessionQuotes.length > 0
              ? 'awaitingWinner'
              : 'noQuotes';

        return {
          session,
          quoteCount: sessionQuotes.length,
          selectedQuote,
          state,
        };
      })
      .sort((a, b) => {
        const priority: Record<QuoteWorkflowState, number> = {
          awaitingWinner: 0,
          noQuotes: 1,
          selected: 2,
          cancelled: 3,
        };
        return priority[a.state] - priority[b.state]
          || (b.session.planYear ?? 0) - (a.session.planYear ?? 0)
          || (a.session.tenantCourseNameAr ?? '').localeCompare(b.session.tenantCourseNameAr ?? '', 'ar');
      });
  });

  readonly filteredRows = computed(() => {
    const term = this.searchText().trim().toLocaleLowerCase();
    const workflow = this.workflowFilter();
    const courseType = this.courseTypeFilter();
    const year = this.yearFilter();

    return this.rows().filter(row => {
      if (workflow !== 'all' && row.state !== workflow) return false;
      if (courseType !== null && row.session.courseType !== courseType) return false;
      if (year !== null && row.session.planYear !== year) return false;
      if (!term) return true;

      return [
        row.session.tenantCourseNameAr,
        row.session.tenantCourseNameEn,
        row.selectedQuote?.providerName,
      ].some(value => value?.toLocaleLowerCase().includes(term));
    });
  });

  readonly yearOptions = computed(() => [...new Set(
    this.rows()
      .map(row => row.session.planYear)
      .filter((year): year is number => year !== undefined && year !== null),
  )].sort((a, b) => b - a));

  readonly noQuotesCount = computed(() => this.rows().filter(row => row.state === 'noQuotes').length);
  readonly awaitingWinnerCount = computed(() =>
    this.rows().filter(row => row.state === 'awaitingWinner').length);
  readonly selectedCount = computed(() => this.rows().filter(row => row.state === 'selected').length);
  readonly hasFilters = computed(() => !!this.searchText().trim()
    || this.workflowFilter() !== 'all'
    || this.courseTypeFilter() !== null
    || this.yearFilter() !== null);

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.isLoading.set(true);
    this.loadError.set(null);
    try {
      const [sessionsResult, quotesResult] = await Promise.all([
        firstValueFrom(this.sessionService.getList({ maxResultCount: 1000, skipCount: 0 })),
        firstValueFrom(this.quoteService.getList({ maxResultCount: 1000, skipCount: 0 })),
      ]);
      this.sessions.set(sessionsResult.items ?? []);
      this.quotes.set(quotesResult.items ?? []);
    } catch (error: unknown) {
      this.sessions.set([]);
      this.quotes.set([]);
      this.loadError.set(this.extractError(error));
    } finally {
      this.isLoading.set(false);
    }
  }

  setWorkflowFilter(value: QuoteWorkflowFilter): void {
    this.workflowFilter.set(value);
  }

  onWorkflowChange(value: string): void {
    this.workflowFilter.set(value as QuoteWorkflowFilter);
  }

  onCourseTypeChange(value: string): void {
    this.courseTypeFilter.set(value === '' ? null : Number(value) as CourseType);
  }

  onYearChange(value: string): void {
    this.yearFilter.set(value === '' ? null : Number(value));
  }

  clearFilters(): void {
    this.searchText.set('');
    this.workflowFilter.set('all');
    this.courseTypeFilter.set(null);
    this.yearFilter.set(null);
  }

  openQuotes(row: QuoteSessionRow): void {
    void this.router.navigate(['/training/sessions', row.session.id], {
      queryParams: { stage: 'quotes' },
    });
  }

  stateMeta(state: QuoteWorkflowState): { key: string; css: string; icon: string } {
    switch (state) {
      case 'noQuotes':
        return { key: '::Training.PriceQuotes.Dashboard.NoQuotes', css: 'state-empty', icon: 'bi bi-inbox' };
      case 'awaitingWinner':
        return { key: '::Training.PriceQuotes.Dashboard.AwaitingWinner', css: 'state-action', icon: 'bi bi-hourglass-split' };
      case 'selected':
        return { key: '::Training.PriceQuotes.Dashboard.WinnerSelected', css: 'state-selected', icon: 'bi bi-check-circle-fill' };
      case 'cancelled':
        return { key: '::Training.PriceQuotes.Dashboard.Cancelled', css: 'state-cancelled', icon: 'bi bi-x-circle' };
    }
  }

  actionKey(state: QuoteWorkflowState): string {
    switch (state) {
      case 'noQuotes': return '::Training.PriceQuotes.Dashboard.AddQuotes';
      case 'awaitingWinner': return '::Training.PriceQuotes.Dashboard.ReviewAndSelect';
      case 'selected': return '::Training.PriceQuotes.Dashboard.ViewDecision';
      case 'cancelled': return '::Training.PriceQuotes.Dashboard.ViewRecord';
    }
  }

  courseTypeKey(courseType: CourseType | undefined): string {
    return courseType === CourseType.ExternalInternational
      ? '::Training.CourseType.ExternalInternational'
      : '::Training.CourseType.ExternalLocal';
  }

  courseTypeCss(courseType: CourseType | undefined): string {
    return courseType === CourseType.ExternalInternational ? 'type-international' : 'type-local';
  }

  formatDate(value: string | null | undefined): string {
    return value?.substring(0, 10) ?? '—';
  }

  formatOMR(value: number | null | undefined): string {
    return (value ?? 0).toLocaleString('en-US', {
      minimumFractionDigits: 3,
      maximumFractionDigits: 3,
    });
  }

  quarterLabel(session: CourseSessionDto): string {
    const quarter = session.preferredQuarter ? `Q${session.preferredQuarter}` : '—';
    return session.planYear ? `${quarter} · ${session.planYear}` : quarter;
  }

  private extractError(error: unknown): string {
    if (error && typeof error === 'object') {
      const apiError = error as { error?: { error?: { message?: string } }; message?: string };
      return apiError.error?.error?.message
        ?? apiError.message
        ?? this.l.t('::Training.PriceQuotes.Dashboard.LoadError');
    }
    return this.l.t('::Training.PriceQuotes.Dashboard.LoadError');
  }
}
