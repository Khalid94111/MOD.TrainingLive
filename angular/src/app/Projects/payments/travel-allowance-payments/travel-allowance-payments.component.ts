import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { PaymentStatus } from 'src/app/proxy/training/enums/payment-status.enum';
import { PersonnelType } from 'src/app/proxy/training/enums/personnel-type.enum';
import type {
  TravelAllowancePaymentDto,
  TravelAllowancePaymentGetListInput,
} from 'src/app/proxy/training/payments/dtos/models';
import { TravelAllowancePaymentService } from 'src/app/proxy/training/payments/travel-allowance-payment.service';
import { TrainingLocalizationHelper } from '../../shared';

type TravelPaymentSource = 'session' | 'casual';

interface TravelPaymentGroup {
  key: string;
  parentId: string | null;
  source: TravelPaymentSource;
  courseName: string;
  rows: TravelAllowancePaymentDto[];
  employeeCount: number;
  confirmedCount: number;
  totalOMR: number;
  allConfirmed: boolean;
  hasDraft: boolean;
  effectiveTravelDays: number;
  externalRequestId: string | null;
  externalStatus: string | null;
  confirmedAt: string | null;
  latestActivityAt: string;
}

@Component({
  standalone: true,
  selector: 'app-travel-allowance-payments',
  templateUrl: './travel-allowance-payments.component.html',
  styleUrls: [
    './travel-allowance-payments.component.scss',
    '../../shared/gtms-design.scss',
  ],
  imports: [CommonModule, LocalizationPipe],
})
export class TravelAllowancePaymentsComponent implements OnInit {
  private readonly paymentService = inject(TravelAllowancePaymentService);
  private readonly router = inject(Router);
  readonly l = inject(TrainingLocalizationHelper);

