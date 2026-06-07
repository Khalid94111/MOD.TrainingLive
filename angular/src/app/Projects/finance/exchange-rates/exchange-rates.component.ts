import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { ExchangeRateService } from '../../shared/services/finance-proxy.service';
import { ExchangeRateDto, CreateExchangeRateDto } from 'src/app/proxy/training/finance/dtos';
import { TrainingLocalizationHelper } from '../../shared';

@Component({
  selector: 'app-exchange-rates',
  standalone: true,
  imports: [CommonModule, LocalizationPipe],
  templateUrl: './exchange-rates.component.html',
  styleUrl: './exchange-rates.component.scss',
})
export class ExchangeRatesComponent implements OnInit {
  private readonly service = inject(ExchangeRateService);
  readonly l = inject(TrainingLocalizationHelper);

  rates = signal<ExchangeRateDto[]>([]);
  activeRate = computed(() => this.rates().find(x => x.isActive) ?? null);
  currentActiveRate = signal<number | null>(null);
  isDialogVisible = signal(false);
  isLoading = signal(false);
  rateError = signal<string | null>(null);

  totalCount = computed(() => this.rates().length);
  activeCount = computed(() => this.rates().filter(x => x.isActive).length);
  inactiveCount = computed(() => this.rates().filter(x => !x.isActive).length);

  formData: CreateExchangeRateDto = { rate: undefined, notes: '' };

  ngOnInit(): void {
    this.loadData();
  }

  async loadData(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await this.service.getList({
        maxResultCount: 100,
        skipCount: 0,
        sorting: '',
      });
      this.rates.set(result.items ?? []);

      const active = (result.items ?? []).find(x => x.isActive);
      this.currentActiveRate.set(active?.rate ?? null);
    } finally {
      this.isLoading.set(false);
    }
  }

  onAdd(): void {
    this.formData = { rate: undefined, notes: '' };
    this.rateError.set(null);
    this.isDialogVisible.set(true);
  }

  async onSave(): Promise<void> {
    this.rateError.set(null);

    if (!this.formData.rate || this.formData.rate <= 0) {
      this.rateError.set(this.l.t('::Training.ExchangeRates.RateMustBePositive'));
      return;
    }

    // Round to 3 decimal places
    this.formData.rate = Math.round(this.formData.rate * 1000) / 1000;

    if (!confirm(this.l.t('::Training.ExchangeRates.ConfirmCreate'))) {
      return;
    }

    await this.service.create(this.formData);
    this.isDialogVisible.set(false);
    await this.loadData();
  }

  async onDelete(id: string): Promise<void> {
    if (!confirm(this.l.t('::Training.ExchangeRates.DeleteConfirm'))) return;
    await this.service.delete(id);
    await this.loadData();
  }

  onRateInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    let value = input.value;

    if (value === '') {
      this.formData.rate = undefined;
      return;
    }

    // Allow only digits and one decimal point
    const parts = value.split('.');
    if (parts.length > 2) {
      value = parts[0] + '.' + parts.slice(1).join('');
    }

    // Restrict to 3 decimal places
    if (parts.length === 2 && parts[1].length > 3) {
      value = parts[0] + '.' + parts[1].substring(0, 3);
    }

    // Prevent negative
    const num = parseFloat(value);
    if (num < 0) {
      value = '0';
    }

    this.formData.rate = parseFloat(value);
    input.value = value;
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    // Use Arabic text with Latin (English) numerals
    return d.toLocaleDateString('ar-SA', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      numberingSystem: 'latn',
    });
  }
}
