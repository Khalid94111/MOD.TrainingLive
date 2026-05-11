import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import { firstValueFrom } from 'rxjs';
import {
  DxDataGridModule,
  DxPopupModule,
  DxSelectBoxModule,
  DxNumberBoxModule,
  DxTextBoxModule,
  DxTextAreaModule,
  DxButtonModule,
  DxDateBoxModule,
} from 'devextreme-angular';
import type { ToolbarItem } from 'devextreme/ui/popup';

import { CoursePaymentService } from 'src/app/proxy/training/payments/course-payment.service';
import type {
  CoursePaymentDto,
  CreateUpdateCoursePaymentDto,
  CoursePaymentGetListInput,
  CoursePaymentConfirmResultDto,
} from 'src/app/proxy/training/payments/dtos/models';
import { PaymentStatus } from 'src/app/proxy/training/enums/payment-status.enum';

import { CasualCourseService } from 'src/app/proxy/training/casual-courses';
import type {
  CasualCourseDto,
  CasualCourseDetailDto,
  CasualCourseFinancialItemDto,
} from 'src/app/proxy/training/casual-courses/dtos/models';
import { CasualCourseStatus } from 'src/app/proxy/training/enums/casual-course-status.enum';
import { FundingScenario } from 'src/app/proxy/training/enums/funding-scenario.enum';
import { FinancialAmountSource } from 'src/app/proxy/training/enums/financial-amount-source.enum';

import { TrainingProviderService } from 'src/app/proxy/training/finance';
import type { TrainingProviderDto, PriceQuoteDto } from 'src/app/proxy/training/finance/dtos/models';
import { PriceQuoteService } from 'src/app/proxy/training/finance/price-quote.service';
import { ExchangeRateService } from '../../shared/services/finance-proxy.service';

import { TrainingLocalizationHelper } from '../../shared';

interface DialogState {
  visible: boolean;
  isEdit: boolean;
  id: string | null;
  current: CoursePaymentDto | null;
}

interface UploadState {
  status: 'idle' | 'uploading' | 'uploaded' | 'error';
  fileName: string | null;
  fileSize: number | null;
  progress: number;
  errorMessage: string | null;
}

interface ForecastRow {
  voteCode: string;
  itemName: string;
  amountOMR: number;
}

interface ToastState {
  visible: boolean;
  text: string;
  kind: 'success' | 'error';
}

@Component({
  standalone: true,
  selector: 'app-course-payments',
  templateUrl: './course-payments.component.html',
  styleUrls: [
    './course-payments.component.scss',
    '../../shared/gtms-design.scss',
  ],
  imports: [
    CommonModule,
    LocalizationPipe,
    DxDataGridModule,
    DxPopupModule,
    DxSelectBoxModule,
    DxNumberBoxModule,
    DxTextBoxModule,
    DxTextAreaModule,
    DxButtonModule,
    DxDateBoxModule,
  ],
})
export class CoursePaymentsComponent implements OnInit {
  private paymentService = inject(CoursePaymentService);
  private courseService = inject(CasualCourseService);
  private providerService = inject(TrainingProviderService);
  private quoteService = inject(PriceQuoteService);
  private exchangeService = inject(ExchangeRateService);
  private permissions = inject(PermissionService);
  l = inject(TrainingLocalizationHelper);

  PaymentStatus = PaymentStatus;
  FundingScenario = FundingScenario;

  // ── Lookups & state ──
  rows = signal<CoursePaymentDto[]>([]);
  loading = signal(true);
  approvedCourses = signal<CasualCourseDto[]>([]);
  providers = signal<TrainingProviderDto[]>([]);
  exchangeRate = signal<number>(2.6);

  // ── Filters ──
  filterCourseId = signal<string | null>(null);
  filterStatus = signal<PaymentStatus | null>(null);
  filterProviderId = signal<string | null>(null);

  // ── Permissions ──
  canCreate = computed(() => this.permissions.getGrantedPolicy('TrainingPayments.CoursePayments.Create'));
  canUpdate = computed(() => this.permissions.getGrantedPolicy('TrainingPayments.CoursePayments.Update'));
  canDelete = computed(() => this.permissions.getGrantedPolicy('TrainingPayments.CoursePayments.Delete'));
  canConfirm = computed(() => this.permissions.getGrantedPolicy('TrainingPayments.CoursePayments.Confirm'));
  canUpload = computed(() => this.permissions.getGrantedPolicy('TrainingPayments.CoursePayments.UploadInvoice'));
  canDownload = computed(() => this.permissions.getGrantedPolicy('TrainingPayments.CoursePayments.DownloadInvoice'));

