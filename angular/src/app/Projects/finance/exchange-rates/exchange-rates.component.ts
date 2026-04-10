import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { DxPopupModule } from 'devextreme-angular/ui/popup';
import { DxNumberBoxModule } from 'devextreme-angular/ui/number-box';
import { DxTextBoxModule } from 'devextreme-angular/ui/text-box';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { ToolbarItem } from 'devextreme/ui/popup';
import { ExchangeRateService } from '../../shared/services/finance-proxy.service';
import { ExchangeRateDto, CreateExchangeRateDto } from 'src/app/proxy/training/finance/dtos';
import { TrainingLocalizationHelper } from '../../shared';

@Component({
  selector: 'app-exchange-rates',
  standalone: true,
  imports: [
    CommonModule,
    LocalizationPipe,
    DxDataGridModule,
    DxPopupModule,
    DxNumberBoxModule,
    DxTextBoxModule,
    DxButtonModule,
  ],
  templateUrl: './exchange-rates.component.html',
  styleUrl: './exchange-rates.component.scss',
})
export class ExchangeRatesComponent implements OnInit {
  private readonly service = inject(ExchangeRateService);
  readonly l = inject(TrainingLocalizationHelper);

  rates = signal<ExchangeRateDto[]>([]);
  currentActiveRate = signal<number | null>(null);
  isDialogVisible = signal(false);

  formData: CreateExchangeRateDto = { rate: 0, notes: '' };
  dialogToolbarItems: ToolbarItem[] | undefined;

  ngOnInit(): void {
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
      maxResultCount: 100,
      skipCount: 0,
      sorting: '',
    });
    this.rates.set(result.items ?? []);

    const active = (result.items ?? []).find(x => x.isActive);
    this.currentActiveRate.set(active?.rate ?? null);
  }

  onAdd(): void {
    this.formData = { rate: 0, notes: '' };
    this.isDialogVisible.set(true);
  }

  async onSave(): Promise<void> {
    await this.service.create(this.formData);
    this.isDialogVisible.set(false);
    await this.loadData();
  }

  async onDelete(id: string): Promise<void> {
    await this.service.delete(id);
    await this.loadData();
  }
}
