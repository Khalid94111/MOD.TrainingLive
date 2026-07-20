import { CommonModule } from '@angular/common';
import { Component, computed, inject, input, signal } from '@angular/core';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import type { CourseSessionDetailDto } from 'src/app/proxy/training/plans/dtos/models';
import { CoursePaymentService } from 'src/app/proxy/training/payments/course-payment.service';
import type {
  CoursePaymentDto,
  CreateUpdateCoursePaymentDto,
  TravelAllowancePaymentDto,
} from 'src/app/proxy/training/payments/dtos/models';
import { CourseType } from 'src/app/proxy/training/enums/course-type.enum';
import { PaymentStatus } from 'src/app/proxy/training/enums/payment-status.enum';

import { TrainingLocalizationHelper } from '../../../../shared';
import { SessionDetailRefreshService } from '../../session-detail-refresh.service';

@Component({
  selector: 'app-session-section-payments',
  standalone: true,
  templateUrl: './session-section-payments.component.html',
  styleUrls: ['../../../../shared/gtms-design.scss', './session-section-payments.component.scss'],
  imports: [CommonModule, LocalizationPipe],
})
export class SessionSectionPaymentsComponent {
  readonly l = inject(TrainingLocalizationHelper);
  private readonly router = inject(Router);
  private readonly permissions = inject(PermissionService);
  private readonly paymentService = inject(CoursePaymentService);
  private readonly refresh = inject(SessionDetailRefreshService);

  readonly CourseType = CourseType;
  readonly PaymentStatus = PaymentStatus;

  readonly session = input<CourseSessionDetailDto | null>(null);
  readonly travelAllowancePayments = input<TravelAllowancePaymentDto[]>([]);
  readonly coursePayment = input<CoursePaymentDto | null>(null);

  readonly dialogOpen = signal(false);
  readonly workingPayment = signal<CoursePaymentDto | null>(null);
  readonly saving = signal(false);
  readonly uploading = signal(false);
  readonly dialogError = signal<string | null>(null);
  readonly dialogSuccess = signal<string | null>(null);
  readonly confirmRequested = signal(false);
  readonly invoiceAmountOMR = signal(0);
  readonly invoiceDate = signal('');
  readonly notes = signal('');

  readonly canCreate = computed(() =>
    this.permissions.getGrantedPolicy('TrainingPayments.CoursePayments.Create'));
  readonly canUpdate = computed(() =>
    this.permissions.getGrantedPolicy('TrainingPayments.CoursePayments.Update'));
  readonly canConfirm = computed(() =>
    this.permissions.getGrantedPolicy('TrainingPayments.CoursePayments.Confirm'));
  readonly canUpload = computed(() =>
    this.permissions.getGrantedPolicy('TrainingPayments.CoursePayments.UploadInvoice'));
  readonly canDownload = computed(() =>
    this.permissions.getGrantedPolicy('TrainingPayments.CoursePayments.DownloadInvoice'));

  readonly isInternal = computed(() => this.session()?.courseType === CourseType.Internal);
  readonly showTravelCard = computed(
    () => this.session()?.courseType === CourseType.ExternalInternational,
  );

  readonly expectedNomineeCount = computed(() => this.session()?.nominations?.length ?? 0);
  readonly travelConfirmedCount = computed(() =>
    this.travelAllowancePayments().filter(p => p.status === PaymentStatus.Confirmed).length,
  );
  readonly travelTotalOMR = computed(() =>
    this.travelAllowancePayments().reduce((sum, payment) => sum + (payment.totalOMR ?? 0), 0),
  );
  readonly travelStatusBadge = computed(() => {
    if (this.isInternal()) {
      return { textKey: '::Training.Payments.Section5.TravelBadgeNotRequired', css: 'badge-muted' };
    }
    const confirmed = this.travelConfirmedCount();
    const expected = this.expectedNomineeCount();
    if (expected === 0) {
      return { textKey: '::Training.Payments.Section5.TravelBadgeNoNominees', css: 'badge-muted' };
    }
    if (confirmed === expected) {
      return { textLiteral: `${confirmed}/${expected} ✓`, css: 'badge-confirmed' };
    }
    return { textLiteral: `${confirmed}/${expected}`, css: confirmed > 0 ? 'badge-pending' : 'badge-empty' };
  });

  readonly coursePaymentBadge = computed(() => {
    const payment = this.coursePayment();
    if (!payment) return { textKey: '::Training.Payments.Section5.CourseEmptyBadge', css: 'badge-empty' };
    if (payment.status === PaymentStatus.Confirmed) {
      return { textKey: '::Training.Payments.Section5.CourseConfirmedBadge', css: 'badge-confirmed' };
    }
    if (payment.status === PaymentStatus.Cancelled) {
      return { textKey: '::Training.Payments.Section5.CourseCancelledBadge', css: 'badge-cancelled' };
    }
    return { textKey: '::Training.Payments.Section5.CourseDraftBadge', css: 'badge-pending' };
  });

