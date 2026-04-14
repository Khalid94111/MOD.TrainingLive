import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { DxPopupModule } from 'devextreme-angular/ui/popup';
import { DxNumberBoxModule } from 'devextreme-angular/ui/number-box';
import { DxSelectBoxModule } from 'devextreme-angular/ui/select-box';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { ToolbarItem } from 'devextreme/ui/popup';
import { TrainingBudgetService } from '../../shared/services/finance-proxy.service';
import { TrainingLocalizationHelper } from '../../shared';
import { TrainingBudgetDto } from 'src/app/proxy/training/finance/dtos';
import { LocalizationPipe } from '@abp/ng.core';

interface BudgetCardPalette {
  colorClass: string;
  barGradient: string;
  borderColor: string;
  bgGradient: string;
  badgeBg: string;
  badgeColor: string;
  spentColor: string;
}

@Component({
  selector: 'app-training-budgets',
  standalone: true,
  imports: [
    CommonModule,
    LocalizationPipe,
    DxDataGridModule,
    DxPopupModule,
    DxNumberBoxModule,
    DxSelectBoxModule,
    DxButtonModule,
  ],
  templateUrl: './training-budgets.component.html',
  styleUrl: './training-budgets.component.scss',
})
export class TrainingBudgetsComponent implements OnInit {
  private readonly service = inject(TrainingBudgetService);
  readonly l = inject(TrainingLocalizationHelper);

  budgets = signal<TrainingBudgetDto[]>([]);
  activeBudgets = computed(() =>
    this.budgets().filter(b => b.isFinancialItemActive)
  );
  visibleBudgets = computed(() =>
    this.budgets().filter(b => b.isFinancialItemActive || (b.totalAmount ?? 0) > 0)
  );
  selectedYear = signal<number>(new Date().getFullYear());
  isThresholdDialogVisible = signal(false);

  editingId: string | null = null;
  thresholdValue = signal<number>(80);

  dialogToolbarItems: ToolbarItem[] | undefined;
  yearOptions: number[] = [];

  private readonly palettes: BudgetCardPalette[] = [
    {
      colorClass: 'blue',
      barGradient: 'linear-gradient(90deg, #3b82f6, #2563eb)',
      borderColor: '#bfdbfe',
      bgGradient: 'linear-gradient(135deg, #eff6ff, #dbeafe)',
      badgeBg: '#bfdbfe',
      badgeColor: '#1e40af',
      spentColor: '#2563eb',
    },
    {
      colorClass: 'green',
      barGradient: 'linear-gradient(90deg, #22c55e, #16a34a)',
      borderColor: '#bbf7d0',
      bgGradient: 'linear-gradient(135deg, #f0fdf4, #dcfce7)',
      badgeBg: '#bbf7d0',
      badgeColor: '#15803d',
      spentColor: '#16a34a',
    },
    {
      colorClass: 'purple',
      barGradient: 'linear-gradient(90deg, #a855f7, #9333ea)',
      borderColor: '#e9d5ff',
      bgGradient: 'linear-gradient(135deg, #faf5ff, #f3e8ff)',
      badgeBg: '#e9d5ff',
      badgeColor: '#7e22ce',
      spentColor: '#9333ea',
    },
    {
      colorClass: 'red',
      barGradient: 'linear-gradient(90deg, #ef4444, #dc2626)',
      borderColor: '#fecaca',
      bgGradient: 'linear-gradient(135deg, #fef2f2, #fee2e2)',
      badgeBg: '#fecaca',
      badgeColor: '#b91c1c',
      spentColor: '#dc2626',
    },
    {
      colorClass: 'amber',
      barGradient: 'linear-gradient(90deg, #f59e0b, #d97706)',
      borderColor: '#fde68a',
      bgGradient: 'linear-gradient(135deg, #fffbeb, #fef3c7)',
      badgeBg: '#fde68a',
      badgeColor: '#b45309',
      spentColor: '#d97706',
    },
    {
      colorClass: 'teal',
      barGradient: 'linear-gradient(90deg, #14b8a6, #0d9488)',
      borderColor: '#99f6e4',
      bgGradient: 'linear-gradient(135deg, #f0fdfa, #ccfbf1)',
      badgeBg: '#99f6e4',
      badgeColor: '#0f766e',
      spentColor: '#0d9488',
    },
  ];

  async ngOnInit(): Promise<void> {
    const currentYear = new Date().getFullYear();
    this.yearOptions = [currentYear + 1, currentYear, currentYear - 1, currentYear - 2];

    this.dialogToolbarItems = [
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: {
          text: this.l.t('::Training.Common.Save'),
          type: 'default',
          stylingMode: 'contained',
          onClick: () => this.onSaveThreshold(),
        },
      },
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: {
          text: this.l.t('::Training.Common.Cancel'),
          onClick: () => this.isThresholdDialogVisible.set(false),
        },
      },
    ];

    await this.loadData();
  }

  async loadData(): Promise<void> {
    const result = await this.service.getList({
      year: this.selectedYear(),
      maxResultCount: 100,
      skipCount: 0,
      sorting: '',
    });
    this.budgets.set(result.items ?? []);
  }

  paletteFor(index: number): BudgetCardPalette {
    return this.palettes[index % this.palettes.length];
  }

  onYearChange(year: number): void {
    this.selectedYear.set(year);
    this.loadData();
  }

  onEditThreshold(item: TrainingBudgetDto): void {
    this.editingId = item.id ?? null;
    this.thresholdValue.set(item.alertThreshold ?? 80);
    this.isThresholdDialogVisible.set(true);
  }

  async onSaveThreshold(): Promise<void> {
    if (!this.editingId) return;
    await this.service.updateThreshold(this.editingId, {
      alertThreshold: this.thresholdValue(),
    });
    this.isThresholdDialogVisible.set(false);
    await this.loadData();
  }

  formatCurrency(amount: number): string {
    return amount.toLocaleString('en-US', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    });
  }
}