  // ── Dialog ──
  dialog = signal<DialogState>({ visible: false, isEdit: false, id: null, current: null });
  dialogError = signal<string | null>(null);
  dialogSaving = signal(false);
  dialogCourse = signal<CasualCourseDetailDto | null>(null);
  dialogQuoteProviderId = signal<string | null>(null);   // locked from SelectedPriceQuote when present
  upload = signal<UploadState>({ status: 'idle', fileName: null, fileSize: null, progress: 0, errorMessage: null });
  toast = signal<ToastState>({ visible: false, text: '', kind: 'success' });
  isDragging = signal(false);

  // Form
  fCourseId = signal<string | null>(null);
  fProviderId = signal<string | null>(null);
  fInvoiceAmountOMR = signal<number>(0);
  fNebrasAmountOMR = signal<number>(0);
  fInvoiceDate = signal<string>(new Date().toISOString().substring(0, 10));
  fNotes = signal<string>('');

  statusOptions: { value: PaymentStatus; text: string }[] = [];

  // ── Derived ──
  filteredRows = computed(() => {
    const courseId = this.filterCourseId();
    const status = this.filterStatus();
    const providerId = this.filterProviderId();
    return this.rows().filter(r => {
      if (courseId && r.casualCourseId !== courseId) return false;
      if (status !== null && r.status !== status) return false;
      if (providerId && r.trainingProviderId !== providerId) return false;
      return true;
    });
  });

  formVarianceOMR = computed(() => this.fNebrasAmountOMR() - this.fInvoiceAmountOMR());

  /**
   * Courses offered in the dialog picker. Filters out casual courses that
   * already carry a course payment (server enforces the polymorphic-parent
   * uniqueness invariant). In Edit mode the currently-selected course is
   * preserved so the dropdown can render its bound value.
   */
  dialogAvailableCourses = computed<CasualCourseDto[]>(() => {
    const editingCourseId = this.dialog().isEdit
      ? this.dialog().current?.casualCourseId ?? null
      : null;
    const takenIds = new Set(
      this.rows()
        .map(r => r.casualCourseId)
        .filter((id): id is string => !!id && id !== editingCourseId),
    );
    return this.approvedCourses().filter(c => !takenIds.has(c.id ?? ''));
  });

  // Forecast — computed from loaded course detail (client-side preview).
  forecast = computed<ForecastRow[]>(() => {
    const c = this.dialogCourse();
    if (!c) return [];
    const scenario = c.fundingScenario;
    if (scenario === undefined || scenario === null) return [];
    if (scenario === FundingScenario.FundingSourceCoversAll) return [];
    const voteCode = c.fundingSourceVoteCode || '';
    if (!voteCode) return [];
    const items = c.financialItems ?? [];
    return items
      .filter(it => this.itemEligibleForReallocation(it, scenario))
      .map(it => ({
        voteCode,
        itemName: it.financialItemName ?? '—',
        amountOMR: this.sumRanks(it),
      }))
      .filter(r => r.amountOMR > 0);
  });

  forecastTotalOMR = computed(() => this.forecast().reduce((s, r) => s + r.amountOMR, 0));

  forecastApplicable = computed(() => {
    const c = this.dialogCourse();
    if (!c) return false;
    return !!c.id && c.fundingScenario !== undefined && c.fundingScenario !== null;
  });

  scenarioPillCss = computed(() => {
    switch (this.dialogCourse()?.fundingScenario) {
      case FundingScenario.FundingSourceCoversAll:    return 'scenario-pill scenario-1';
      case FundingScenario.FundingSourceCoversCourse: return 'scenario-pill scenario-2';
      case FundingScenario.FinancialItemsCoverAll:    return 'scenario-pill scenario-3';
      default: return 'scenario-pill';
    }
  });

  scenarioLabel = computed(() => {
    switch (this.dialogCourse()?.fundingScenario) {
      case FundingScenario.FundingSourceCoversAll:    return this.l.t('::Training.Payments.CoursePayment.Scenario1');
      case FundingScenario.FundingSourceCoversCourse: return this.l.t('::Training.Payments.CoursePayment.Scenario2');
      case FundingScenario.FinancialItemsCoverAll:    return this.l.t('::Training.Payments.CoursePayment.Scenario3');
      default: return this.l.t('::Training.Payments.CoursePayment.ScenarioUnknown');
    }
  });

