import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { TrainingExpenseRecoveryService } from 'src/app/proxy/training/payments/training-expense-recovery.service';
import type { TrainingExpenseRecoveryDto, TrainingExpenseRecoveryItemDto } from 'src/app/proxy/training/payments/dtos/models';
import { TrainingExpenseRecoveryStatus } from 'src/app/proxy/training/enums/training-expense-recovery-status.enum';
import { TrainingLocalizationHelper } from '../../shared';

type DialogMode = 'review' | 'settle-item' | 'settle-all';

interface DialogState {
  visible: boolean;
  mode: DialogMode;
  record: TrainingExpenseRecoveryDto | null;
  item: TrainingExpenseRecoveryItemDto | null;
}

@Component({
  standalone: true,
  selector: 'app-training-expense-recoveries',
  templateUrl: './budget-reallocations.component.html',
  styleUrls: ['./budget-reallocations.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, LocalizationPipe],
})
export class TrainingExpenseRecoveriesComponent implements OnInit {
  private service = inject(TrainingExpenseRecoveryService);
  private permissions = inject(PermissionService);
  private router = inject(Router);
  private l = inject(TrainingLocalizationHelper);

  readonly Status = TrainingExpenseRecoveryStatus;

  records = signal<TrainingExpenseRecoveryDto[]>([]);
  loading = signal(true);
  loadError = signal<string | null>(null);
  search = signal('');
  expandedRecords = signal<Set<string>>(new Set());

  dialog = signal<DialogState>({ visible: false, mode: 'review', record: null, item: null });
  reviewNote = signal('');
  settlementReference = signal('');
  settlementNote = signal('');
  dialogSaving = signal(false);
  dialogError = signal<string | null>(null);
  toast = signal<{ text: string; kind: 'success' | 'error' } | null>(null);

  canReview = computed(() =>
    this.permissions.getGrantedPolicy('TrainingPayments.Reallocations.MarkApproved'));
  canSettle = computed(() =>
    this.permissions.getGrantedPolicy('TrainingPayments.Reallocations.MarkSettled'));

  filteredRecords = computed(() => {
    const query = this.search().trim().toLowerCase();
    if (!query) return this.records();
    return this.records().filter(record =>
      (record.casualCourseNameAr ?? '').toLowerCase().includes(query)
      || (record.travelRequestId ?? '').toLowerCase().includes(query)
      || (record.items ?? []).some(item =>
        (item.financialItemNameAr ?? '').toLowerCase().includes(query)
        || (item.fundingSourceVoteCode ?? '').toLowerCase().includes(query)));
  });

  pendingRecords = computed(() =>
    this.records().filter(x => x.status === TrainingExpenseRecoveryStatus.PendingReview));
  reviewedRecords = computed(() =>
    this.records().filter(x => x.status === TrainingExpenseRecoveryStatus.Reviewed
      || x.status === TrainingExpenseRecoveryStatus.PartiallySettled));
  pendingTotal = computed(() => this.sum(this.pendingRecords()));
  reviewedTotal = computed(() => this.reviewedRecords()
    .reduce((total, item) => total + (item.remainingAmountOMR ?? 0), 0));
  settledTotal = computed(() => this.records()
    .reduce((total, item) => total + (item.settledAmountOMR ?? 0), 0));
  settledItemsCount = computed(() => this.records()
    .reduce((total, record) => total + (record.items ?? []).filter(item => item.isSettled).length, 0));

  ngOnInit(): void {
    void this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      // Backfills requests completed before this register was introduced. Future requests arrive
      // through Travel's completion event, so this remains an idempotent recovery path.
      try { await firstValueFrom(this.service.refresh()); } catch { /* Keep existing records readable. */ }
      const result = await firstValueFrom(this.service.getList({ maxResultCount: 1000 }));
      this.records.set(result.items ?? []);
    } catch (error) {
      this.loadError.set(this.extractError(error));
    } finally {
      this.loading.set(false);
    }
  }

  onSearchInput(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value ?? '');
  }

  clearSearch(): void {
    this.search.set('');
  }

  isExpanded(id: string | undefined): boolean {
    return !!id && this.expandedRecords().has(id);
  }

  toggleRecord(id: string | undefined): void {
    if (!id) return;
    const expanded = new Set(this.expandedRecords());
    if (expanded.has(id)) expanded.delete(id);
    else expanded.add(id);
    this.expandedRecords.set(expanded);
  }

  openTravelRequest(record: TrainingExpenseRecoveryDto): void {
    if (record.travelRequestId) void this.router.navigate(['/travel/requests', record.travelRequestId]);
  }

  openCourse(record: TrainingExpenseRecoveryDto): void {
    if (record.casualCourseId) {
      void this.router.navigate(['/training/casual-courses', record.casualCourseId], {
        queryParams: { stage: 'payments' },
      });
    }
  }

  openDialog(
    record: TrainingExpenseRecoveryDto,
    mode: DialogMode,
    item: TrainingExpenseRecoveryItemDto | null = null,
  ): void {
    this.dialog.set({ visible: true, mode, record, item });
    this.reviewNote.set('');
    this.settlementReference.set('');
    this.settlementNote.set('');
    this.dialogError.set(null);
  }

  closeDialog(): void {
    if (this.dialogSaving()) return;
    this.dialog.set({ visible: false, mode: 'review', record: null, item: null });
    this.dialogError.set(null);
  }

  async submitDialog(): Promise<void> {
    const state = this.dialog();
    if (!state.record?.id) return;
    if (state.mode !== 'review' && !this.settlementReference().trim()) {
      this.dialogError.set(this.l.t('::Training.Payments.ExpenseRecovery.ReferenceRequired'));
      return;
    }

    this.dialogSaving.set(true);
    this.dialogError.set(null);
    try {
      if (state.mode === 'review') {
        await firstValueFrom(this.service.markReviewed(state.record.id, {
          reviewNote: this.reviewNote().trim() || null,
        }));
      } else if (state.mode === 'settle-item' && state.item?.id) {
        await firstValueFrom(this.service.markItemSettled(state.record.id, state.item.id, {
          settlementReference: this.settlementReference().trim(),
          settlementNote: this.settlementNote().trim() || null,
        }));
      } else if (state.mode === 'settle-all') {
        await firstValueFrom(this.service.markAllSettled(state.record.id, {
          settlementReference: this.settlementReference().trim(),
          settlementNote: this.settlementNote().trim() || null,
        }));
      }
      this.dialogSaving.set(false);
      this.closeDialog();
      await this.reload();
      this.showToast(this.l.t(state.mode === 'review'
        ? '::Training.Payments.ExpenseRecovery.ReviewSuccess'
        : '::Training.Payments.ExpenseRecovery.SettleSuccess'), 'success');
    } catch (error) {
      this.dialogError.set(this.extractError(error));
      this.dialogSaving.set(false);
    }
  }

  statusKey(status: TrainingExpenseRecoveryStatus | undefined): string {
    switch (status) {
      case TrainingExpenseRecoveryStatus.Reviewed:
        return '::Training.Payments.ExpenseRecovery.StatusReviewed';
      case TrainingExpenseRecoveryStatus.PartiallySettled:
        return '::Training.Payments.ExpenseRecovery.StatusPartiallySettled';
      case TrainingExpenseRecoveryStatus.Settled:
        return '::Training.Payments.ExpenseRecovery.StatusSettled';
      default:
        return '::Training.Payments.ExpenseRecovery.StatusPending';
    }
  }

  statusCss(status: TrainingExpenseRecoveryStatus | undefined): string {
    switch (status) {
      case TrainingExpenseRecoveryStatus.Reviewed: return 'status-pill reviewed';
      case TrainingExpenseRecoveryStatus.PartiallySettled: return 'status-pill partial';
      case TrainingExpenseRecoveryStatus.Settled: return 'status-pill settled';
      default: return 'status-pill pending';
    }
  }

  statusIcon(status: TrainingExpenseRecoveryStatus | undefined): string {
    switch (status) {
      case TrainingExpenseRecoveryStatus.Reviewed: return 'bi bi-clipboard2-check';
      case TrainingExpenseRecoveryStatus.PartiallySettled: return 'bi bi-pie-chart';
      case TrainingExpenseRecoveryStatus.Settled: return 'bi bi-check2-circle';
      default: return 'bi bi-exclamation-circle';
    }
  }

  expenseTypeKey(typeCode: string | undefined): string {
    switch ((typeCode ?? '').toLowerCase()) {
      case 'ticket': return '::Training.Payments.ExpenseRecovery.Type.Ticket';
      case 'visa': return '::Training.Payments.ExpenseRecovery.Type.Visa';
      case 'healthinsurance': return '::Training.Payments.ExpenseRecovery.Type.HealthInsurance';
      case 'dailyallowance': return '::Training.Payments.ExpenseRecovery.Type.DailyAllowance';
      case 'clothingallowance': return '::Training.Payments.ExpenseRecovery.Type.ClothingAllowance';
      default: return '::Training.Payments.ExpenseRecovery.Type.Other';
    }
  }

  settlementDialogTitleKey(): string {
    switch (this.dialog().mode) {
      case 'review': return '::Training.Payments.ExpenseRecovery.ReviewDialogTitle';
      case 'settle-item': return '::Training.Payments.ExpenseRecovery.SettleItemDialogTitle';
      default: return '::Training.Payments.ExpenseRecovery.SettleAllDialogTitle';
    }
  }

  dialogAmount(): number {
    const state = this.dialog();
    return state.mode === 'settle-item'
      ? state.item?.remainingAmountOMR ?? state.item?.amountOMR ?? 0
      : state.record?.remainingAmountOMR ?? state.record?.totalAmountOMR ?? 0;
  }

  formatOMR(value: number | null | undefined): string {
    return (value ?? 0).toLocaleString('en-US', {
      minimumFractionDigits: 3,
      maximumFractionDigits: 3,
    });
  }

  shortDate(value: string | null | undefined): string {
    return (value ?? '').substring(0, 10) || '—';
  }

  updateReviewNote(event: Event): void {
    this.reviewNote.set((event.target as HTMLTextAreaElement).value ?? '');
  }

  updateSettlementReference(event: Event): void {
    this.settlementReference.set((event.target as HTMLInputElement).value ?? '');
  }

  updateSettlementNote(event: Event): void {
    this.settlementNote.set((event.target as HTMLTextAreaElement).value ?? '');
  }

  private sum(records: TrainingExpenseRecoveryDto[]): number {
    return records.reduce((total, item) => total + (item.totalAmountOMR ?? 0), 0);
  }

  private showToast(text: string, kind: 'success' | 'error'): void {
    this.toast.set({ text, kind });
    window.setTimeout(() => this.toast.set(null), 4000);
  }

  private extractError(error: unknown): string {
    if (error && typeof error === 'object') {
      const apiError = error as { error?: { error?: { message?: string } }; message?: string };
      return apiError.error?.error?.message
        ?? apiError.message
        ?? this.l.t('::Training.Payments.Errors.Generic');
    }
    return this.l.t('::Training.Payments.Errors.Generic');
  }
}
