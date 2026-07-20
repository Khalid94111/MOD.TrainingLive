import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { CasualCourseService } from 'src/app/proxy/training/casual-courses';
import type {
  CasualCourseDetailDto,
  SelectPriceQuoteDto,
} from 'src/app/proxy/training/casual-courses/dtos/models';
import { CourseSessionService } from 'src/app/proxy/training/plans/course-session.service';
import type {
  CourseSessionDetailDto,
  SelectSessionPriceQuoteDto,
} from 'src/app/proxy/training/plans/dtos/models';
import { PriceQuoteService, TrainingProviderService } from 'src/app/proxy/training/finance';
import type {
  CreateUpdatePriceQuoteDto,
  PriceQuoteDto,
  TrainingProviderDto,
} from 'src/app/proxy/training/finance/dtos/models';
import { GeographicalLocationService } from 'src/app/proxy/training/hr-integration/geographical-location.service';
import type { GeographicalLocationDto } from 'src/app/proxy/training/hr-integration/dtos/models';
import { ProviderScope } from 'src/app/proxy/training/enums/provider-scope.enum';

import {
  CasualCourseStatus,
  TrainingLocalizationHelper,
  VarianceChipComponent,
} from '../../shared';
import { CasualCourseDetailRefreshService } from '../../casual-courses/casual-course-detail/casual-course-detail-refresh.service';
import { SessionDetailRefreshService } from '../../sessions/session-detail/session-detail-refresh.service';

type ParentArm = 'casualCourse' | 'session';