  readonly allTravelConfirmed = computed(() => {
    if (!this.showTravelCard()) return true;
    const expected = this.expectedNomineeCount();
    return expected > 0 && this.travelConfirmedCount() === expected;
  });
  readonly coursePaymentConfirmed = computed(
    () => this.coursePayment()?.status === PaymentStatus.Confirmed,
  );
  readonly lifecycleComplete = computed(
    () => this.allTravelConfirmed() && this.coursePaymentConfirmed(),
  );
  readonly totalLifecycleOMR = computed(
    () => this.travelTotalOMR() + (this.coursePayment()?.invoiceAmountOMR ?? 0),
  );

  readonly dialogPayment = computed(() => this.workingPayment() ?? this.coursePayment());
  readonly dialogLocked = computed(
    () => this.dialogPayment()?.status === PaymentStatus.Confirmed,
  );
  readonly dialogEditable = computed(() => {
    if (this.dialogLocked()) return false;
    return this.dialogPayment() ? this.canUpdate() : this.canCreate();
  });
  readonly hasInvoice = computed(() => !!this.dialogPayment()?.hasInvoice);
  readonly agreedAmountOMR = computed(() => this.session()?.selectedPriceQuoteAmountOMR ?? 0);
  readonly amountDifferenceOMR = computed(
    () => this.roundOMR(this.invoiceAmountOMR() - this.agreedAmountOMR()),
  );

  formatOMR(value: number | null | undefined): string {
    return (value ?? 0).toLocaleString('en-US', {
      minimumFractionDigits: 3,
      maximumFractionDigits: 3,
    });
  }

  formatDate(value: string | null | undefined): string {
    return value?.substring(0, 10) || '—';
  }

  openPaymentDialog(): void {
    const session = this.session();
    if (!session?.id || !session.selectedPriceQuoteProviderId) return;

    const payment = this.coursePayment();
    this.workingPayment.set(payment);
    this.invoiceAmountOMR.set(payment?.invoiceAmountOMR ?? this.agreedAmountOMR());
    this.invoiceDate.set(payment?.invoiceDate?.substring(0, 10) ?? new Date().toISOString().substring(0, 10));
    this.notes.set(payment?.notes ?? '');
    this.dialogError.set(null);
    this.dialogSuccess.set(null);
    this.confirmRequested.set(false);
    this.dialogOpen.set(true);
  }

  closePaymentDialog(): void {
    if (this.saving() || this.uploading()) return;
    this.dialogOpen.set(false);
    this.workingPayment.set(null);
    this.dialogError.set(null);
    this.dialogSuccess.set(null);
    this.confirmRequested.set(false);
  }

  closeFromBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) this.closePaymentDialog();
  }

  updateInvoiceAmount(event: Event): void {
    this.invoiceAmountOMR.set(Number((event.target as HTMLInputElement).value) || 0);
    this.clearMessages();
  }

  updateInvoiceDate(event: Event): void {
    this.invoiceDate.set((event.target as HTMLInputElement).value);
    this.clearMessages();
  }

  updateNotes(event: Event): void {
    this.notes.set((event.target as HTMLTextAreaElement).value);
    this.clearMessages();
  }

  async saveDraft(): Promise<void> {
    await this.persistDraft(false);
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    input.value = '';
    if (!file) return;

    if (file.type !== 'application/pdf' && !file.name.toLowerCase().endsWith('.pdf')) {
      this.dialogError.set(this.l.t('::Training.Payments.CoursePayment.Dialog.UploadOnlyPdf'));
      return;
    }
    if (file.size > 25 * 1024 * 1024) {
      this.dialogError.set(this.l.t('::Training.Payments.CoursePayment.Dialog.UploadTooLarge'));
      return;
    }

    const payment = await this.persistDraft(true);
    if (!payment?.id) return;

    this.uploading.set(true);
    this.dialogError.set(null);
    this.dialogSuccess.set(null);
    try {
      const formData = new FormData();
      formData.append('file', file, file.name);
      const updated = await firstValueFrom(this.paymentService.uploadInvoice(payment.id, formData));
      this.workingPayment.set(updated);
      this.dialogSuccess.set(this.l.t('::Training.Sessions.Detail.Section5.InvoiceUploaded'));
      this.refresh.refresh();
    } catch (error) {
      this.dialogError.set(this.extractError(error));
    } finally {
      this.uploading.set(false);
    }
  }

  requestConfirm(): void {
    if (!this.canConfirm() || this.dialogLocked()) return;
    if (!this.hasInvoice()) {
      this.dialogError.set(this.l.t('::Training.Payments.CoursePayment.Dialog.UploadRequired'));
      return;
    }
    if (this.invoiceAmountOMR() <= 0) {
      this.dialogError.set(this.l.t('::Training.Sessions.Detail.Section5.InvoiceAmountRequired'));
      return;
    }
    if (!this.invoiceDate()) {
      this.dialogError.set(this.l.t('::Training.Payments.CoursePayment.Dialog.InvoiceDateRequired'));
      return;
    }
    this.dialogError.set(null);
    this.dialogSuccess.set(null);
    this.confirmRequested.set(true);
  }

  cancelConfirm(): void {
    this.confirmRequested.set(false);
  }

  async confirmPayment(): Promise<void> {
    if (!this.confirmRequested() || !this.canConfirm() || this.dialogLocked()) return;

    const payment = await this.persistDraft(true);
    if (!payment?.id) return;

    this.saving.set(true);
    this.dialogError.set(null);
    this.dialogSuccess.set(null);
    try {
      const result = await firstValueFrom(this.paymentService.confirm(payment.id));
      if (result.payment) this.workingPayment.set(result.payment);
      this.confirmRequested.set(false);
      this.dialogSuccess.set(this.l.t('::Training.Sessions.Detail.Section5.PaymentConfirmed'));
      this.refresh.refresh();
    } catch (error) {
      this.dialogError.set(this.extractError(error));
    } finally {
      this.saving.set(false);
    }
  }

  async downloadInvoice(): Promise<void> {
    const payment = this.dialogPayment() ?? this.coursePayment();
    if (!payment?.id || !payment.hasInvoice || !this.canDownload()) return;
    try {
      const blob = await firstValueFrom(this.paymentService.downloadInvoice(payment.id));
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = payment.invoiceOriginalFileName || 'invoice.pdf';
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    } catch (error) {
      this.dialogError.set(this.extractError(error));
    }
  }

  goTravelAllowances(event: Event): void {
    event.stopPropagation();
    void this.router.navigate(['/training/payments/travel-allowances']);
  }

  goCoursePayments(event: Event): void {
    event.stopPropagation();
    void this.router.navigate(['/training/payments/courses']);
  }

  private async persistDraft(silent: boolean): Promise<CoursePaymentDto | null> {
    const session = this.session();
    const current = this.dialogPayment();
    if (!session?.id || !session.selectedPriceQuoteProviderId) {
      this.dialogError.set(this.l.t('::Training.Sessions.Detail.Section5.MissingWinningQuote'));
      return null;
    }
    if (this.invoiceAmountOMR() <= 0) {
      this.dialogError.set(this.l.t('::Training.Sessions.Detail.Section5.InvoiceAmountRequired'));
      return null;
    }
    if (!this.invoiceDate()) {
      this.dialogError.set(this.l.t('::Training.Payments.CoursePayment.Dialog.InvoiceDateRequired'));
      return null;
    }
    if (current?.status === PaymentStatus.Confirmed) return current;
    if (!current && !this.canCreate()) return null;
    if (current && !this.canUpdate()) return null;

    const dto: CreateUpdateCoursePaymentDto = {
      casualCourseId: null,
      sessionId: session.id,
      trainingProviderId: session.selectedPriceQuoteProviderId,
      invoiceAmountOMR: this.invoiceAmountOMR(),
      invoiceDate: this.invoiceDate(),
      notes: this.notes().trim() || null,
    };

    this.saving.set(true);
    this.dialogError.set(null);
    if (!silent) this.dialogSuccess.set(null);
    try {
      const saved = current?.id
        ? await firstValueFrom(this.paymentService.update(current.id, dto))
        : await firstValueFrom(this.paymentService.create(dto));
      this.workingPayment.set(saved);
      if (!silent) {
        this.dialogSuccess.set(this.l.t('::Training.Payments.CoursePayment.Dialog.SaveDraftSuccess'));
      }
      this.refresh.refresh();
      return saved;
    } catch (error) {
      this.dialogError.set(this.extractError(error));
      return null;
    } finally {
      this.saving.set(false);
    }
  }

  private clearMessages(): void {
    this.dialogError.set(null);
    this.dialogSuccess.set(null);
    this.confirmRequested.set(false);
  }

  private extractError(error: unknown): string {
    const fallback = this.l.t('::Training.Payments.GenericError');
    if (error && typeof error === 'object') {
      const response = error as { error?: { error?: { message?: string } }; message?: string };
      return response.error?.error?.message ?? response.message ?? fallback;
    }
    return fallback;
  }

  private roundOMR(value: number): number {
    return Math.round((value + Number.EPSILON) * 1000) / 1000;
  }
}
