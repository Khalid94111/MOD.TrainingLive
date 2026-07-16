import { Component, computed, inject, input, OnInit, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe } from '@abp/ng.core';
import { TrainingProviderService } from 'src/app/proxy/training/finance';
import { TrainingProviderDto, CreateUpdateTrainingProviderDto } from 'src/app/proxy/training/finance/dtos';
import { ProviderScope } from 'src/app/proxy/training/enums/provider-scope.enum';
import { GeographicalLocationDto } from 'src/app/proxy/training/hr-integration/dtos/models';
import { TrainingLocalizationHelper } from '../../../shared';

@Component({
  selector: 'app-provider-form-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe],
  templateUrl: './provider-form-dialog.component.html',
  styleUrl: './provider-form-dialog.component.scss',
})
export class ProviderFormDialogComponent implements OnInit {
  private readonly providerService = inject(TrainingProviderService);
  readonly l = inject(TrainingLocalizationHelper);

  ProviderScope = ProviderScope;

  readonly visible = input.required<boolean>();
  readonly provider = input<TrainingProviderDto | null>(null);
  readonly countries = input<GeographicalLocationDto[]>([]);
  readonly saved = output<void>();
  readonly cancelled = output<void>();

  formData = signal<CreateUpdateTrainingProviderDto>({
    providerNameAr: '', providerNameEn: '',
    isApproved: false, isActive: true,
    scope: ProviderScope.Local,
    countryId: null,
  });

  isSaving = signal(false);
  validationError = signal<string | null>(null);
  countrySearch = signal('');           // typeahead text inside the country combobox
  countryDropdownOpen = signal(false);  // whether the combobox result list is visible

  scopeOptions = [
    { value: ProviderScope.Internal, key: '::Training.ProviderScope.Internal' },
    { value: ProviderScope.Local, key: '::Training.ProviderScope.Local' },
    { value: ProviderScope.International, key: '::Training.ProviderScope.International' },
  ];

  scopeDataSource = computed(() =>
    this.scopeOptions.map(o => ({ value: o.value, text: this.l.t(o.key) }))
  );

  isFromNebras = computed(() => !!this.provider()?.isFromNebras);
  countryRequired = computed(() => this.formData().scope !== ProviderScope.Internal);

  filteredCountries = computed(() => {
    const term = this.countrySearch().trim().toLowerCase();
    const list = this.countries();
    if (!term) return list;
    return list.filter(c =>
      (c.arabicName ?? '').toLowerCase().includes(term) ||
      (c.englishName ?? '').toLowerCase().includes(term)
    );
  });

  get isEditMode(): boolean { return !!this.provider(); }
  get dialogTitle(): string {
    return this.isEditMode ? this.l.t('::Training.TrainingProvider') : this.l.t('::Training.CreateProvider');
  }

  ngOnInit(): void {
    const p = this.provider();
    if (p) {
      this.formData.set({
        providerNameAr: p.providerNameAr ?? '',
        providerNameEn: p.providerNameEn ?? '',
        contactPerson: p.contactPerson,
        email: p.email,
        phone: p.phone,
        address: p.address,
        website: p.website,
        isApproved: p.isApproved ?? false,
        isActive: p.isActive ?? true,
        scope: p.scope ?? ProviderScope.Local,
        countryId: p.countryId ?? null,
      });
    }
  }

  updateField<K extends keyof CreateUpdateTrainingProviderDto>(key: K, value: CreateUpdateTrainingProviderDto[K]): void {
    this.formData.update(f => ({ ...f, [key]: value }));
  }

  updateScope(value: ProviderScope): void {
    this.formData.update(f => ({
      ...f,
      scope: value,
      countryId: value === ProviderScope.Internal ? null : f.countryId,
    }));
  }

  countryLabel(countryId: string | null | undefined): string {
    if (!countryId) return '';
    const c = this.countries().find(x => x.id === countryId);
    return c?.arabicName ?? c?.englishName ?? '';
  }

  // ── Country combobox (typeahead) ──
  selectedCountryName = computed<string>(() => this.countryLabel(this.formData().countryId));

  openCountryDropdown(): void {
    if (!this.countryRequired()) return;
    this.countrySearch.set('');
    this.countryDropdownOpen.set(true);
  }

  selectCountry(c: GeographicalLocationDto): void {
    this.updateField('countryId', c.id ?? null);
    this.countryDropdownOpen.set(false);
    this.countrySearch.set('');
  }

  closeCountryDropdown(): void {
    this.countryDropdownOpen.set(false);
    this.countrySearch.set('');
  }

  async onSave(): Promise<void> {
    const data = { ...this.formData() };

    if (data.scope !== ProviderScope.Internal && !data.countryId) {
      this.validationError.set(this.l.t('::Training:TrainingProvider:CountryRequiredForExternal'));
      return;
    }
    if (data.scope === ProviderScope.Internal) {
      data.countryId = null;
    }
    this.validationError.set(null);

    this.isSaving.set(true);
    try {
      if (this.isEditMode) {
        await firstValueFrom(this.providerService.update(this.provider()!.id, data));
      } else {
        await firstValueFrom(this.providerService.create(data));
      }
      this.saved.emit();
    } finally {
      this.isSaving.set(false);
    }
  }

  onCancel(): void { this.cancelled.emit(); }
}