  hasInvoice = computed(() => {
    if (this.upload().status === 'uploaded') return true;
    return !!this.dialog().current?.invoiceBlobName;
  });

  confirmDisabled = computed(() => {
    const cur = this.dialog().current;
    if (!cur || !cur.id) return true;
    if (cur.status !== PaymentStatus.Draft) return true;
    if (!this.hasInvoice()) return true;
    return this.dialogSaving();
  });

  /** Toolbar buttons reflect the current row's status:
   *   • Confirmed (or any non-Draft) → only Close, since the record is immutable.
   *   • Draft / new                  → Cancel + Save Draft + Confirm.
   *  Built as a computed so status transitions update the button set live. */
  dialogToolbarItems = computed<ToolbarItem[]>(() => {
    const cur = this.dialog().current;
    const isLocked = !!cur && cur.status !== PaymentStatus.Draft;
    const cancelItem: ToolbarItem = {
      widget: 'dxButton', location: 'after', toolbar: 'bottom',
      options: { text: this.l.t('::Close'), onClick: () => this.closeDialog() },
    };
    if (isLocked) return [cancelItem];
    return [
      { widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Cancel'), onClick: () => this.closeDialog() } },
      { widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Training.Payments.SaveDraft'), onClick: () => this.onSaveDraft() } },
      { widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: {
          text: this.l.t('::Training.Payments.CoursePayment.Dialog.ConfirmActionLabel'),
          type: 'success',
          onClick: () => this.onConfirm(),
        },
      },
    ];
  });

  // ── Lifecycle ──
  async ngOnInit(): Promise<void> {
    this.statusOptions = [
      { value: PaymentStatus.Draft,     text: this.l.t('::Training.PaymentStatus.Draft') },
      { value: PaymentStatus.Confirmed, text: this.l.t('::Training.PaymentStatus.Confirmed') },
      { value: PaymentStatus.Cancelled, text: this.l.t('::Training.PaymentStatus.Cancelled') },
    ];

    await Promise.all([
      this.loadRows(),
      this.loadApprovedCourses(),
      this.loadProviders(),
      this.loadExchangeRate(),
    ]);
  }

  private async loadRows(): Promise<void> {
    this.loading.set(true);
    try {
      const input: CoursePaymentGetListInput = { maxResultCount: 1000 };
      const result = await firstValueFrom(this.paymentService.getList(input));
      this.rows.set(result.items ?? []);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadApprovedCourses(): Promise<void> {
    try {
      const result = await firstValueFrom(
        this.courseService.getList({
          status: [CasualCourseStatus.THApproved],
          maxResultCount: 500,
        }),
      );
      this.approvedCourses.set(result.items ?? []);
    } catch {
      this.approvedCourses.set([]);
    }
  }

  private async loadProviders(): Promise<void> {
    try {
      const list = await firstValueFrom(this.providerService.getAllActive());
      this.providers.set(list ?? []);
    } catch {
      this.providers.set([]);
    }
  }

  private async loadExchangeRate(): Promise<void> {
    try {
      const r = await this.exchangeService.getActive();
      if (r?.rate && r.rate > 0) this.exchangeRate.set(r.rate);
    } catch { /* noop */ }
  }

  // ── Display helpers ──
  toUSD(omr: number | null | undefined): string {
    return ((omr ?? 0) * this.exchangeRate()).toLocaleString('en-US', { maximumFractionDigits: 0 });
  }

  formatOMR(value: number | null | undefined): string {
    return (value ?? 0).toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 });
  }

  varianceCss(v: number | null | undefined): string {
    const n = v ?? 0;
    if (Math.abs(n) < 0.001) return 'variance-equal';
    if (n < 0) return 'variance-down';
    return 'variance-up';
  }

  varianceText(v: number | null | undefined): string {
    const n = v ?? 0;
    if (Math.abs(n) < 0.001) return this.l.t('::Training.Payments.CoursePayment.VarianceEqual');
    const sign = n > 0 ? '+' : '−';
    return `${sign}${this.formatOMR(Math.abs(n))} ر.ع`;
  }

  statusBadgeText(s: PaymentStatus | undefined): string {
    if (s === undefined) return '';
    return this.statusOptions.find(o => o.value === s)?.text ?? '';
  }

  statusBadgeCss(s: PaymentStatus | undefined): string {
    switch (s) {
      case PaymentStatus.Confirmed: return 'status-badge status-confirmed';
      case PaymentStatus.Cancelled: return 'status-badge status-cancelled';
      default: return 'status-badge status-draft';
    }
  }

  providerName(id: string | null | undefined): string {
    if (!id) return '—';
    return this.providers().find(p => p.id === id)?.providerNameAr ?? '—';
  }

  courseLabel(id: string | null | undefined): string {
    if (!id) return '—';
    return this.approvedCourses().find(c => c.id === id)?.courseNameAr ?? '—';
  }

  formatFileSize(bytes: number | null | undefined): string {
    const b = bytes ?? 0;
    if (b < 1024) return `${b} B`;
    if (b < 1024 * 1024) return `${(b / 1024).toFixed(1)} KB`;
    return `${(b / (1024 * 1024)).toFixed(1)} MB`;
  }

  // ── Forecast helpers ──
  private itemEligibleForReallocation(item: CasualCourseFinancialItemDto, scenario: FundingScenario): boolean {
    if (scenario === FundingScenario.FundingSourceCoversCourse) {
      return item.source !== FinancialAmountSource.FundingSource;
    }
    if (scenario === FundingScenario.FinancialItemsCoverAll) return true;
    return false;
  }

  private sumRanks(item: CasualCourseFinancialItemDto): number {
    return (item.ranks ?? []).reduce((s, r) => s + (r.subtotalOMR ?? 0), 0);
  }

  // ── Filter handlers ──
  onCourseFilterChange(v: string | null): void { this.filterCourseId.set(v); }
  onStatusFilterChange(v: PaymentStatus | null): void { this.filterStatus.set(v); }
  onProviderFilterChange(v: string | null): void { this.filterProviderId.set(v); }
  clearFilters(): void {
    this.filterCourseId.set(null);
    this.filterStatus.set(null);
    this.filterProviderId.set(null);
  }

  // ── Dialog flow ──
  async onAdd(): Promise<void> {
    if (!this.canCreate()) return;
    this.resetForm();
    this.dialog.set({ visible: true, isEdit: false, id: null, current: null });
    this.dialogCourse.set(null);
    this.dialogQuoteProviderId.set(null);
    this.upload.set({ status: 'idle', fileName: null, fileSize: null, progress: 0, errorMessage: null });
    const courseId = this.filterCourseId();
    if (courseId) {
      this.fCourseId.set(courseId);
      await this.loadCourseContext(courseId);
    }
  }

  async onEdit(row: CoursePaymentDto): Promise<void> {
    if (!this.canUpdate() || row.status !== PaymentStatus.Draft) return;
    await this.openExistingPayment(row);
  }

  async onView(row: CoursePaymentDto): Promise<void> {
    await this.openExistingPayment(row);
  }

  private async openExistingPayment(row: CoursePaymentDto): Promise<void> {
    this.dialog.set({ visible: true, isEdit: true, id: row.id, current: row });
    this.dialogError.set(null);
    this.fCourseId.set(row.casualCourseId ?? null);
    this.fProviderId.set(row.trainingProviderId ?? null);
    this.fInvoiceAmountOMR.set(row.invoiceAmountOMR ?? 0);
    this.fNebrasAmountOMR.set(row.nebrasAmountOMR ?? 0);
    this.fInvoiceDate.set((row.invoiceDate ?? '').substring(0, 10));
    this.fNotes.set(row.notes ?? '');
    this.upload.set({
      status: row.invoiceBlobName ? 'uploaded' : 'idle',
      fileName: row.invoiceOriginalFileName ?? null,
      fileSize: null,
      progress: row.invoiceBlobName ? 100 : 0,
      errorMessage: null,
    });
    if (row.casualCourseId) await this.loadCourseContext(row.casualCourseId);
  }

  closeDialog(): void {
    this.dialog.set({ visible: false, isEdit: false, id: null, current: null });
    this.dialogError.set(null);
    this.dialogCourse.set(null);
    this.dialogQuoteProviderId.set(null);
    this.upload.set({ status: 'idle', fileName: null, fileSize: null, progress: 0, errorMessage: null });
  }

  private async loadCourseContext(courseId: string): Promise<void> {
    try {
      const detail = await firstValueFrom(this.courseService.getDetail(courseId));
      this.dialogCourse.set(detail);
      // If the course has a SelectedPriceQuote, lock the provider to its provider.
      if (detail.selectedPriceQuoteId) {
        try {
          const quote = await firstValueFrom(this.quoteService.get(detail.selectedPriceQuoteId));
          if (quote?.providerId) {
            this.dialogQuoteProviderId.set(quote.providerId);
            // Only auto-fill provider on Create; don't override an explicit Edit selection.
            if (!this.dialog().isEdit) this.fProviderId.set(quote.providerId);
          }
        } catch { /* noop */ }
      }
    } catch {
      this.dialogCourse.set(null);
      this.dialogQuoteProviderId.set(null);
    }
  }

  async onDialogCourseChange(courseId: string | null): Promise<void> {
    this.fCourseId.set(courseId);
    this.dialogCourse.set(null);
    this.dialogQuoteProviderId.set(null);
    if (!this.dialog().isEdit) this.fProviderId.set(null);
    if (courseId) await this.loadCourseContext(courseId);
  }

  // ── Save / Confirm / Delete ──
  private buildDto(): CreateUpdateCoursePaymentDto {
    return {
      casualCourseId: this.fCourseId(),
      sessionId: null,
      trainingProviderId: this.dialogQuoteProviderId() ?? this.fProviderId() ?? '',
      invoiceAmountOMR: this.fInvoiceAmountOMR(),
      nebrasAmountOMR: this.fNebrasAmountOMR(),
      invoiceDate: this.fInvoiceDate(),
      notes: this.fNotes() || null,
    };
  }

  async onSaveDraft(): Promise<void> {
    if (!this.fCourseId()) {
      this.dialogError.set(this.l.t('::Training.Payments.CoursePayment.Dialog.PickCourseFirst'));
      return;
    }
    if (!this.fProviderId() && !this.dialogQuoteProviderId()) {
      this.dialogError.set(this.l.t('::Training.Payments.CoursePayment.Dialog.ProviderRequired'));
      return;
    }
    if (!this.fInvoiceDate()) {
      this.dialogError.set(this.l.t('::Training.Payments.CoursePayment.Dialog.InvoiceDateRequired'));
      return;
    }
    this.dialogError.set(null);
    this.dialogSaving.set(true);
    try {
      const dto = this.buildDto();
      let saved: CoursePaymentDto;
      if (this.dialog().isEdit && this.dialog().id) {
        saved = await firstValueFrom(this.paymentService.update(this.dialog().id!, dto));
      } else {
        saved = await firstValueFrom(this.paymentService.create(dto));
      }
      this.dialog.set({ visible: true, isEdit: true, id: saved.id, current: saved });
      this.fInvoiceAmountOMR.set(saved.invoiceAmountOMR ?? 0);
      this.fNebrasAmountOMR.set(saved.nebrasAmountOMR ?? 0);
      await this.loadRows();
      this.showToast(this.l.t('::Training.Payments.CoursePayment.Dialog.SaveDraftSuccess'), 'success');
    } catch (err) {
      this.dialogError.set(this.extractError(err));
    } finally {
      this.dialogSaving.set(false);
    }
  }

  async onConfirm(): Promise<void> {
    const id = this.dialog().id;
    if (!id) {
      this.dialogError.set(this.l.t('::Training.Payments.SaveBeforeAction'));
      return;
    }
    if (!this.hasInvoice()) {
      this.dialogError.set(this.l.t('::Training.Payments.CoursePayment.Dialog.UploadRequired'));
      return;
    }
    if (this.dialog().current?.status !== PaymentStatus.Draft) {
      this.dialogError.set(this.l.t('::Training.Payments.CoursePayment.Dialog.OnlyDraftConfirmable'));
      return;
    }
    if (!confirm(this.l.t('::Training.Payments.CoursePayment.Dialog.ConfirmPrompt'))) return;
    this.dialogError.set(null);
    this.dialogSaving.set(true);
    try {
      // Persist any pending edits first.
      const dto = this.buildDto();
      await firstValueFrom(this.paymentService.update(id, dto));
      const result: CoursePaymentConfirmResultDto = await firstValueFrom(this.paymentService.confirm(id));
      const count = result.generatedReallocationsCount ?? 0;
      await this.loadRows();
      // Confirm is terminal — close the dialog and surface the result via toast.
      this.closeDialog();
      const toastMsg = count > 0
        ? this.l.t('::Training.Payments.CoursePayment.Dialog.ConfirmedToastWithCount').replace('{0}', String(count))
        : this.l.t('::Training.Payments.CoursePayment.Dialog.ConfirmedToastNoReallocations');
      this.showToast(toastMsg, 'success');
    } catch (err) {
      this.dialogError.set(this.extractError(err));
    } finally {
      this.dialogSaving.set(false);
    }
  }

  async onDelete(row: CoursePaymentDto): Promise<void> {
    if (!this.canDelete() || row.status !== PaymentStatus.Draft) return;
    const prompt = `${this.l.t('::Training.Payments.CoursePayment.Dialog.DeletePromptPrefix')} "${row.courseNameAr ?? ''}"؟`;
    if (!confirm(prompt)) return;
    await firstValueFrom(this.paymentService.delete(row.id));
    await this.loadRows();
  }

  // ── Upload flow ──
  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;
    input.value = '';   // allow same file to be re-selected later
    if (file) await this.handleFile(file);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(true);
  }
  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
  }
  async onDrop(event: DragEvent): Promise<void> {
    event.preventDefault();
    this.isDragging.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (file) await this.handleFile(file);
  }

