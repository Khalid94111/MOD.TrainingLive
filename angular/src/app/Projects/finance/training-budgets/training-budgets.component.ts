import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';
import {
  TrainingBudgetActivityDto,
  TrainingBudgetDto,
} from 'src/app/proxy/training/finance/dtos';
import { FinancialItemType } from 'src/app/proxy/training/enums/financial-item-type.enum';
import { TrainingBudgetService } from '../../shared/services/finance-proxy.service';
import { TrainingLocalizationHelper } from '../../shared';

type BudgetFilter = 'all' | 'attention' | 'recovery';

@Component({
  selector: 'app-training-budgets',
  standalone: true,
  imports: [CommonModule, LocalizationPipe],
  templateUrl: './training-budgets.component.html',
  styleUrl: './training-budgets.component.scss',
})
export class TrainingBudgetsComponent implements OnInit {
  private readonly service = inject(TrainingBudgetService);
  readonly l = inject(TrainingLocalizationHelper);

  budgets = signal<TrainingBudgetDto[]>([]);
  isLoading = signal(false);
  isSaving = signal(false);
  loadError = signal<string | null>(null);

  selectedYear = signal(new Date().getFullYear());
  yearOptions = signal<number[]>([new Date().getFullYear()]);
  searchText = signal('');
  activeFilter = signal<BudgetFilter>('all');
  expandedFinancialItemId = signal<string | null>(null);

  isThresholdDialogVisible = signal(false);
  editingFinancialItemId = signal<string | null>(null);
  editingFinancialItemName = signal('');
  thresholdValue = signal(80);

  filteredBudgets = computed(() => {
    const query = this.searchText().trim().toLowerCase();
    const filter = this.activeFilter();

    return this.budgets().filter(budget => {
      const matchesSearch =
        !query ||
        (budget.financialItemNameAr ?? '').toLowerCase().includes(query) ||
        (budget.financialItemNameEn ?? '').toLowerCase().includes(query) ||
        (budget.financialItemVoteCode ?? '').toLowerCase().includes(query) ||
        (budget.budgetCategoryNameAr ?? '').toLowerCase().includes(query);
      if (!matchesSearch) return false;
      if (filter === 'attention') return !!budget.isOverThreshold || !!budget.isOverBudget;
      if (filter === 'recovery') return (budget.amountToRecoverOMR ?? 0) > 0;
      return true;
    });
  });

  totalAllocated = computed(() =>
    this.budgets().reduce((sum, item) => sum + (item.allocatedAmountOMR ?? 0), 0),
  );
  totalGrossSpent = computed(() =>
    this.budgets().reduce((sum, item) => sum + (item.grossSpentAmountOMR ?? 0), 0),
  );
  totalRecovered = computed(() =>
    this.budgets().reduce((sum, item) => sum + (item.recoveredAmountOMR ?? 0), 0),
  );
  totalPendingRecovery = computed(() =>
    this.budgets().reduce((sum, item) => sum + (item.amountToRecoverOMR ?? 0), 0),
  );
  totalNetSpent = computed(() =>
    this.budgets().reduce((sum, item) => sum + (item.netSpentAmountOMR ?? 0), 0),
  );
  totalRemaining = computed(() =>
    this.budgets().reduce((sum, item) => sum + (item.remaining ?? 0), 0),
  );
  attentionCount = computed(() =>
    this.budgets().filter(item => item.isOverThreshold || item.isOverBudget).length,
  );

  async ngOnInit(): Promise<void> {
    await this.loadYears();
    await this.loadData();
  }

  private async loadYears(): Promise<void> {
    try {
      const years = await this.service.getYears();
      const current = new Date().getFullYear();
      const available = [...new Set([current, ...(years ?? [])])].sort((a, b) => b - a);
      this.yearOptions.set(available);
      if (!available.includes(this.selectedYear())) {
        this.selectedYear.set(available[0] ?? current);
      }
    } catch {
      // The current year remains usable if the year lookup is temporarily unavailable.
    }
  }

  async loadData(): Promise<void> {
    this.isLoading.set(true);
    this.loadError.set(null);
    try {
      const result = await this.service.getList({
        year: this.selectedYear(),
        maxResultCount: 500,
        skipCount: 0,
        sorting: '',
      });
      this.budgets.set(result.items ?? []);
      this.expandedFinancialItemId.set(null);
    } catch {
      this.loadError.set(this.l.t('::Training.TrainingBudgets.LoadError'));
    } finally {
      this.isLoading.set(false);
    }
  }