  readonly PaymentStatus = PaymentStatus;
  readonly rows = signal<TravelAllowancePaymentDto[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly filterSearch = signal('');
  readonly expandedGroups = signal<Set<string>>(new Set());

  readonly statusOptions: { value: PaymentStatus; text: string }[] = [];
  readonly personnelOptions: { value: PersonnelType; text: string }[] = [];

  readonly groups = computed<TravelPaymentGroup[]>(() => {
    const byParent = new Map<string, TravelAllowancePaymentDto[]>();
    for (const row of this.rows()) {
      const key = this.groupKey(row);
      byParent.set(key, [...(byParent.get(key) ?? []), row]);
    }

    return Array.from(byParent.entries())
      .map(([key, groupRows]): TravelPaymentGroup => {
        const first = groupRows[0];
        const source: TravelPaymentSource = first.sessionId ? 'session' : 'casual';
        const confirmedRows = groupRows.filter(row => row.status === PaymentStatus.Confirmed);
        const confirmedDates = groupRows
          .map(row => row.confirmedAt)
          .filter((value): value is string => !!value)
          .sort();
        const activityDates = groupRows
          .map(row => row.externalResponseAt ?? row.confirmedAt ?? row.creationTime ?? '')
          .filter(Boolean)
          .sort();

        return {
          key,
          parentId: first.sessionId ?? first.casualCourseId ?? null,
          source,
          courseName: first.courseNameAr?.trim()
            || this.l.t('::Training.Payments.TravelRegister.UnknownCourse'),
          rows: [...groupRows].sort((a, b) =>
            (a.employeeNameAr ?? '').localeCompare(b.employeeNameAr ?? '', 'ar')),
          employeeCount: groupRows.length,
          confirmedCount: confirmedRows.length,
          totalOMR: groupRows.reduce((sum, row) => sum + (row.totalOMR ?? 0), 0),
          allConfirmed: groupRows.length > 0 && confirmedRows.length === groupRows.length,
          hasDraft: groupRows.some(row => row.status === PaymentStatus.Draft),
          effectiveTravelDays: Math.max(0, ...groupRows.map(row => row.effectiveTravelDays ?? 0)),
          externalRequestId: groupRows.find(row => !!row.externalRequestId)?.externalRequestId ?? null,
          externalStatus: groupRows.find(row => !!row.externalStatus)?.externalStatus ?? null,
          confirmedAt: confirmedDates.at(-1) ?? null,
          latestActivityAt: activityDates.at(-1) ?? '',
        };
      })
      .sort((a, b) => {
        if (a.source !== b.source) return a.source === 'session' ? -1 : 1;
        return b.latestActivityAt.localeCompare(a.latestActivityAt);
      });
  });

  readonly filteredGroups = computed(() => {
    const query = this.filterSearch().trim().toLowerCase();
    if (!query) return this.groups();

    return this.groups().filter(group => {
      const employees = group.rows
        .map(row => `${row.employeeNameAr ?? ''} ${row.rankNameAr ?? ''}`)
        .join(' ');
      return `${group.courseName} ${group.externalRequestId ?? ''} ${employees}`
        .toLowerCase()
        .includes(query);
    });
  });

  readonly hasSearch = computed(() => this.filterSearch().trim().length > 0);

  async ngOnInit(): Promise<void> {
    this.statusOptions.push(
      { value: PaymentStatus.Draft, text: this.l.t('::Training.PaymentStatus.Draft') },
      { value: PaymentStatus.Confirmed, text: this.l.t('::Training.PaymentStatus.Confirmed') },
      { value: PaymentStatus.Cancelled, text: this.l.t('::Training.PaymentStatus.Cancelled') },
    );
    this.personnelOptions.push(
      { value: PersonnelType.Officer, text: this.l.t('::Training.PersonnelType.Officer') },
      { value: PersonnelType.Enlisted, text: this.l.t('::Training.PersonnelType.Enlisted') },
    );
    await this.loadRows();
  }

  async reload(): Promise<void> {
    await this.loadRows();
  }

  onSearchInput(event: Event): void {
    this.filterSearch.set((event.target as HTMLInputElement).value ?? '');
  }

  clearSearch(): void {
    this.filterSearch.set('');
  }

  isExpanded(groupKey: string): boolean {
    return this.expandedGroups().has(groupKey);
  }

  toggleGroup(groupKey: string): void {
    const expanded = new Set(this.expandedGroups());
    if (expanded.has(groupKey)) expanded.delete(groupKey);
    else expanded.add(groupKey);
    this.expandedGroups.set(expanded);
  }

  openParent(group: TravelPaymentGroup): void {
    if (!group.parentId) return;
    const route = group.source === 'session'
      ? ['/training/sessions', group.parentId]
      : ['/training/casual-courses', group.parentId];
    void this.router.navigate(route, { queryParams: { stage: 'payments' } });
  }

  openTravelRequest(group: TravelPaymentGroup): void {
    if (group.externalRequestId)
      void this.router.navigate(['/travel/requests', group.externalRequestId]);
  }

  formatOMR(value: number | null | undefined): string {
    return (value ?? 0).toLocaleString('en-US', {
      minimumFractionDigits: 3,
      maximumFractionDigits: 3,
    });
  }

  shortDate(iso: string | null | undefined): string {
    return (iso ?? '').substring(0, 10) || '—';
  }

  personnelBadgeText(type: PersonnelType | undefined): string {
    return this.personnelOptions.find(option => option.value === type)?.text ?? '';
  }

  groupStatusKey(group: TravelPaymentGroup): string {
    if (group.allConfirmed && group.externalRequestId)
      return '::Training.Payments.TravelRegister.ConfirmedByTravel';
    if (group.allConfirmed) return '::Training.PaymentStatus.Confirmed';
    if (group.hasDraft) return '::Training.PaymentStatus.Draft';
    return '::Training.PaymentStatus.Cancelled';
  }

  groupStatusCss(group: TravelPaymentGroup): string {
    if (group.allConfirmed) return 'record-status confirmed';
    if (group.hasDraft) return 'record-status draft';
    return 'record-status cancelled';
  }

  sourceKey(source: TravelPaymentSource): string {
    return source === 'session'
      ? '::Training.Payments.TravelRegister.SourceAnnual'
      : '::Training.Payments.TravelRegister.SourceCasual';
  }

  private async loadRows(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const input: TravelAllowancePaymentGetListInput = { maxResultCount: 1000 };
      const result = await firstValueFrom(this.paymentService.getList(input));
      this.rows.set(result.items ?? []);
    } catch (error) {
      this.loadError.set(this.extractError(error));
    } finally {
      this.loading.set(false);
    }
  }

  private groupKey(row: TravelAllowancePaymentDto): string {
    if (row.sessionId) return `session:${row.sessionId}`;
    if (row.casualCourseId) return `casual:${row.casualCourseId}`;
    return `payment:${row.id}`;
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