  private async handleFile(file: File): Promise<void> {
    if (!this.dialog().id) {
      this.dialogError.set(this.l.t('::Training.Payments.CoursePayment.Dialog.SaveBeforeUpload'));
      return;
    }
    if (file.type !== 'application/pdf' && !file.name.toLowerCase().endsWith('.pdf')) {
      this.upload.set({ status: 'error', fileName: file.name, fileSize: file.size, progress: 0, errorMessage: this.l.t('::Training.Payments.CoursePayment.Dialog.UploadOnlyPdf') });
      return;
    }
    if (file.size > 25 * 1024 * 1024) {
      this.upload.set({ status: 'error', fileName: file.name, fileSize: file.size, progress: 0, errorMessage: this.l.t('::Training.Payments.CoursePayment.Dialog.UploadTooLarge') });
      return;
    }
    this.upload.set({ status: 'uploading', fileName: file.name, fileSize: file.size, progress: 50, errorMessage: null });
    try {
      const formData = new FormData();
      formData.append('file', file, file.name);
      const updated = await firstValueFrom(this.paymentService.uploadInvoice(this.dialog().id!, formData));
      this.dialog.update(d => ({ ...d, current: updated }));
      this.upload.set({
        status: 'uploaded',
        fileName: updated.invoiceOriginalFileName ?? file.name,
        fileSize: file.size,
        progress: 100,
        errorMessage: null,
      });
      await this.loadRows();
    } catch (err) {
      this.upload.set({ status: 'error', fileName: file.name, fileSize: file.size, progress: 0, errorMessage: this.extractError(err) });
    }
  }

