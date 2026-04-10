import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { DxPopupModule } from 'devextreme-angular/ui/popup';
import { DxNumberBoxModule } from 'devextreme-angular/ui/number-box';
import { DxSelectBoxModule } from 'devextreme-angular/ui/select-box';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { ToolbarItem } from 'devextreme/ui/popup';
import { TrainingBudgetService } from '../../shared/services/finance-proxy.service';
import { TrainingLocalizationHelper } from '../../shared';
import { CreateUpdateTrainingBudgetDto, TrainingBudgetDto } from 'src/app/proxy/training/finance/dtos';
import { BudgetType } from 'src/app/proxy/training/enums';

interface BudgetCard {
  budgetType: BudgetType;
  labelAr: string;
  labelEn: string;
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
  selectedYear = signal<number>(new Date().getFullYear());
  isDialogVisible = signal(false);
  isEditMode = signal(false);

  editingId: string | null = null;
  formData: CreateUpdateTrainingBudgetDto = this.getEmptyForm();

  dialogToolbarItems: ToolbarItem[] | undefined;
  yearOptions: number[] = [];

  budgetCardConfigs: BudgetCard[] = [
    {
      budgetType: BudgetType.Internal,
      labelAr: 'التدريب الداخلي',
      labelEn: 'Internal Training',
      colorClass: 'blue',
      barGradient: 'linear-gradient(90deg, #3b82f6, #2563eb)',
      borderColor: '#bfdbfe',
      bgGradient: 'linear-gradient(135deg, #eff6ff, #dbeafe)',
      badgeBg: '#bfdbfe',
      badgeColor: '#1e40af',
      spentColor: '#2563eb',
    },
    {
      budgetType: BudgetType.ExternalInternational,
      labelAr: 'التدريب الخارجي الدولي',
      labelEn: 'International Training',
      colorClass: 'green',
      barGradient: 'linear-gradient(90deg, #22c55e, #16a34a)',
      borderColor: '#bbf7d0',
      bgGradient: 'linear-gradient(135deg, #f0fdf4, #dcfce7)',
      badgeBg: '#bbf7d0',
      badgeColor: '#15803d',
      spentColor: '#16a34a',
    },
    {
      budgetType: BudgetType.Planning,
      labelAr: 'فريق التخطيط',
      labelEn: 'Planning Team',
      colorClass: 'purple',
      barGradient: 'linear-gradient(90deg, #a855f7, #9333ea)',
      borderColor: '#e9d5ff',
      bgGradient: 'linear-gradient(135deg, #faf5ff, #f3e8ff)',
      badgeBg: '#e9d5ff',
      badgeColor: '#7e22ce',
      spentColor: '#9333ea',
    },
    {
      budgetType: BudgetType.HigherEducation,
      labelAr: 'الدراسات العليا',
      labelEn: 'Higher Education',
      colorClass: 'red',
      barGradient: 'linear-gradient(90deg, #ef4444, #dc2626)',
      borderColor: '#fecaca',
      bgGradient: 'linear-gradient(135deg, #fef2f2, #fee2e2)',
      badgeBg: '#fecaca',
      badgeColor: '#b91c1c',
      spentColor: '#dc2626',
    },
  ];

  budgetTypeOptions: { value: BudgetType; text: string }[] = [];

  get dialogTitle(): string {
    return this.isEditMode()
      ? this.l.t('::Training.TrainingBudgets.Edit')
      : this.l.t('::Training.TrainingBudgets.Add');
  }

  ngOnInit(): void {
    const currentYear = new Date().getFullYear();
    this.yearOptions = [currentYear + 1, currentYear, currentYear - 1, currentYear - 2];

    this.budgetTypeOptions = [
      { value: BudgetType.Internal, text: this.l.t('::Training.BudgetType.Internal') },
      { value: BudgetType.ExternalInternational, text: this.l.t('::Training.BudgetType.ExternalInternational') },
      { value: BudgetType.Planning, text: this.l.t('::Training.BudgetType.Planning') },
      { value: BudgetType.HigherEducation, text: this.l.t('::Training.BudgetType.HigherEducation') },
    ];

    this.dialogToolbarItems = [
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: {
          text: this.l.t('::Training.Common.Save'),
          type: 'default',
          stylingMode: 'contained',
          onClick: () => this.onSave(),
        },
      },
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: {
          text: this.l.t('::Training.Common.Cancel'),
          onClick: () => this.isDialogVisible.set(false),
        },
      },
    ];

    this.loadData();
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

  getBudgetForType(type: BudgetType): TrainingBudgetDto | undefined {
    return this.budgets().find(b => b.budgetType === type);
  }

  onYearChange(year: number): void {
    this.selectedYear.set(year);
    this.loadData();
  }

  onAdd(): void {
    this.isEditMode.set(false);
    this.editingId = null;
    this.formData = this.getEmptyForm();
    this.formData.year = this.selectedYear();
    this.isDialogVisible.set(true);
  }

  onEdit(item: TrainingBudgetDto): void {
    this.isEditMode.set(true);
    this.editingId = item.id;
    this.formData = {
      year: item.year,
      budgetType: item.budgetType,
      totalAmount: item.totalAmount,
      spentAmount: item.spentAmount,
      alertThreshold: item.alertThreshold,
    };
    this.isDialogVisible.set(true);
  }

  async onSave(): Promise<void> {
    if (this.isEditMode() && this.editingId) {
      await this.service.update(this.editingId, this.formData);
    } else {
      await this.service.create(this.formData);
    }
    this.isDialogVisible.set(false);
    await this.loadData();
  }

  async onDelete(id: string): Promise<void> {
    await this.service.delete(id);
    await this.loadData();
  }

  getBudgetTypeKey(type: BudgetType): string {
    const map: Record<number, string> = {
      [BudgetType.Internal]: 'Internal',
      [BudgetType.ExternalInternational]: 'ExternalInternational',
      [BudgetType.Planning]: 'Planning',
      [BudgetType.HigherEducation]: 'HigherEducation',
    };
    return map[type] ?? 'Internal';
  }

  formatCurrency(amount: number): string {
    return amount.toLocaleString('en-US', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    });
  }

  private getEmptyForm(): CreateUpdateTrainingBudgetDto {
    return {
      year: new Date().getFullYear(),
      budgetType: BudgetType.Internal,
      totalAmount: 0,
      spentAmount: 0,
      alertThreshold: 80,
    };
  }
}
