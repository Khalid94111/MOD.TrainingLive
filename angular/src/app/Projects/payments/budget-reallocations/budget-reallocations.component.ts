import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import { firstValueFrom } from 'rxjs';
import {
  DxDataGridModule,
  DxPopupModule,
  DxSelectBoxModule,
  DxTextBoxModule,
  DxTextAreaModule,
  DxButtonModule,
  DxDateBoxModule,
} from 'devextreme-angular';
import type { ToolbarItem } from 'devextreme/ui/popup';

import { BudgetReallocationService } from 'src/app/proxy/training/payments/budget-reallocation.service';
import type {
  BudgetReallocationDto,
  BudgetReallocationGetListInput,
  MarkReallocationApprovedDto,
} from 'src/app/proxy/training/payments/dtos/models';
import { ReallocationStatus } from 'src/app/proxy/training/enums/reallocation-status.enum';
import { FundingScenario } from 'src/app/proxy/training/enums/funding-scenario.enum';

import { CasualCourseService } from 'src/app/proxy/training/casual-courses';
import type { CasualCourseDto } from 'src/app/proxy/training/casual-courses/dtos/models';
import { CasualCourseStatus } from 'src/app/proxy/training/enums/casual-course-status.enum';

import { FinancialItemService } from 'src/app/proxy/training/finance';
import type { FinancialItemDto } from 'src/app/proxy/training/finance/dtos/models';
import { ExchangeRateService } from '../../shared/services/finance-proxy.service';

import { TrainingLocalizationHelper } from '../../shared';

interface ApprovalDialogState {
  visible: boolean;
  row: BudgetReallocationDto | null;
}

interface ToastState {
  visible: boolean;
  text: string;
  kind: 'success' | 'error';
}

@Component({
  standalone: true,
  selector: 'app-budget-reallocations',
  templateUrl: './budget-reallocations.component.html',
  styleUrls: [
    './budget-reallocations.component.scss',
    '../../shared/gtms-design.scss',
  ],
  imports: [
    CommonModule,
    LocalizationPipe,
    DxDataGridModule,
    DxPopupModule,
    DxSelectBoxModule,
    DxTextBoxModule,
    DxTextAreaModule,
    DxButtonModule,
    DxDateBoxModule,
  ],
})
export class BudgetReallocationsComponent implements OnInit {
  private service = inject(BudgetReallocationService);
  private courseService = inject(CasualCourseService);
  private financialItemService = inject(FinancialItemService);
  private exchangeService = inject(ExchangeRateService);
  private permissions = inject(PermissionService);
  l = inject(TrainingLocalizationHelper);

  ReallocationStatus = ReallocationStatus;
  FundingScenario = FundingScenario;

  // ── State ──
  rows = signal<BudgetReallocationDto[]>([]);
  allCourses = signal<CasualCourseDto[]>([]);
  financialItems = signal<FinancialItemDto[]>([]);
  loading = signal(true);
  exchangeRate = signal<number>(2.6);

  // ── Filters ──
  filterStatus = signal<ReallocationStatus | null>(null);
  filterCourseId = signal<string | null>(null);
  filterFinancialItemId = signal<string | null>(null);
  filterCreatedFrom = signal<string | null>(null);
  filterCreatedTo = signal<string | null>(null);
  filterVoteCode = signal<string>('');

  // ── Permissions ──
  canMarkApproved = computed(() => this.permissions.getGrantedPolicy('TrainingPayments.Reallocations.MarkApproved'));

  // ── Approval dialog ──
  dialog = signal<ApprovalDialogState>({ visible: false, row: null });
  dialogToolbarItems: ToolbarItem[] | undefined;
  dialogError = signal<string | null>(null);
  dialogSaving = signal(false);

  fApprovalNote = signal<string>('');

  toast = signal<ToastState>({ visible: false, text: '', kind: 'success' });

  statusOptions: { value: ReallocationStatus; text: string }[] = [];

  // ── Derived rows / stats ──
  filteredRows = computed(() => {
    const status = this.filterStatus();
    const courseId = this.filterCourseId();
    const itemId = this.filterFinancialItemId();
    const from = this.filterCreatedFrom();
    const to = this.filterCreatedTo();
    const voteQ = this.filterVoteCode().trim().toLowerCase();

    return this.rows().filter(r => {
      if (status !== null && r.status !== status) return false;
      if (courseId && r.casualCourseId !== courseId) return false;
      if (itemId && r.toFinancialItemId !== itemId) return false;
      if (from && (r.creationTime ?? '') < from) return false;
      if (to && (r.creationTime ?? '') > to) return false;
      if (voteQ) {
        const code = (r.fundingSourceVoteCode ?? '').toLowerCase();
        const name = (r.fundingSourceName ?? '').toLowerCase();
        if (!code.includes(voteQ) && !name.includes(voteQ)) return false;
      }
      return true;
    });
  });

