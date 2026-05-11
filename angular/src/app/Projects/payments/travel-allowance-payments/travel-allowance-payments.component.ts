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
} from 'devextreme-angular';
import type { ToolbarItem } from 'devextreme/ui/popup';

import { TravelAllowancePaymentService } from 'src/app/proxy/training/payments/travel-allowance-payment.service';
import type {
  TravelAllowancePaymentDto,
  CreateUpdateTravelAllowancePaymentDto,
  TravelAllowancePaymentGetListInput,
} from 'src/app/proxy/training/payments/dtos/models';
import { PaymentStatus } from 'src/app/proxy/training/enums/payment-status.enum';
import { PersonnelType } from 'src/app/proxy/training/enums/personnel-type.enum';

import { CasualCourseService } from 'src/app/proxy/training/casual-courses';
import type {
  CasualCourseDto,
  CasualCourseDetailDto,
  CasualCourseNominationDto,
} from 'src/app/proxy/training/casual-courses/dtos/models';
import { CasualCourseStatus } from 'src/app/proxy/training/enums/casual-course-status.enum';
import { ExchangeRateService } from '../../shared/services/finance-proxy.service';
import type { ExchangeRateDto, PriceQuoteDto } from 'src/app/proxy/training/finance/dtos/models';
import { PriceQuoteService } from 'src/app/proxy/training/finance/price-quote.service';
import { TravelInstructionService } from 'src/app/proxy/training/execution/travel-instruction.service';
import type { TravelInstructionDto } from 'src/app/proxy/training/execution/dtos/models';
import { TrainingLocalizationHelper } from '../../shared';

interface DialogState {
  visible: boolean;
  isEdit: boolean;
  id: string | null;
  current: TravelAllowancePaymentDto | null;
}

@Component({
  standalone: true,
  selector: 'app-travel-allowance-payments',
  templateUrl: './travel-allowance-payments.component.html',
  styleUrls: [
    './travel-allowance-payments.component.scss',
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
  ],
})
export class TravelAllowancePaymentsComponent implements OnInit {
  private paymentService = inject(TravelAllowancePaymentService);
  private courseService = inject(CasualCourseService);
  private quoteService = inject(PriceQuoteService);
  private travelInstructionService = inject(TravelInstructionService);
  private exchangeService = inject(ExchangeRateService);
  private permissions = inject(PermissionService);
  l = inject(TrainingLocalizationHelper);

  PaymentStatus = PaymentStatus;
  PersonnelType = PersonnelType;

  // ── Lookups & filters ──
  rows = signal<TravelAllowancePaymentDto[]>([]);
  loading = signal(true);
  approvedCourses = signal<CasualCourseDto[]>([]);
  exchangeRate = signal<number>(2.6);   // OMR → USD; reset on load

  filterCourseId = signal<string | null>(null);
  filterStatus = signal<PaymentStatus | null>(null);
  filterPersonnelType = signal<PersonnelType | null>(null);
  filterSearch = signal<string>('');

  // ── Permissions ──
  canCreate = computed(() => this.permissions.getGrantedPolicy('TrainingPayments.TravelAllowance.Create'));
  canUpdate = computed(() => this.permissions.getGrantedPolicy('TrainingPayments.TravelAllowance.Update'));
  canDelete = computed(() => this.permissions.getGrantedPolicy('TrainingPayments.TravelAllowance.Delete'));
  canConfirm = computed(() => this.permissions.getGrantedPolicy('TrainingPayments.TravelAllowance.Confirm'));

  // ── Dialog ──
  dialog = signal<DialogState>({ visible: false, isEdit: false, id: null, current: null });
  dialogError = signal<string | null>(null);
  dialogSaving = signal(false);