// Phase 4C-α (v4.10.0) — renamed from CasualCoursePriceQuotesComponent. Polymorphic
// price-quote workflow shared between casual courses (Phase 4B-α) and annual-plan
// sessions (Phase 4C-α); route data carries `parentArm: 'casualCourse' | 'session'`.
@Component({
  standalone: true,
  selector: 'app-price-quotes',
  templateUrl: './price-quotes.component.html',
  styleUrls: ['./price-quotes.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, VarianceChipComponent],
})
export class PriceQuotesComponent implements OnInit {
  private courseService = inject(CasualCourseService);
  private sessionService = inject(CourseSessionService);
  private quoteService = inject(PriceQuoteService);
  private providerService = inject(TrainingProviderService);
  private geoService = inject(GeographicalLocationService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private refreshShell = inject(CasualCourseDetailRefreshService);
  private refreshSessionShell = inject(SessionDetailRefreshService);
  l = inject(TrainingLocalizationHelper);

  ProviderScope = ProviderScope;
  CasualCourseStatus = CasualCourseStatus;

  parentArm = signal<ParentArm>('casualCourse');
  parentId = signal<string>('');
  embedded = signal<boolean>(false);

  course = signal<CasualCourseDetailDto | null>(null);
  // Patch 1 (v4.10.1) — session arm needs the parent detail too so variance chips can
  // compare against ApprovedCostOMR. loadSession() populates this when parentArm === 'session'.
  session = signal<CourseSessionDetailDto | null>(null);
  quotes = signal<PriceQuoteDto[]>([]);
  providers = signal<TrainingProviderDto[]>([]);
  countries = signal<GeographicalLocationDto[]>([]);
  citiesByCountry = signal<Record<string, GeographicalLocationDto[]>>({});

  loading = signal(false);
  saveError = signal<string | null>(null);

  // Add/Edit quote dialog state
  quoteDialogOpen = signal(false);
  isEditMode = signal(false);
  editingQuoteId = signal<string | null>(null);
  fProviderId = signal<string>('');
  fQuotedPriceOMR = signal<number>(0);
  fCountryId = signal<string | null>(null);
  fCityId = signal<string | null>(null);
  fNotes = signal<string>('');

  // Pick Winner dialog state
  pickDialogOpen = signal(false);
  pickQuote = signal<PriceQuoteDto | null>(null);
  fActualStartDate = signal<string>('');
  fActualEndDate = signal<string>('');

  // Patch 1 (v4.10.1) — polymorphic approved-cost source. For casual courses we use
  // EstimatedTotalCost (post-StaffReviewed); for sessions we use ApprovedCostOMR from
  // the originating TrainingPlanItem. Surface name kept for backwards compatibility.
  estimatedTotalCost = computed(() => {
    if (this.parentArm() === 'session') {
      return this.session()?.approvedCostOMR ?? 0;
    }
    return this.course()?.estimatedTotalCost ?? 0;
  });
  approvedCost = computed(() => this.estimatedTotalCost());
  hasSelected = computed(() => this.quotes().some(q => q.isSelected));
  selectedQuote = computed(() => this.quotes().find(q => q.isSelected) ?? null);
  quotesLocked = computed(() => this.parentArm() === 'session'
    && (!!this.session()?.selectedPriceQuoteId || this.hasSelected()));
  expectedDateFrom = computed(() => this.parentArm() === 'session'
    ? this.session()?.estimatedDateFrom
    : this.course()?.estimatedDateFrom);
  expectedDateTo = computed(() => this.parentArm() === 'session'
    ? this.session()?.estimatedDateTo
    : this.course()?.estimatedDateTo);
  actualStartDate = computed(() => this.parentArm() === 'session'
    ? this.session()?.actualStartDate
    : this.course()?.actualStartDate);
  actualEndDate = computed(() => this.parentArm() === 'session'
    ? this.session()?.actualEndDate
    : this.course()?.actualEndDate);

  // sorted: winner first, then by creation
  sortedQuotes = computed(() => {
    const list = [...this.quotes()];
    list.sort((a, b) => {
      if (a.isSelected && !b.isSelected) return -1;
      if (!a.isSelected && b.isSelected) return 1;
      return new Date(a.creationTime ?? 0).getTime() - new Date(b.creationTime ?? 0).getTime();
    });
    return list;
  });

  // Phase 4C-α (v4.10.0): session-arm equivalent is "session is in Planned status",
  // which is enforced server-side by SessionCreationValidator before this page renders.
  // So for the session arm, treat the gate as always passed; the casual-course arm keeps
  // the original THApproved check.
  isCourseTHApproved = computed(() =>
    this.parentArm() === 'session'
      ? true
      : this.course()?.status === CasualCourseStatus.THApproved);

  citiesForSelected = computed(() => {
    const cid = this.fCountryId();
    if (!cid) return [];
    return this.citiesByCountry()[cid] ?? [];
  });

  ngOnInit(): void {
    const arm = (this.route.snapshot.data['parentArm'] as ParentArm) ?? 'casualCourse';
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.parentArm.set(arm);
    this.parentId.set(id);
    this.embedded.set(!!this.route.snapshot.data['embedded']);

    this.bootstrap();
  }

  private async bootstrap(): Promise<void> {
    this.loading.set(true);
    try {
      const tasks: Promise<unknown>[] = [
        this.loadCourse(),
        this.loadSession(),
        this.loadQuotes(),
        this.loadProviders(),
        this.loadCountries(),
      ];
      await Promise.all(tasks);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadCourse(): Promise<void> {
    if (this.parentArm() !== 'casualCourse') return;
    const detail = await firstValueFrom(this.courseService.getDetail(this.parentId()));
    this.course.set(detail);
  }

  private async loadSession(): Promise<void> {
    if (this.parentArm() !== 'session') return;
    try {
      const detail = await firstValueFrom(this.sessionService.get(this.parentId()));
      this.session.set(detail);
    } catch {
      this.session.set(null);
    }
  }

  private async loadQuotes(): Promise<void> {
    const filter = this.parentArm() === 'casualCourse'
      ? { casualCourseId: this.parentId(), maxResultCount: 200 }
      : { sessionId: this.parentId(), maxResultCount: 200 };
    const result = await firstValueFrom(this.quoteService.getList(filter));
    this.quotes.set(result.items ?? []);
  }

  private async loadProviders(): Promise<void> {
    const result = await firstValueFrom(this.providerService.getList({
      isActive: true, maxResultCount: 500,
    }));
    this.providers.set(result.items ?? []);
  }

  private async loadCountries(): Promise<void> {
    const result = await firstValueFrom(this.geoService.getCountries());
    this.countries.set(result.items ?? []);
  }

  private async ensureCitiesLoaded(countryId: string): Promise<void> {
    if (this.citiesByCountry()[countryId]) return;
    const result = await firstValueFrom(this.geoService.getCities(countryId));
    this.citiesByCountry.update(map => ({ ...map, [countryId]: result.items ?? [] }));
  }

  // ── Provider helpers ──
  providerName(id: string | null | undefined): string {
    if (!id) return '';
    const p = this.providers().find(x => x.id === id);
    return p?.providerNameAr ?? p?.providerNameEn ?? '';
  }

  providerScopeOf(id: string | null | undefined): ProviderScope | undefined {
    if (!id) return undefined;
    return this.providers().find(x => x.id === id)?.scope;
  }

  scopeLabel(scope: ProviderScope | undefined): string {
    switch (scope) {
      case ProviderScope.Internal:      return 'داخلية';
      case ProviderScope.Local:         return 'محلية';
      case ProviderScope.International: return 'دولية';
      default: return '';
    }
  }

  scopeCssClass(scope: ProviderScope | undefined): string {
    switch (scope) {
      case ProviderScope.Internal:      return 'scope-chip scope-internal';
      case ProviderScope.Local:         return 'scope-chip scope-local';
      case ProviderScope.International: return 'scope-chip scope-international';
      default: return 'scope-chip';
    }
  }

  countryNameOf(id: string | null | undefined): string {
    if (!id) return '';
    const c = this.countries().find(x => x.id === id);
    return c?.arabicName ?? c?.englishName ?? '';
  }

  cityNameOf(countryId: string | null | undefined, cityId: string | null | undefined): string {
    if (!countryId || !cityId) return '';
    const cities = this.citiesByCountry()[countryId];
    const c = cities?.find(x => x.id === cityId);
    return c?.arabicName ?? c?.englishName ?? '';
  }

  formatCurrency(value: number | null | undefined): string {
    return (value ?? 0).toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 3 });
  }

  // ── Add/Edit quote ──
  onAddQuote(): void {
    if (!this.ensureQuotesEditable()) return;
    this.isEditMode.set(false);
    this.editingQuoteId.set(null);
    this.fProviderId.set('');
    this.fQuotedPriceOMR.set(0);
    this.fCountryId.set(null);
    this.fCityId.set(null);
    this.fNotes.set('');
    this.saveError.set(null);
    this.quoteDialogOpen.set(true);
  }

  onEditQuote(quote: PriceQuoteDto): void {
    if (!this.ensureQuotesEditable()) return;
    if (quote.isSelected) {
      this.saveError.set(this.l.t('::Training:PriceQuote:CannotEditSelected'));
      return;
    }
    this.isEditMode.set(true);
    this.editingQuoteId.set(quote.id);
    this.fProviderId.set(quote.providerId ?? '');
    this.fQuotedPriceOMR.set(quote.quotedPriceOMR ?? 0);
    this.fCountryId.set(quote.countryId ?? null);
    this.fCityId.set(quote.cityId ?? null);
    this.fNotes.set(quote.notes ?? '');
    this.saveError.set(null);
    if (quote.countryId) {
      this.ensureCitiesLoaded(quote.countryId);
    }
    this.quoteDialogOpen.set(true);
  }

  onCountryChange(countryId: string): void {
    this.fCountryId.set(countryId || null);
    this.fCityId.set(null);
    if (countryId) {
      this.ensureCitiesLoaded(countryId);
    }
  }

  onCityChange(cityId: string): void {
    this.fCityId.set(cityId || null);
  }

  onProviderChange(providerId: string): void {
    this.fProviderId.set(providerId);
  }

  closeQuoteDialog(): void {
    this.quoteDialogOpen.set(false);
    this.saveError.set(null);
  }

  async onSaveQuote(): Promise<void> {
    if (!this.ensureQuotesEditable()) return;
    if (!this.fProviderId()) {
      this.saveError.set('يرجى اختيار جهة التدريب');
      return;
    }
    if ((this.fQuotedPriceOMR() ?? 0) <= 0) {
      this.saveError.set('يرجى إدخال سعر معروض صحيح');
      return;
    }

    const dto: CreateUpdatePriceQuoteDto = {
      casualCourseId: this.parentArm() === 'casualCourse' ? this.parentId() : null,
      sessionId: this.parentArm() === 'session' ? this.parentId() : null,
      providerId: this.fProviderId(),
      quotedPriceOMR: this.fQuotedPriceOMR(),
      countryId: this.fCountryId(),
      cityId: this.fCityId(),
      notes: this.fNotes() || null,
      // Legacy fields kept for session arm; default to 0/0 for casual
      quotedPrice: 0,
      participantsCount: 0,
    };

    try {
      if (this.isEditMode() && this.editingQuoteId()) {
        await firstValueFrom(this.quoteService.update(this.editingQuoteId()!, dto));
      } else {
        await firstValueFrom(this.quoteService.create(dto));
      }
      this.quoteDialogOpen.set(false);
      await this.loadQuotes();
      if (this.embedded()) this.fireShellRefresh();
    } catch (err: unknown) {
      this.saveError.set(this.extractError(err));
    }
  }

  async onDeleteQuote(quote: PriceQuoteDto): Promise<void> {
    if (!this.ensureQuotesEditable()) return;
    if (quote.isSelected) {
      this.saveError.set(this.l.t('::Training:PriceQuote:CannotEditSelected'));
      return;
    }
    if (!confirm('هل أنت متأكد من حذف هذا العرض؟')) return;
    try {
      await firstValueFrom(this.quoteService.delete(quote.id));
      await this.loadQuotes();
      if (this.embedded()) this.fireShellRefresh();
    } catch (err: unknown) {
      this.saveError.set(this.extractError(err));
    }
  }

  // ── Pick Winner ──
  onOpenPickDialog(quote: PriceQuoteDto): void {
    if (!this.ensureQuotesEditable()) return;
    if (!this.isCourseTHApproved()) {
      this.saveError.set(this.l.t('::Training:CasualCourse:NotApprovedYet'));
      return;
    }
    this.pickQuote.set(quote);
    this.fActualStartDate.set(this.expectedDateFrom()?.substring(0, 10) ?? '');
    this.fActualEndDate.set(this.expectedDateTo()?.substring(0, 10) ?? '');
    this.saveError.set(null);
    this.pickDialogOpen.set(true);
  }

  closePickDialog(): void {
    this.pickDialogOpen.set(false);
    this.pickQuote.set(null);
    this.saveError.set(null);
  }

  async onConfirmPickWinner(): Promise<void> {
    if (!this.ensureQuotesEditable()) return;
    const quote = this.pickQuote();
    if (!quote) return;
    if (!this.fActualStartDate() || !this.fActualEndDate()) {
      this.saveError.set('يرجى إدخال تاريخ البدء والانتهاء الفعلي');
      return;
    }
    if (this.fActualEndDate() < this.fActualStartDate()) {
      this.saveError.set(this.l.t('::Training:CasualCourse:InvalidActualDates'));
      return;
    }

    try {
      if (this.parentArm() === 'casualCourse') {
        const dto: SelectPriceQuoteDto = {
          priceQuoteId: quote.id,
          actualStartDate: this.fActualStartDate(),
          actualEndDate: this.fActualEndDate(),
        };
        await firstValueFrom(this.courseService.selectPriceQuote(this.parentId(), dto));
      } else {
        // Phase 4C-α (v4.10.0): atomic session select — flips quote IsSelected,
        // writes session ActualStart/End, transitions Status Planned → Scheduled.
        const dto: SelectSessionPriceQuoteDto = {
          priceQuoteId: quote.id,
          actualStartDate: this.fActualStartDate(),
          actualEndDate: this.fActualEndDate(),
        };
        await firstValueFrom(this.sessionService.selectPriceQuote(this.parentId(), dto));
      }
      this.pickDialogOpen.set(false);
      this.pickQuote.set(null);
      await Promise.all([this.loadCourse(), this.loadSession(), this.loadQuotes()]);
      if (this.embedded()) this.fireShellRefresh();
    } catch (err: unknown) {
      this.saveError.set(this.extractError(err));
    }
  }

  private ensureQuotesEditable(): boolean {
    if (!this.quotesLocked()) return true;
    this.quoteDialogOpen.set(false);
    this.pickDialogOpen.set(false);
    this.saveError.set(this.l.t('::Training:PriceQuote:SessionQuotesLocked'));
    return false;
  }

  goBack(): void {
    if (this.parentArm() === 'casualCourse') {
      this.router.navigate(['/training/casual-courses', this.parentId(), 'details']);
    } else {
      this.router.navigate(['/training']);
    }
  }

  // Phase 4C-α (v4.10.0): polymorphic shell refresh — dispatches to the casual-course or
  // session refresh service based on the route's parentArm. Both services are root-provided
  // so injecting both is harmless even when only one shell is mounted.
  private fireShellRefresh(): void {
    if (this.parentArm() === 'session') {
      this.refreshSessionShell.refresh();
    } else {
      this.refreshShell.refresh();
    }
  }

  private extractError(err: unknown): string {
    if (err && typeof err === 'object') {
      const anyErr = err as { error?: { error?: { message?: string } }; message?: string };
      return anyErr.error?.error?.message ?? anyErr.message ?? 'حدث خطأ أثناء تنفيذ العملية';
    }
    return 'حدث خطأ أثناء تنفيذ العملية';
  }
}