  async onDownloadInvoice(row: CoursePaymentDto | null): Promise<void> {
    const target = row ?? this.dialog().current;
    if (!target?.id || !target.invoiceBlobName) return;
    try {
      const blob = await firstValueFrom(this.paymentService.downloadInvoice(target.id));
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = target.invoiceOriginalFileName ?? 'invoice.pdf';
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
    } catch (err) {
      this.showToast(this.extractError(err), 'error');
    }
  }

  // ── Form field setters ──
  updateCourseId(v: string | null): void { void this.onDialogCourseChange(v); }
  updateProviderId(v: string | null): void { this.fProviderId.set(v); }
  updateInvoiceAmount(v: number | null): void { this.fInvoiceAmountOMR.set(v ?? 0); }
  updateNebrasAmount(v: number | null): void { this.fNebrasAmountOMR.set(v ?? 0); }
  updateInvoiceDate(v: string | null | Date): void {
    if (!v) { this.fInvoiceDate.set(''); return; }
    const iso = typeof v === 'string' ? v : v.toISOString();
    this.fInvoiceDate.set(iso.substring(0, 10));
  }
  updateNotes(v: string): void { this.fNotes.set(v ?? ''); }

  private resetForm(): void {
    this.fCourseId.set(null);
    this.fProviderId.set(null);
    this.fInvoiceAmountOMR.set(0);
    this.fNebrasAmountOMR.set(0);
    this.fInvoiceDate.set(new Date().toISOString().substring(0, 10));
    this.fNotes.set('');
    this.dialogError.set(null);
  }

  private showToast(text: string, kind: 'success' | 'error'): void {
    this.toast.set({ visible: true, text, kind });
    setTimeout(() => this.toast.set({ visible: false, text: '', kind }), 4500);
  }

  dismissToast(): void {
    this.toast.update(t => ({ ...t, visible: false }));
  }

  private extractError(err: unknown): string {
    const fallback = this.l.t('::Training.Payments.GenericError');
    if (err && typeof err === 'object') {
      const anyErr = err as { error?: { error?: { message?: string } }; message?: string };
      return anyErr.error?.error?.message ?? anyErr.message ?? fallback;
    }
    return fallback;
  }
}