  pendingRows = computed(() => this.rows().filter(r => r.status === ReallocationStatus.Pending));
  approvedRows = computed(() => this.rows().filter(r => r.status === ReallocationStatus.Approved));

  pendingCount = computed(() => this.pendingRows().length);
  pendingTotalOMR = computed(() => this.pendingRows().reduce((s, r) => s + (r.amountOMR ?? 0), 0));

  approvedCount = computed(() => this.approvedRows().length);
  approvedTotalOMR = computed(() => this.approvedRows().reduce((s, r) => s + (r.amountOMR ?? 0), 0));

  thisMonthRows = computed(() => {
    const now = new Date();
    const yearMonth = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
    return this.rows().filter(r => (r.creationTime ?? '').startsWith(yearMonth));
  });
  thisMonthCount = computed(() => this.thisMonthRows().length);
  thisMonthTotalOMR = computed(() => this.thisMonthRows().reduce((s, r) => s + (r.amountOMR ?? 0), 0));

  lastApproval = computed<BudgetReallocationDto | null>(() => {
    const approved = this.approvedRows();
    if (approved.length === 0) return null;
    return [...approved].sort((a, b) => (b.approvedAt ?? '').localeCompare(a.approvedAt ?? ''))[0];
  });

  lastApprovalRelative = computed<string>(() => {
    const last = this.lastApproval();
    if (!last?.approvedAt) return '—';
    return this.relativeTime(last.approvedAt);
  });

