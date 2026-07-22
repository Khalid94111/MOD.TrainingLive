import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { DxSelectBoxModule } from 'devextreme-angular';

import { FundingScenario } from 'src/app/proxy/training/enums/funding-scenario.enum';
import { PaymentStatus } from 'src/app/proxy/training/enums/payment-status.enum';
import { CoursePaymentService } from 'src/app/proxy/training/payments/course-payment.service';
import type {
  CoursePaymentDto,
  CoursePaymentGetListInput,
} from 'src/app/proxy/training/payments/dtos/models';

import { TrainingLocalizationHelper } from '../../shared';

interface FilterOption {
  id: string;
  label: string;
}

interface ToastState {
  visible: boolean;
  text: string;
}

/**
 * Central read-only register for course invoices.
 * Payment mutations belong to the source workflow (annual-plan session or
 * casual course); this page only searches, downloads and opens that workflow.
 */
@Component({
  standalone: true,
  selector: 'app-course-payments',
  templateUrl: './course-payments.component.html',
  styleUrls: [
    './course-payments.component.scss',
    '../../shared/gtms-design.scss',
  ],
  imports: [CommonModule, LocalizationPipe, DxSelectBoxModule],
})
export class CoursePaymentsComponent implements OnInit {
  private readonly paymentService = inject(CoursePaymentService);
  private readonly permissions = inject(PermissionService);
  private readonly router = inject(Router);
  readonly l = inject(TrainingLocalizationHelper);

  readonly PaymentStatus = PaymentStatus;

  readonly rows = signal<CoursePaymentDto[]>([]);
  readonly loading = signal(true);
  readonly toast = signal<ToastState>({ visible: false, text: '' });

  readonly filterCourseId = signal<string | null>(null);
  readonly filterProviderId = signal<string | null>(null);
  readonly filterSource = signal<'all' | 'session' | 'casual'>('all');
  readonly searchTerm = signal('');

  readonly canDownload = computed(() =>
    this.permissions.getGrantedPolicy('TrainingPayments.CoursePayments.DownloadInvoice'));

  readonly totalPayments = computed(() => this.rows().length);
  readonly draftPayments = computed(() =>
    this.rows().filter(row => row.status === PaymentStatus.Draft).length);
  readonly confirmedPayments = computed(() =>
    this.rows().filter(row => row.status === PaymentStatus.Confirmed).length);
  readonly attachedInvoices = computed(() =>
    this.rows().filter(row => row.hasInvoice).length);
  readonly confirmedTotalOMR = computed(() => this.rows()
    .filter(row => row.status === PaymentStatus.Confirmed)
    .reduce((sum, row) => sum + (row.invoiceAmountOMR ?? 0), 0));

  readonly hasActiveFilters = computed(() =>
    this.filterSource() !== 'all'
    || !!this.filterCourseId()
    || !!this.filterProviderId()
    || !!this.searchTerm().trim());

  readonly courseFilterOptions = computed<FilterOption[]>(() =>
    this.distinctOptions(row => row.casualCourseId ?? row.sessionId, row => row.courseNameAr));

  readonly providerFilterOptions = computed<FilterOption[]>(() =>
    this.distinctOptions(row => row.trainingProviderId, row => row.trainingProviderName));

  readonly filteredRows = computed(() => {
    const courseId = this.filterCourseId();
    const providerId = this.filterProviderId();
    const source = this.filterSource();
    const search = this.searchTerm().trim().toLocaleLowerCase('ar');

    return this.rows().filter(row => {
      if (courseId && (row.casualCourseId ?? row.sessionId) !== courseId) return false;
      if (providerId && row.trainingProviderId !== providerId) return false;
      if (source === 'session' && !row.sessionId) return false;
      if (source === 'casual' && !row.casualCourseId) return false;
      if (!search) return true;

      return [
        row.courseNameAr,
        row.trainingProviderName,
        row.invoiceOriginalFileName,
        row.notes,
        row.confirmedByName,
      ].filter(Boolean).join(' ').toLocaleLowerCase('ar').includes(search);
    }).sort((a, b) => {
      const priority = (row: CoursePaymentDto): number =>
        row.status === PaymentStatus.Draft ? 0 : row.status === PaymentStatus.Confirmed ? 1 : 2;
      return priority(a) - priority(b)
        || (b.creationTime ?? '').localeCompare(a.creationTime ?? '');
    });
  });

  private statusOptions: { value: PaymentStatus; text: string }[] = [];

