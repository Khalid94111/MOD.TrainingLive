import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';
import { PriceQuoteService, TrainingProviderService } from 'src/app/proxy/training/finance';
import { PriceQuoteDto, TrainingProviderDto, CreateUpdatePriceQuoteDto } from 'src/app/proxy/training/finance/dtos';
import { CourseSessionService } from 'src/app/proxy/training/plans';
import { CourseSessionDto } from 'src/app/proxy/training/plans/dtos';
import { TrainingLocalizationHelper, ConfirmDialogComponent } from '../../shared';
import { ApprovalStatus, PricingType } from '../../shared/models/training-enums';

@Component({
  standalone: true,
  selector: 'app-price-quote-list',
  templateUrl: './price-quote-list.component.html',
  styleUrl: './price-quote-list.component.scss',
  imports: [CommonModule, LocalizationPipe, ConfirmDialogComponent],
})
export class PriceQuoteListComponent implements OnInit {
  private readonly quoteService = inject(PriceQuoteService);
  private readonly providerService = inject(TrainingProviderService);
  private readonly sessionService = inject(CourseSessionService);
  private readonly permissionService = inject(PermissionService);
  private readonly toaster = inject(ToasterService);
  private readonly l = inject(TrainingLocalizationHelper);

  readonly ApprovalStatus = ApprovalStatus;
  readonly PricingType = PricingType;

  // ── Data ──
  quotes = signal<PriceQuoteDto[]>([]);
  providers = signal<TrainingProviderDto[]>([]);
  sessions = signal<CourseSessionDto[]>([]);
  isLoading = signal(false);

  // ── Filters ──
  searchText = signal('');
  filterStatus = signal<ApprovalStatus | null>(null);

  // ── Dialog ──
  isDialogVisible = signal(false);
  isEditMode = signal(false);
  selectedQuoteId = signal<string | null>(null);
  isDeleteDialogVisible = signal(false);
  quoteToDelete = signal<PriceQuoteDto | null>(null);
  formData = signal<CreateUpdatePriceQuoteDto>({
    sessionId: undefined,
    providerId: '',
    pricingType: PricingType.PerPerson,
    quotedPrice: 0,
    participantsCount: 1,
    notes: undefined,
  });
  isSaving = signal(false);
  validationErrors = signal<string[]>([]);

  // ── Permissions ──
  canCreate = computed(() => this.permissionService.getGrantedPolicy('TrainingExecution.PriceQuotes.Create'));
  canUpdate = computed(() => this.permissionService.getGrantedPolicy('TrainingExecution.PriceQuotes.Edit'));
  canDelete = computed(() => this.permissionService.getGrantedPolicy('TrainingExecution.PriceQuotes.Delete'));
  canApprove = computed(() => this.permissionService.getGrantedPolicy('TrainingExecution.PriceQuotes.Approve'));

  // ── Computed stats / filtered list ──
  filteredQuotes = computed(() => {
    let list = this.quotes();
    const term = this.searchText().trim().toLowerCase();
    if (term) {
      list = list.filter(q =>
        (q.courseName?.toLowerCase().includes(term) ?? false) ||
        (q.providerName?.toLowerCase().includes(term) ?? false)
      );
    }
    if (this.filterStatus() !== null) {
      list = list.filter(q => q.status === this.filterStatus());
    }
    return list;
  });

  totalCount = computed(() => this.filteredQuotes().length);
  pendingCount = computed(() => this.filteredQuotes().filter(q => q.status === ApprovalStatus.Pending).length);
  approvedCount = computed(() => this.filteredQuotes().filter(q => q.status === ApprovalStatus.Approved).length);
  rejectedCount = computed(() => this.filteredQuotes().filter(q => q.status === ApprovalStatus.Rejected).length);

  pricingTypeOptions = computed(() => [
    { value: PricingType.PerPerson, text: this.l.t('::Training.PricingType.PerPerson') },
    { value: PricingType.Total, text: this.l.t('::Training.PricingType.Total') },
  ]);

