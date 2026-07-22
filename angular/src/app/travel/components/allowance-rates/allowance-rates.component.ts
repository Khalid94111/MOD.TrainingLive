import { Component, inject, OnInit, OnDestroy, TemplateRef, ViewChild, ChangeDetectionStrategy, computed, signal, model } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbModal, NgbModalModule } from '@ng-bootstrap/ng-bootstrap';
import { ConfirmationService, ToasterService } from '@abp/ng.theme.shared';
import { LocalizationModule, PermissionDirective, LocalizationService } from '@abp/ng.core';
import { forkJoin } from 'rxjs';
import { AllowanceRateService, AllowanceRateDto, CreateUpdateAllowanceRateDto } from '../../services/allowance-rate.service';
import { RankLookupService, RankLookupDto } from '../../services/rank-lookup.service';

interface GroupedRate {
  rankId: string;
  rankName: string;
  rateA?: AllowanceRateDto;
  rateB?: AllowanceRateDto;
  amountA: number;
  amountB: number;
  ticketClassA: number;
  ticketClassB: number;
  isActiveA: boolean;
  isActiveB: boolean;
}

@Component({
  selector: 'app-allowance-rates',
  standalone: true,
  imports: [CommonModule, FormsModule, NgbModalModule, LocalizationModule, PermissionDirective],
  templateUrl: './allowance-rates.component.html',
  styleUrls: ['./allowance-rates.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AllowanceRatesComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();
  private readonly rateService = inject(AllowanceRateService);
  private readonly rankService = inject(RankLookupService);
  private readonly modalService = inject(NgbModal);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly toaster = inject(ToasterService);
  private readonly localizationService = inject(LocalizationService);

  // Signals for reactive state
  readonly rates = signal<AllowanceRateDto[]>([]);
  readonly groupedRates = signal<GroupedRate[]>([]);
  readonly filteredRates = signal<GroupedRate[]>([]);
  readonly ranks = signal<RankLookupDto[]>([]);
  readonly loading = signal<boolean>(true);

  // Two-way binding properties for ngModel
  searchText = '';
  showActiveOnly = false;

  // Modal state
  editingRate: { rateA?: AllowanceRateDto; rateB?: AllowanceRateDto } | null = null;
  rateForm = {
    rankId: '',
    amountA: 0,
    amountB: 0,
    ticketClassA: 1,
    ticketClassB: 1,
    isActive: true,
  };

  @ViewChild('rateModal', { static: false }) rateModalRef!: TemplateRef<any>;

  // Computed statistics
  readonly activeRatesCount = computed(() =>
    this.groupedRates().filter(r => r.isActiveA || r.isActiveB).length
  );

  readonly highestRate = computed(() => {
    const allAmounts = this.groupedRates().reduce<number[]>((acc, r) => [...acc, r.amountA, r.amountB], []);
    return allAmounts.length > 0 ? Math.max(...allAmounts) : 0;
  });

  readonly averageRate = computed(() => {
    const allAmounts = this.groupedRates().reduce<number[]>((acc, r) => [...acc, r.amountA, r.amountB], []);
    return allAmounts.length > 0
      ? allAmounts.reduce((a, b) => a + b, 0) / allAmounts.length
      : 0;
  });

  ngOnInit(): void {
    this.loadRates();
    this.loadRanks();
  }

  loadRates(): void {
    this.loading.set(true);
    this.rateService.getList({ allowanceType: 1, maxResultCount: 1000 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
      next: (res) => {
        this.rates.set(res.items);
        this.buildGroupedRates();
        this.applyFilters();
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toaster.error(
          this.localizationService.instant('Travel::FailedToLoadRates')
        );
      },
    });
  }

  loadRanks(): void {
    this.rankService.getRanks().pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => this.ranks.set(res),
      error: () => {
        this.toaster.error(
          this.localizationService.instant('Travel::FailedToLoadRanks')
        );
      },
    });
  }

  onSearch(): void {
    this.applyFilters();
  }

  clearSearch(): void {
    this.searchText = '';
    this.applyFilters();
  }

  toggleActiveFilter(): void {
    this.showActiveOnly = !this.showActiveOnly;
    this.applyFilters();
  }

  private applyFilters(): void {
    let result = this.groupedRates();

    const search = this.searchText.trim().toLowerCase();
    if (search) {
      result = result.filter(r =>
        r.rankName.toLowerCase().includes(search)
      );
    }

    if (this.showActiveOnly) {
      result = result.filter(r => r.isActiveA || r.isActiveB);
    }

    this.filteredRates.set(result);
  }

  private buildGroupedRates(): void {
    const dailyRates = this.rates().filter(rate => rate.allowanceType === 1);
    const grouped = new Map<string, GroupedRate>();

    for (const rate of dailyRates) {
      const current: GroupedRate = grouped.get(rate.rankId) || {
        rankId: rate.rankId,
        rankName: rate.rankName,
        amountA: 0,
        amountB: 0,
        ticketClassA: 1,
        ticketClassB: 1,
        isActiveA: false,
        isActiveB: false,
      };

      if (rate.category === 1) {
        current.rateA = rate;
        current.amountA = rate.amount;
        current.ticketClassA = rate.ticketClass || 1;
        current.isActiveA = rate.isActive;
      }

      if (rate.category === 2) {
        current.rateB = rate;
        current.amountB = rate.amount;
        current.ticketClassB = rate.ticketClass || 1;
        current.isActiveB = rate.isActive;
      }

      grouped.set(rate.rankId, current);
    }

    const sorted = Array.from(grouped.values()).sort((a, b) =>
      a.rankName.localeCompare(b.rankName)
    );

    this.groupedRates.set(sorted);
  }

  openModal(rate?: GroupedRate): void {
    this.editingRate = rate
      ? { rateA: rate.rateA, rateB: rate.rateB }
      : null;

    this.rateForm = rate
      ? {
          rankId: rate.rankId,
          amountA: rate.amountA,
          amountB: rate.amountB,
          ticketClassA: rate.ticketClassA,
          ticketClassB: rate.ticketClassB,
          isActive: rate.isActiveA || rate.isActiveB,
        }
      : {
          rankId: this.ranks()[0]?.id || '',
          amountA: 0,
          amountB: 0,
          ticketClassA: 1,
          ticketClassB: 1,
          isActive: true,
        };

    this.modalService.open(this.rateModalRef, {
      backdrop: 'static',
      centered: true,
      size: 'lg',
      windowClass: 'rate-modal-window',
    });
  }

  isFormValid(): boolean {
    return !!this.rateForm.rankId &&
      this.rateForm.amountA >= 0 &&
      this.rateForm.amountB >= 0;
  }

  saveRate(): void {
    if (!this.isFormValid()) return;

    const inputA = this.createRateInput(1, this.rateForm.amountA, this.rateForm.ticketClassA);
    const inputB = this.createRateInput(2, this.rateForm.amountB, this.rateForm.ticketClassB);

    const requests = [
      this.editingRate?.rateA
        ? this.rateService.update(this.editingRate.rateA.id, inputA)
        : this.rateService.create(inputA),
      this.editingRate?.rateB
        ? this.rateService.update(this.editingRate.rateB.id, inputB)
        : this.rateService.create(inputB),
    ];

    forkJoin(requests).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toaster.success(
          this.localizationService.instant('Travel::SavedSuccessfully')
        );
        this.loadRates();
      },
      error: () => {
        this.toaster.error(
          this.localizationService.instant('Travel::SaveFailed')
        );
      },
    });
  }

  deleteRateGroup(rate: GroupedRate): void {
    this.confirmationService
      .warn(
        this.localizationService.instant('Travel::DeleteRateConfirmation'),
        this.localizationService.instant('Travel::AreYouSure')
      )
      .subscribe(status => {
        if (status !== 'confirm') return;

        const deletions = [rate.rateA, rate.rateB]
          .filter((item): item is AllowanceRateDto => !!item)
          .map(item => this.rateService.delete(item.id));

        forkJoin(deletions).pipe(takeUntil(this.destroy$)).subscribe({
          next: () => {
            this.toaster.success(
              this.localizationService.instant('Travel::DeletedSuccessfully')
            );
            this.loadRates();
          },
          error: () => {
            this.toaster.error(
              this.localizationService.instant('Travel::DeleteFailed')
            );
          },
        });
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  getTicketClassLabel(ticketClass: number): string {
    return ticketClass === 2
      ? 'Travel::TicketClass.Business'
      : 'Travel::TicketClass.Economy';
  }

  private createRateInput(
    category: number,
    amount: number,
    ticketClass: number
  ): CreateUpdateAllowanceRateDto {
    return {
      rankId: this.rateForm.rankId,
      category,
      allowanceType: 1,
      amount: Number(amount || 0),
      annualPartialAmount: undefined,
      ticketClass,
      isActive: this.rateForm.isActive,
    };
  }
}