  async onYearChange(year: number): Promise<void> {
    this.selectedYear.set(year);
    await this.loadData();
  }

  setFilter(filter: BudgetFilter): void {
    this.activeFilter.set(filter);
  }

  onSearchInput(event: Event): void {
    this.searchText.set((event.target as HTMLInputElement).value);
  }

  toggleDetails(financialItemId?: string): void {
    if (!financialItemId) return;
    this.expandedFinancialItemId.update(current =>
      current === financialItemId ? null : financialItemId,
    );
  }

  isExpanded(financialItemId?: string): boolean {
    return !!financialItemId && this.expandedFinancialItemId() === financialItemId;
  }

  onEditThreshold(item: TrainingBudgetDto): void {
    this.editingFinancialItemId.set(item.financialItemId ?? null);
    this.editingFinancialItemName.set(item.financialItemNameAr ?? '');
    this.thresholdValue.set(item.alertThreshold ?? 80);
    this.isThresholdDialogVisible.set(true);
  }

  async onSaveThreshold(): Promise<void> {
    const financialItemId = this.editingFinancialItemId();
    if (!financialItemId || this.isSaving()) return;

    this.isSaving.set(true);
    try {
      await this.service.setThreshold(financialItemId, this.selectedYear(), {
        alertThreshold: this.thresholdValue(),
      });
      this.isThresholdDialogVisible.set(false);
      await this.loadData();
    } finally {
      this.isSaving.set(false);
    }
  }

  progressWidth(item: TrainingBudgetDto): number {
    return Math.min(100, Math.max(0, item.spentPercent ?? 0));
  }

  formatCurrency(amount?: number | null): string {
    return (amount ?? 0).toLocaleString('en-US', {
      minimumFractionDigits: 3,
      maximumFractionDigits: 3,
    });
  }

  formatDate(value?: string | null): string {
    if (!value) return '—';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '—';
    return date.toLocaleDateString('en-GB');
  }

  itemTypeLabel(type?: FinancialItemType | null): string {
    switch (type) {
      case FinancialItemType.CourseCost:
        return this.l.t('::Training.FinancialItems.ItemType.CourseCost');
      case FinancialItemType.Ticket:
        return this.l.t('::Training.FinancialItems.ItemType.Ticket');
      case FinancialItemType.Insurance:
        return this.l.t('::Training.FinancialItems.ItemType.Insurance');
      case FinancialItemType.Visa:
        return this.l.t('::Training.FinancialItems.ItemType.Visa');
      case FinancialItemType.Allowance:
        return this.l.t('::Training.FinancialItems.ItemType.Allowance');
      case FinancialItemType.Clothing:
        return this.l.t('::Training.FinancialItems.ItemType.Clothing');
      default:
        return this.l.t('::Training.FinancialItems.ItemType.Other');
    }
  }

  activityLabel(activity: TrainingBudgetActivityDto): string {
    switch (activity.activityType) {
      case 'AnnualPlanAllocation':
        return this.l.t('::Training.TrainingBudgets.Activity.AnnualPlanAllocation');
      case 'CoursePayment':
        return this.l.t('::Training.TrainingBudgets.Activity.CoursePayment');
      case 'TravelPayment':
        return this.l.t('::Training.TrainingBudgets.Activity.TravelPayment');
      case 'CasualTravelExpense':
        return this.l.t('::Training.TrainingBudgets.Activity.CasualTravelExpense');
      default:
        return activity.activityType || '—';
    }
  }

  activityClass(activity: TrainingBudgetActivityDto): string {
    switch (activity.activityType) {
      case 'AnnualPlanAllocation': return 'allocation';
      case 'CasualTravelExpense': return 'casual';
      default: return 'spending';
    }
  }

  activityStatusLabel(activity: TrainingBudgetActivityDto): string {
    if (activity.statusCode === 'Settled') {
      return this.l.t('::Training.TrainingBudgets.Activity.Settled');
    }
    if (activity.statusCode === 'PendingRecovery') {
      return this.l.t('::Training.TrainingBudgets.Activity.PendingRecovery');
    }
    return this.l.t('::Training.TrainingBudgets.Activity.Confirmed');
  }

  textWithCount(key: string, count: number): string {
    return this.l.t(key, count);
  }

  trackBudget(_: number, item: TrainingBudgetDto): string {
    return item.financialItemId ?? '';
  }

  trackActivity(index: number, item: TrainingBudgetActivityDto): string {
    return `${item.activityType}-${item.sourceId}-${index}`;
  }
}