  get dialogTitle(): string {
    return this.isEditMode()
      ? this.l.t('::Training.PriceQuote')
      : this.l.t('::Training.CreatePriceQuote');
  }

  async ngOnInit(): Promise<void> {
    await this.loadProviders();
    await this.loadSessions();
    await this.loadQuotes();
  }

  async loadQuotes(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await firstValueFrom(this.quoteService.getList({
        maxResultCount: 1000,
        skipCount: 0,
      }));
      this.quotes.set(result.items ?? []);
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
    } finally {
      this.isLoading.set(false);
    }
  }

  async loadProviders(): Promise<void> {
    try {
      const result = await firstValueFrom(this.providerService.getAllActive());
      this.providers.set(result);
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
    }
  }

  async loadSessions(): Promise<void> {
    try {
      const result = await firstValueFrom(this.sessionService.getList({ maxResultCount: 1000, skipCount: 0 }));
      this.sessions.set(result.items ?? []);
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
    }
  }

  // ── Actions ──
  onAdd(): void {
    this.isEditMode.set(false);
    this.selectedQuoteId.set(null);
    this.validationErrors.set([]);
    this.formData.set({
      sessionId: undefined,
      providerId: '',
      pricingType: PricingType.PerPerson,
      quotedPrice: 0,
      participantsCount: 1,
      notes: undefined,
    });
    this.isDialogVisible.set(true);
  }

  onEdit(quote: PriceQuoteDto): void {
    if (quote.isSelected) {
      this.toaster.warn(this.l.t('::Training:PriceQuote:CannotEditSelected'));
      return;
    }
    this.isEditMode.set(true);
    this.selectedQuoteId.set(quote.id);
    this.validationErrors.set([]);
    this.formData.set({
      sessionId: quote.sessionId,
      providerId: quote.providerId,
      pricingType: quote.pricingType,
      quotedPrice: quote.quotedPrice,
      participantsCount: quote.participantsCount,
      notes: quote.notes,
    });
    this.isDialogVisible.set(true);
  }

  async onSave(): Promise<void> {
    if (!this.validateForm()) return;

    this.isSaving.set(true);
    try {
      const data = this.formData();
      if (this.isEditMode() && this.selectedQuoteId()) {
        await firstValueFrom(this.quoteService.update(this.selectedQuoteId()!, data));
        this.toaster.success(this.l.t('::Training.Common.Save'));
      } else {
        await firstValueFrom(this.quoteService.create(data));
        this.toaster.success(this.l.t('::Training.Common.Save'));
      }
      this.isDialogVisible.set(false);
      await this.loadQuotes();
    } catch {
      this.toaster.error(this.l.t('::Training.Errors.Generic'));
    } finally {
      this.isSaving.set(false);
    }
  }

  onDelete(quote: PriceQuoteDto): void {
    if (quote.isSelected) {
      this.toaster.warn(this.l.t('::Training:PriceQuote:CannotEditSelected'));
      return;
    }
    this.quoteToDelete.set(quote);
    this.isDeleteDialogVisible.set(true);
  }

  async onConfirmDelete(): Promise<void> {
    const quote = this.quoteToDelete();
    if (!quote) return;
    try {
      await firstValueFrom(this.quoteService.delete(quote.id));
      this.toaster.success(this.l.t('::Training.Common.Delete'));
      this.isDeleteDialogVisible.set(false);
      this.quoteToDelete.set(null);
      await this.loadQuotes();
    } catch {
      this.toaster.error(this.l.t('::Training.Errors.Generic'));
    }
  }

  onCancelDelete(): void {
    this.isDeleteDialogVisible.set(false);
    this.quoteToDelete.set(null);
  }

  deleteTargetLabel(): string {
    const q = this.quoteToDelete();
    if (!q) return '';
    const provider = q.providerName || this.providerLabel(q.providerId);
    const course = q.courseName || this.sessionLabel(q.sessionId);
    return [provider, course].filter(Boolean).join(' — ');
  }

  async onApprove(id: string): Promise<void> {
    try {
      await firstValueFrom(this.quoteService.approve(id));
      this.toaster.success(this.l.t('::Training.Approved'));
      await this.loadQuotes();
    } catch {
      this.toaster.error(this.l.t('::Training.Errors.Generic'));
    }
  }

  async onReject(id: string): Promise<void> {
    try {
      await firstValueFrom(this.quoteService.reject(id));
      this.toaster.success(this.l.t('::Training.Rejected'));
      await this.loadQuotes();
    } catch {
      this.toaster.error(this.l.t('::Training.Errors.Generic'));
    }
  }

  // ── Form helpers ──
  private validateForm(): boolean {
    const errors: string[] = [];
    const data = this.formData();
    if (!data.sessionId) {
      errors.push(`${this.l.t('::Training.Session')} ${this.l.t('::Training.Common.Required')}`);
    }
    if (!data.providerId) {
      errors.push(`${this.l.t('::Training.TrainingProvider')} ${this.l.t('::Training.Common.Required')}`);
    }
    if (data.quotedPrice < 0) {
      errors.push(this.l.t('::Training.Payments.GenericError'));
    }
    if (data.participantsCount < 1) {
      errors.push(this.l.t('::Training.Payments.GenericError'));
    }
    this.validationErrors.set(errors);
    return errors.length === 0;
  }

  updateSessionId(value: string): void {
    this.formData.update(f => ({ ...f, sessionId: value || undefined }));
  }

  updateProviderId(value: string): void {
    this.formData.update(f => ({ ...f, providerId: value }));
  }

  updatePricingType(value: number): void {
    this.formData.update(f => ({ ...f, pricingType: value }));
  }

  updateQuotedPrice(value: number): void {
    this.formData.update(f => ({ ...f, quotedPrice: value ?? 0 }));
  }

  updateParticipantsCount(value: number): void {
    this.formData.update(f => ({ ...f, participantsCount: value ?? 1 }));
  }

  updateNotes(value: string): void {
    this.formData.update(f => ({ ...f, notes: value || undefined }));
  }

  setFilterStatus(status: ApprovalStatus | null): void {
    this.filterStatus.set(status);
  }

  onSearch(): void {
    // filteredQuotes is computed — searchText already bound via input event.
  }

  // ── Display helpers ──
  formatMoney(value?: number | null): string {
    if (value === undefined || value === null) return '0.000';
    return value.toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 });
  }

  pricingTypeLabel(type: PricingType): string {
    return type === PricingType.PerPerson
      ? this.l.t('::Training.PricingType.PerPerson')
      : this.l.t('::Training.PricingType.Total');
  }

  statusBadge(status: ApprovalStatus): { css: string; label: string } {
    switch (status) {
      case ApprovalStatus.Approved:
        return { css: 'status-pill status-approved', label: this.l.t('::Training.Approved') };
      case ApprovalStatus.Rejected:
        return { css: 'status-pill status-rejected', label: this.l.t('::Training.Rejected') };
      case ApprovalStatus.Returned:
        return { css: 'status-pill status-returned', label: this.l.t('::Training.Return') };
      default:
        return { css: 'status-pill status-pending', label: this.l.t('::Training.Pending') };
    }
  }

  sessionLabel(sessionId?: string | null): string {
    if (!sessionId) return '—';
    const session = this.sessions().find(s => s.id === sessionId);
    return session?.tenantCourseNameAr ?? sessionId;
  }

  providerLabel(providerId?: string | null): string {
    if (!providerId) return '—';
    const provider = this.providers().find(p => p.id === providerId);
    return provider?.providerNameAr ?? providerId;
  }
}