  // ── Lifecycle ──
  async ngOnInit(): Promise<void> {
    this.statusOptions = [
      { value: ReallocationStatus.Pending,  text: this.l.t('::Training.ReallocationStatus.Pending') },
      { value: ReallocationStatus.Approved, text: this.l.t('::Training.ReallocationStatus.Approved') },
    ];

    this.dialogToolbarItems = [
      { widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Cancel'), onClick: () => this.closeDialog() } },
      { widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: {
          text: this.l.t('::Training.Payments.Reallocation.Dialog.ConfirmAction'),
          type: 'success',
          onClick: () => this.onConfirmApproval(),
        },
      },
    ];

    await Promise.all([
      this.loadRows(),
      this.loadCourses(),
      this.loadFinancialItems(),
      this.loadExchangeRate(),
    ]);
  }

  private async loadRows(): Promise<void> {
    this.loading.set(true);
    try {
      const input: BudgetReallocationGetListInput = { maxResultCount: 1000 };
      const result = await firstValueFrom(this.service.getList(input));
      this.rows.set(result.items ?? []);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadCourses(): Promise<void> {
    try {
      const result = await firstValueFrom(
        this.courseService.getList({
          status: [CasualCourseStatus.THApproved],
          maxResultCount: 500,
        }),
      );
      this.allCourses.set(result.items ?? []);
    } catch {
      this.allCourses.set([]);
    }
  }

  private async loadFinancialItems(): Promise<void> {
    try {
      const result = await firstValueFrom(
        this.financialItemService.getList({ isActive: true, maxResultCount: 500 }),
      );
      this.financialItems.set(result.items ?? []);
    } catch {
      this.financialItems.set([]);
    }
  }

  private async loadExchangeRate(): Promise<void> {
    try {
      const r = await this.exchangeService.getActive();
      if (r?.rate && r.rate > 0) this.exchangeRate.set(r.rate);
    } catch { /* noop */ }
  }

  // ── Filter handlers ──
  onStatusFilterChange(v: ReallocationStatus | null): void { this.filterStatus.set(v); }
  onCourseFilterChange(v: string | null): void { this.filterCourseId.set(v); }
  onItemFilterChange(v: string | null): void { this.filterFinancialItemId.set(v); }
  onCreatedFromChange(v: string | null | Date): void { this.filterCreatedFrom.set(this.toIsoOrNull(v)); }
  onCreatedToChange(v: string | null | Date): void { this.filterCreatedTo.set(this.toIsoOrNull(v)); }
  onVoteCodeInput(v: string): void { this.filterVoteCode.set(v ?? ''); }

  clearFilters(): void {
    this.filterStatus.set(null);
    this.filterCourseId.set(null);
    this.filterFinancialItemId.set(null);
    this.filterCreatedFrom.set(null);
    this.filterCreatedTo.set(null);
    this.filterVoteCode.set('');
  }

  // ── Display helpers ──
  toUSD(omr: number | null | undefined): string {
    return ((omr ?? 0) * this.exchangeRate()).toLocaleString('en-US', { maximumFractionDigits: 0 });
  }

  formatOMR(value: number | null | undefined): string {
    return (value ?? 0).toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 });
  }

  statusBadgeText(s: ReallocationStatus | undefined): string {
    switch (s) {
      case ReallocationStatus.Approved: return this.l.t('::Training.Payments.Reallocation.StatusApproved');
      case ReallocationStatus.Pending:  return this.l.t('::Training.Payments.Reallocation.StatusPending');
      default: return '';
    }
  }

  statusBadgeCss(s: ReallocationStatus | undefined): string {
    switch (s) {
      case ReallocationStatus.Approved: return 'status-badge status-approved';
      default: return 'status-badge status-pending';
    }
  }

  scenarioPillCss(scenario: FundingScenario | null | undefined): string {
    switch (scenario) {
      case FundingScenario.FundingSourceCoversAll:    return 'scenario-pill scenario-1';
      case FundingScenario.FundingSourceCoversCourse: return 'scenario-pill scenario-2';
      case FundingScenario.FinancialItemsCoverAll:    return 'scenario-pill scenario-3';
      default: return 'scenario-pill';
    }
  }

  scenarioLabel(scenario: FundingScenario | null | undefined): string {
    switch (scenario) {
      case FundingScenario.FundingSourceCoversAll:    return this.l.t('::Training.Payments.Reallocation.Scenario1');
      case FundingScenario.FundingSourceCoversCourse: return this.l.t('::Training.Payments.Reallocation.Scenario2');
      case FundingScenario.FinancialItemsCoverAll:    return this.l.t('::Training.Payments.Reallocation.Scenario3');
      default: return '—';
    }
  }

  /** Coarse relative time — localized. The Arabic prefix "قبل" comes from the JSON;
   *  English uses "minutes ago"-style suffixes with the prefix kept empty. */
  private relativeTime(iso: string): string {
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) return iso;
    const diffMs = Date.now() - date.getTime();
    const minutes = Math.floor(diffMs / 60_000);
    if (minutes < 1) return this.l.t('::Training.Payments.Reallocation.RelativeNow');
    const prefix = this.l.t('::Training.Payments.Reallocation.RelativeMinutesPrefix');
    const compose = (n: number, unitKey: string): string => {
      const unit = this.l.t(unitKey);
      return prefix ? `${prefix} ${n} ${unit}` : `${n} ${unit}`;
    };
    if (minutes < 60) return compose(minutes, '::Training.Payments.Reallocation.RelativeMinutesUnit');
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return compose(hours, '::Training.Payments.Reallocation.RelativeHoursUnit');
    const days = Math.floor(hours / 24);
    if (days < 30) return compose(days, '::Training.Payments.Reallocation.RelativeDaysUnit');
    const months = Math.floor(days / 30);
    if (months < 12) return compose(months, '::Training.Payments.Reallocation.RelativeMonthsUnit');
    const years = Math.floor(months / 12);
    return compose(years, '::Training.Payments.Reallocation.RelativeYearsUnit');
  }

  private toIsoOrNull(v: string | null | Date | undefined): string | null {
    if (!v) return null;
    if (v instanceof Date) return v.toISOString().substring(0, 10);
    return v.substring(0, 10);
  }

  // ── Approval flow ──
  onApprove(row: BudgetReallocationDto): void {
    if (!this.canMarkApproved() || row.status !== ReallocationStatus.Pending) return;
    this.dialog.set({ visible: true, row });
    this.fApprovalNote.set('');
    this.dialogError.set(null);
  }

  closeDialog(): void {
    this.dialog.set({ visible: false, row: null });
    this.fApprovalNote.set('');
    this.dialogError.set(null);
  }

  async onConfirmApproval(): Promise<void> {
    const row = this.dialog().row;
    if (!row) return;
    this.dialogSaving.set(true);
    this.dialogError.set(null);
    try {
      const dto: MarkReallocationApprovedDto = {
        approvalNote: this.fApprovalNote() || null,
      };
      await firstValueFrom(this.service.markApproved(row.id, dto));
      await this.loadRows();
      this.closeDialog();
      this.showToast(this.l.t('::Training.Payments.Reallocation.ApprovalSuccess'), 'success');
    } catch (err) {
      this.dialogError.set(this.extractError(err));
    } finally {
      this.dialogSaving.set(false);
    }
  }

  // ── Form setters ──
  updateApprovalNote(v: string): void { this.fApprovalNote.set(v ?? ''); }

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