  async ngOnInit(): Promise<void> {
    this.statusOptions = [
      { value: PaymentStatus.Draft, text: this.l.t('::Training.PaymentStatus.Draft') },
      { value: PaymentStatus.Confirmed, text: this.l.t('::Training.Payments.CoursePayment.StatusConfirmed') },
      { value: PaymentStatus.Cancelled, text: this.l.t('::Training.PaymentStatus.Cancelled') },
    ];
    await this.loadRows();
  }

  async reload(): Promise<void> {
    await this.loadRows();
  }

  onCourseFilterChange(value: string | null): void {
    this.filterCourseId.set(value);
  }

  onProviderFilterChange(value: string | null): void {
    this.filterProviderId.set(value);
  }

  onSourceFilterChange(value: 'all' | 'session' | 'casual'): void {
    this.filterSource.set(value);
  }

  onSearchChange(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value ?? '');
  }

  clearFilters(): void {
    this.filterCourseId.set(null);
    this.filterProviderId.set(null);
    this.filterSource.set('all');
    this.searchTerm.set('');
  }

  openPaymentStage(row: CoursePaymentDto): void {
    if (row.sessionId) {
      void this.router.navigate(['/training/sessions', row.sessionId], {
        queryParams: { stage: 'payments' },
      });
      return;
    }

    if (row.casualCourseId) {
      void this.router.navigate(['/training/casual-courses', row.casualCourseId], {
        queryParams: { stage: 'payments' },
      });
    }
  }

  async downloadInvoice(row: CoursePaymentDto): Promise<void> {
    if (!row.id || !row.hasInvoice || !this.canDownload()) return;
    try {
      const blob = await firstValueFrom(this.paymentService.downloadInvoice(row.id));
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = row.invoiceOriginalFileName ?? 'invoice.pdf';
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    } catch (error) {
      this.showError(this.extractError(error));
    }
  }

  dismissToast(): void {
    this.toast.set({ visible: false, text: '' });
  }

  formatOMR(value: number | null | undefined): string {
    return (value ?? 0).toLocaleString('en-US', {
      minimumFractionDigits: 3,
      maximumFractionDigits: 3,
    });
  }

  statusBadgeText(status: PaymentStatus | undefined): string {
    return this.statusOptions.find(option => option.value === status)?.text ?? '';
  }

  statusBadgeCss(status: PaymentStatus | undefined): string {
    switch (status) {
      case PaymentStatus.Confirmed: return 'status-badge status-confirmed';
      case PaymentStatus.Cancelled: return 'status-badge status-cancelled';
      default: return 'status-badge status-draft';
    }
  }

  scenarioLabelFor(scenario: FundingScenario | null | undefined): string {
    switch (scenario) {
      case FundingScenario.FundingSourceCoversAll:
        return this.l.t('::Training.Payments.CoursePayment.Scenario1');
      case FundingScenario.FundingSourceCoversCourse:
        return this.l.t('::Training.Payments.CoursePayment.Scenario2');
      case FundingScenario.FinancialItemsCoverAll:
        return this.l.t('::Training.Payments.CoursePayment.Scenario3');
      default:
        return this.l.t('::Training.Payments.CoursePayment.ScenarioUnknown');
    }
  }

  private async loadRows(): Promise<void> {
    this.loading.set(true);
    try {
      const input: CoursePaymentGetListInput = { maxResultCount: 1000 };
      const result = await firstValueFrom(this.paymentService.getList(input));
      this.rows.set(result.items ?? []);
    } catch (error) {
      this.rows.set([]);
      this.showError(this.extractError(error));
    } finally {
      this.loading.set(false);
    }
  }

  private distinctOptions(
    idSelector: (row: CoursePaymentDto) => string | null | undefined,
    labelSelector: (row: CoursePaymentDto) => string | null | undefined,
  ): FilterOption[] {
    const options = new Map<string, string>();
    for (const row of this.rows()) {
      const id = idSelector(row);
      if (id) options.set(id, labelSelector(row) || '—');
    }
    return Array.from(options, ([id, label]) => ({ id, label }))
      .sort((a, b) => a.label.localeCompare(b.label, 'ar'));
  }

  private showError(text: string): void {
    this.toast.set({ visible: true, text });
    setTimeout(() => this.dismissToast(), 4500);
  }

  private extractError(error: unknown): string {
    const fallback = this.l.t('::Training.Payments.GenericError');
    if (error && typeof error === 'object') {
      const response = error as { error?: { error?: { message?: string } }; message?: string };
      return response.error?.error?.message ?? response.message ?? fallback;
    }
    return fallback;
  }
}