  /** Toolbar buttons reflect the current row's status:
   *   • Confirmed (or any non-Draft) → only Close, since the record is immutable.
   *   • Draft / new                  → Cancel + Save Draft + Confirm.
   *  Built as a computed so status transitions update the button set live. */
  dialogToolbarItems = computed<ToolbarItem[]>(() => {
    const cur = this.dialog().current;
    const isLocked = !!cur && cur.status !== PaymentStatus.Draft;
    const closeItem: ToolbarItem = {
      widget: 'dxButton', location: 'after', toolbar: 'bottom',
      options: { text: this.l.t('::Close'), onClick: () => this.closeDialog() },
    };
    if (isLocked) return [closeItem];
    return [
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: { text: this.l.t('::Cancel'), onClick: () => this.closeDialog() },
      },
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: { text: this.l.t('::Training.Payments.SaveDraft'), onClick: () => this.onSaveDraft() },
      },
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: {
          text: this.l.t('::Training.Payments.TravelAllowance.Dialog.ConfirmActionLabel'),
          type: 'success',
          onClick: () => this.onConfirm(),
        },
      },
    ];
  });

  // Course context loaded when dialog opens (for nomination dropdown + auto-default banner)
  dialogCourse = signal<CasualCourseDetailDto | null>(null);
  /** Travel instruction for the chosen course — surfaces visa/insurance/ticket flags + dates. */
  dialogTravelInstruction = signal<TravelInstructionDto | null>(null);
  /** The course's winning price quote — surfaces provider name, country/city, and quoted price. */
  dialogSelectedQuote = signal<PriceQuoteDto | null>(null);

  /**
   * Nominees offered in the picker. Filters out anyone who already has a
   * travel-allowance payment for this course (server enforces uniqueness via
   * `Training:TravelAllowancePayment:DuplicateForNomination`). In Edit mode
   * the currently-selected nominee is preserved so the dropdown shows its value.
   */
  dialogNominees = computed<CasualCourseNominationDto[]>(() => {
    const detail = this.dialogCourse();
    if (!detail) return [];
    const courseId = detail.id;
    const editingNominationId = this.dialog().isEdit
      ? this.dialog().current?.nominationId ?? null
      : null;
    const takenIds = new Set(
      this.rows()
        .filter(r => r.casualCourseId === courseId)
        .map(r => r.nominationId)
        .filter((id): id is string => !!id && id !== editingNominationId),
    );
    return (detail.nominations ?? []).filter(n => !takenIds.has(n.id ?? ''));
  });

  // Form fields — individual signals per BRD frontend conventions.
  fNominationId = signal<string | null>(null);
  fTicketAmountOMR = signal<number>(0);
  // Daily rate is the user-editable input; the travel-allowance total below is
  // derived (effectiveTravelDays × rate) and sent to the backend as the total.
  fDailyAllowanceRateOMR = signal<number>(0);
  fTravelAllowanceOMR = signal<number>(0);
  fClothingAllowanceOMR = signal<number>(0);
  fInsuranceOMR = signal<number>(0);
  fVisaFeesOMR = signal<number>(0);
  fNotes = signal<string>('');

  // ── Filter options (resolved at ngOnInit via TrainingLocalizationHelper). ──
  statusOptions: { value: PaymentStatus; text: string }[] = [];
  personnelOptions: { value: PersonnelType; text: string }[] = [];

  // ── Derived ──
  filteredRows = computed(() => {
    const courseId = this.filterCourseId();
    const status = this.filterStatus();
    const personnel = this.filterPersonnelType();
    const q = this.filterSearch().trim().toLowerCase();

    return this.rows().filter(r => {
      if (courseId && r.casualCourseId !== courseId) return false;
      if (status !== null && r.status !== status) return false;
      if (personnel !== null && r.personnelType !== personnel) return false;
      if (q) {
        const hay = `${r.employeeNameAr ?? ''} ${r.rankNameAr ?? ''} ${r.courseNameAr ?? ''}`.toLowerCase();
        if (!hay.includes(q)) return false;
      }
      return true;
    });
  });

  totalRow = computed(() => {
    const sum = this.filteredRows().reduce((acc, r) => acc + (r.totalOMR ?? 0), 0);
    return sum;
  });

  formTotalOMR = computed(() =>
    this.fTicketAmountOMR()
    + this.fTravelAllowanceOMR()
    + this.fClothingAllowanceOMR()
    + this.fInsuranceOMR()
    + this.fVisaFeesOMR(),
  );

  /** Course duration in days — prefers the course's own field, falls back to a
   *  date-range diff if both ends are present. */
  courseDurationDays = computed<number>(() => {
    const c = this.dialogCourse();
    if (!c) return 0;
    if ((c.durationDays ?? 0) > 0) return c.durationDays!;
    const from = c.actualStartDate ?? c.estimatedDateFrom ?? null;
    const to   = c.actualEndDate   ?? c.estimatedDateTo   ?? null;
    return this.daysBetween(from, to);
  });

  /** Single resolved course date range — prefers actual over estimated. Returns
   *  null when neither pair is present so the template can hide the row. */
  courseDateRange = computed<{ from: string; to: string; actual: boolean } | null>(() => {
    const c = this.dialogCourse();
    if (!c) return null;
    if (c.actualStartDate && c.actualEndDate) {
      return { from: c.actualStartDate, to: c.actualEndDate, actual: true };
    }
    if (c.estimatedDateFrom && c.estimatedDateTo) {
      return { from: c.estimatedDateFrom, to: c.estimatedDateTo, actual: false };
    }
    return null;
  });

  /** Effective travel days from the loaded TI; falls back to the editing payment's
   *  enriched value when the TI fetch failed. */
  contextEffectiveTravelDays = computed<number>(() => {
    const ti = this.dialogTravelInstruction();
    if (ti?.effectiveTravelDays && ti.effectiveTravelDays > 0) return ti.effectiveTravelDays;
    const c = this.dialog().current;
    return c?.effectiveTravelDays ?? 0;
  });

  /** Outbound trip span (departure → arrival back) in days, including endpoints. */
  travelSpanDays = computed<number>(() => {
    const ti = this.dialogTravelInstruction();
    if (!ti) return 0;
    return this.daysBetween(ti.departureDate ?? null, ti.arrivalBackDate ?? null);
  });

  selectedNominee = computed<CasualCourseNominationDto | null>(() => {
    const id = this.fNominationId();
    if (!id) return null;
    return this.dialogNominees().find(n => n.id === id) ?? null;
  });

  hasTravelInstruction = computed(() => {
    // Server returns EffectiveTravelDays > 0 only after TI is issued; use it as a proxy.
    const c = this.dialog().current;
    return (c?.effectiveTravelDays ?? 0) > 0;
  });

  // ── Lifecycle ──
  async ngOnInit(): Promise<void> {
    this.statusOptions = [
      { value: PaymentStatus.Draft,     text: this.l.t('::Training.PaymentStatus.Draft') },
      { value: PaymentStatus.Confirmed, text: this.l.t('::Training.PaymentStatus.Confirmed') },
      { value: PaymentStatus.Cancelled, text: this.l.t('::Training.PaymentStatus.Cancelled') },
    ];
    this.personnelOptions = [
      { value: PersonnelType.Officer,  text: this.l.t('::Training.PersonnelType.Officer') },
      { value: PersonnelType.Enlisted, text: this.l.t('::Training.PersonnelType.Enlisted') },
    ];

    await Promise.all([this.loadRows(), this.loadApprovedCourses(), this.loadExchangeRate()]);
  }

  private async loadRows(): Promise<void> {
    this.loading.set(true);
    try {
      const input: TravelAllowancePaymentGetListInput = { maxResultCount: 1000 };
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

  private async loadExchangeRate(): Promise<void> {
    try {
      const rate = await this.exchangeService.getActive();
      if (rate?.rate && rate.rate > 0) this.exchangeRate.set(rate.rate);
    } catch {
      // keep default 2.6
    }
  }

  // ── Filter handlers ──
  onCourseFilterChange(value: string | null): void {
    this.filterCourseId.set(value);
  }
  onStatusFilterChange(value: PaymentStatus | null): void {
    this.filterStatus.set(value);
  }
  onPersonnelFilterChange(value: PersonnelType | null): void {
    this.filterPersonnelType.set(value);
  }
  onSearchInput(value: string): void {
    this.filterSearch.set(value ?? '');
  }
  clearFilters(): void {
    this.filterCourseId.set(null);
    this.filterStatus.set(null);
    this.filterPersonnelType.set(null);
    this.filterSearch.set('');
  }

  // ── Display helpers ──
  toUSD(omr: number | null | undefined): string {
    const v = (omr ?? 0) * this.exchangeRate();
    return v.toLocaleString('en-US', { maximumFractionDigits: 0 });
  }

  formatOMR(value: number | null | undefined): string {
    return (value ?? 0).toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 });
  }

  shortDate(iso: string | null | undefined): string {
    return (iso ?? '').substring(0, 10) || '—';
  }

  /** Inclusive day count between two ISO dates; 0 when either is missing or order is reversed. */
  private daysBetween(fromIso: string | null | undefined, toIso: string | null | undefined): number {
    if (!fromIso || !toIso) return 0;
    const from = new Date(fromIso);
    const to = new Date(toIso);
    if (Number.isNaN(from.getTime()) || Number.isNaN(to.getTime()) || to < from) return 0;
    return Math.floor((to.getTime() - from.getTime()) / 86_400_000) + 1;
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

  personnelBadgeText(p: PersonnelType | undefined): string {
    if (p === undefined) return '';
    return this.personnelOptions.find(o => o.value === p)?.text ?? '';
  }

  courseLabel(courseId: string | null | undefined): string {
    if (!courseId) return '—';
    const c = this.approvedCourses().find(x => x.id === courseId);
    return c?.courseNameAr ?? '—';
  }

  // ── Dialog flow ──
  async onAdd(): Promise<void> {
    if (!this.canCreate()) return;
    this.resetForm();
    this.dialog.set({ visible: true, isEdit: false, id: null, current: null });
    this.dialogCourse.set(null);
    // If a course filter is set, prime the dialog with it.
    const courseId = this.filterCourseId();
    if (courseId) await this.loadCourseContext(courseId);
  }

  async onEdit(row: TravelAllowancePaymentDto): Promise<void> {
    if (!this.canUpdate() || row.status !== PaymentStatus.Draft) return;
    this.dialog.set({ visible: true, isEdit: true, id: row.id, current: row });
    this.dialogError.set(null);
    this.fNominationId.set(row.nominationId ?? null);
    this.fTicketAmountOMR.set(row.ticketAmountOMR ?? 0);
    this.fTravelAllowanceOMR.set(row.travelAllowanceOMR ?? 0);
    this.fClothingAllowanceOMR.set(row.clothingAllowanceOMR ?? 0);
    this.fInsuranceOMR.set(row.insuranceOMR ?? 0);
    this.fVisaFeesOMR.set(row.visaFeesOMR ?? 0);
    this.fNotes.set(row.notes ?? '');
    if (row.casualCourseId) await this.loadCourseContext(row.casualCourseId);
    // Days are now known — back-derive the daily rate from the stored total so the
    // rate input shows a sensible value the user can adjust.
    this.deriveRateFromTotal(row.travelAllowanceOMR ?? 0);
  }

  async onView(row: TravelAllowancePaymentDto): Promise<void> {
    this.dialog.set({ visible: true, isEdit: true, id: row.id, current: row });
    this.fNominationId.set(row.nominationId ?? null);
    this.fTicketAmountOMR.set(row.ticketAmountOMR ?? 0);
    this.fTravelAllowanceOMR.set(row.travelAllowanceOMR ?? 0);
    this.fClothingAllowanceOMR.set(row.clothingAllowanceOMR ?? 0);
    this.fInsuranceOMR.set(row.insuranceOMR ?? 0);
    this.fVisaFeesOMR.set(row.visaFeesOMR ?? 0);
    this.fNotes.set(row.notes ?? '');
    if (row.casualCourseId) await this.loadCourseContext(row.casualCourseId);
    this.deriveRateFromTotal(row.travelAllowanceOMR ?? 0);
  }

  closeDialog(): void {
    this.dialog.set({ visible: false, isEdit: false, id: null, current: null });
    this.dialogError.set(null);
    this.dialogCourse.set(null);
    this.dialogTravelInstruction.set(null);
    this.dialogSelectedQuote.set(null);
  }

  private async loadCourseContext(courseId: string): Promise<void> {
    let detail: CasualCourseDetailDto | null = null;
    try {
      detail = await firstValueFrom(this.courseService.getDetail(courseId));
      this.dialogCourse.set(detail);
    } catch {
      this.dialogCourse.set(null);
      this.dialogTravelInstruction.set(null);
      this.dialogSelectedQuote.set(null);
      return;
    }
    // Pull TI + selected quote in parallel — both are read-only enrichment for the
    // context panel, so a failure on either side is non-fatal.
    const [ti, quote] = await Promise.all([
      firstValueFrom(this.travelInstructionService.getByParent(courseId, '')).catch(() => null),
      detail.selectedPriceQuoteId
        ? firstValueFrom(this.quoteService.get(detail.selectedPriceQuoteId)).catch(() => null)
        : Promise.resolve(null),
    ]);
    this.dialogTravelInstruction.set(ti ?? null);
    this.dialogSelectedQuote.set(quote ?? null);
  }

  /** Derives the daily rate from a stored/server total ÷ effective travel days.
   *  Used on Edit/View load and after server-side autofill on Save, so the rate
   *  input shows a value consistent with the displayed total. */
  private deriveRateFromTotal(total: number): void {
    const days = this.contextEffectiveTravelDays();
    if (days > 0 && total > 0) {
      this.fDailyAllowanceRateOMR.set(Math.round((total / days) * 1000) / 1000);
    } else {
      this.fDailyAllowanceRateOMR.set(0);
    }
  }

  async onDialogCourseChange(courseId: string | null): Promise<void> {
    if (!courseId) {
      this.dialogCourse.set(null);
      this.dialogTravelInstruction.set(null);
      this.dialogSelectedQuote.set(null);
      this.fNominationId.set(null);
      this.recomputeTravelAllowance();   // days=0 → total=0
      return;
    }
    this.fNominationId.set(null);
    await this.loadCourseContext(courseId);
    // TI may have loaded with a new effective-days value; re-derive the total
    // from the current rate (zero on first open) so the breakdown stays honest.
    this.recomputeTravelAllowance();
  }

  // ── Save / Confirm / Delete ──
  private buildDto(): CreateUpdateTravelAllowancePaymentDto {
    const courseId = this.dialog().current?.casualCourseId ?? this.dialogCourse()?.id ?? null;
    return {
      casualCourseId: courseId,
      sessionId: null,
      nominationId: this.fNominationId() ?? '',
      ticketAmountOMR: this.fTicketAmountOMR(),
      travelAllowanceOMR: this.fTravelAllowanceOMR(),
      clothingAllowanceOMR: this.fClothingAllowanceOMR(),
      insuranceOMR: this.fInsuranceOMR(),
      visaFeesOMR: this.fVisaFeesOMR(),
      notes: this.fNotes() || null,
    };
  }

  async onSaveDraft(): Promise<void> {
    if (!this.fNominationId()) {
      this.dialogError.set(this.l.t('::Training.Payments.TravelAllowance.Dialog.PickNomineeFirst'));
      return;
    }
    this.dialogError.set(null);
    this.dialogSaving.set(true);
    try {
      const dto = this.buildDto();
      let saved: TravelAllowancePaymentDto;
      if (this.dialog().isEdit && this.dialog().id) {
        saved = await firstValueFrom(this.paymentService.update(this.dialog().id!, dto));
      } else {
        saved = await firstValueFrom(this.paymentService.create(dto));
      }
      // Server may pre-fill defaults — reflect them back into the form.
      this.fTicketAmountOMR.set(saved.ticketAmountOMR ?? 0);
      this.fTravelAllowanceOMR.set(saved.travelAllowanceOMR ?? 0);
      this.fClothingAllowanceOMR.set(saved.clothingAllowanceOMR ?? 0);
      this.fInsuranceOMR.set(saved.insuranceOMR ?? 0);
      this.fVisaFeesOMR.set(saved.visaFeesOMR ?? 0);
      // Back-derive the daily rate so it stays consistent with whatever total the
      // server just returned (it may have applied rank-breakdown defaults).
      this.deriveRateFromTotal(saved.travelAllowanceOMR ?? 0);
      this.dialog.set({ visible: true, isEdit: true, id: saved.id, current: saved });
      await this.loadRows();
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
    if (this.dialog().current?.status !== PaymentStatus.Draft) {
      this.dialogError.set(this.l.t('::Training.Payments.TravelAllowance.Dialog.OnlyDraftEditable'));
      return;
    }
    if (!confirm(this.l.t('::Training.Payments.TravelAllowance.Dialog.ConfirmPrompt'))) {
      return;
    }
    this.dialogError.set(null);
    this.dialogSaving.set(true);
    try {
      // Persist any pending edits first to keep amounts honest.
      const dto = this.buildDto();
      await firstValueFrom(this.paymentService.update(id, dto));
      await firstValueFrom(this.paymentService.confirm(id));
      await this.loadRows();
      // Confirm is terminal — close the dialog so the user lands back on the
      // grid where the row now reflects the Confirmed state.
      this.closeDialog();
    } catch (err) {
      this.dialogError.set(this.extractError(err));
    } finally {
      this.dialogSaving.set(false);
    }
  }

  async onDelete(row: TravelAllowancePaymentDto): Promise<void> {
    if (!this.canDelete() || row.status !== PaymentStatus.Draft) return;
    const prompt = `${this.l.t('::Training.Payments.TravelAllowance.Dialog.DeletePromptPrefix')} "${row.employeeNameAr}"؟`;
    if (!confirm(prompt)) return;
    await firstValueFrom(this.paymentService.delete(row.id));
    await this.loadRows();
  }

  // ── Form field setters ──
  /** In Create mode, picking a nominee fires a server preview that pre-fills the
   *  five amount fields + the per-day rate from the casual course's review-page
   *  breakdown for the nominee's rank, so Finance can review/adjust before Save. */
  async updateNominationId(v: string | null): Promise<void> {
    this.fNominationId.set(v);
    if (!v || this.dialog().isEdit) return;
    const courseId = this.dialogCourse()?.id;
    if (!courseId) return;
    try {
      const d = await firstValueFrom(this.paymentService.getDefaults(courseId, v));
      this.fTicketAmountOMR.set(d.ticketAmountOMR ?? 0);
      this.fClothingAllowanceOMR.set(d.clothingAllowanceOMR ?? 0);
      this.fInsuranceOMR.set(d.insuranceOMR ?? 0);
      this.fVisaFeesOMR.set(d.visaFeesOMR ?? 0);
      this.fDailyAllowanceRateOMR.set(d.dailyAllowanceRateOMR ?? 0);
      // Use the server's preview total verbatim — it's days × rate computed with
      // the same rounding rules as a future Save, so the form mirrors what the
      // backend will store.
      this.fTravelAllowanceOMR.set(d.travelAllowanceOMR ?? 0);
    } catch {
      // Preview is non-fatal: TI may not be Issued yet, nominee may be invalid, etc.
      // Leave whatever the user already had; the Save Draft path still validates strictly.
    }
  }
  updateTicket(v: number | null): void { this.fTicketAmountOMR.set(v ?? 0); }
  /** User changed the per-day rate → re-derive the travel-allowance total
   *  (sent to the backend as TravelAllowanceOMR). */
  updateDailyAllowanceRate(v: number | null): void {
    this.fDailyAllowanceRateOMR.set(v ?? 0);
    this.recomputeTravelAllowance();
  }
  updateClothing(v: number | null): void { this.fClothingAllowanceOMR.set(v ?? 0); }
  updateInsurance(v: number | null): void { this.fInsuranceOMR.set(v ?? 0); }
  updateVisaFees(v: number | null): void { this.fVisaFeesOMR.set(v ?? 0); }
  updateNotes(v: string): void { this.fNotes.set(v ?? ''); }

  /** TravelAllowance total = effectiveTravelDays × dailyRate, rounded to 3 dp
   *  to match the OMR currency precision used everywhere else in the form. */
  private recomputeTravelAllowance(): void {
    const days = this.contextEffectiveTravelDays();
    const rate = this.fDailyAllowanceRateOMR();
    const total = Math.round(days * rate * 1000) / 1000;
    this.fTravelAllowanceOMR.set(total);
  }

  private resetForm(): void {
    this.fNominationId.set(null);
    this.fTicketAmountOMR.set(0);
    this.fDailyAllowanceRateOMR.set(0);
    this.fTravelAllowanceOMR.set(0);
    this.fClothingAllowanceOMR.set(0);
    this.fInsuranceOMR.set(0);
    this.fVisaFeesOMR.set(0);
    this.fNotes.set('');
    this.dialogError.set(null);
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
