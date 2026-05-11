import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import {
  DxDataGridModule, DxPopupModule, DxTextBoxModule,
  DxCheckBoxModule, DxSwitchModule, DxSelectBoxModule,
  DxButtonModule,
} from 'devextreme-angular';
import { ToolbarItem } from 'devextreme/ui/popup';
import { TrainingProviderService } from 'src/app/proxy/training/finance';
import { TrainingProviderDto, CreateUpdateTrainingProviderDto } from 'src/app/proxy/training/finance/dtos';
import { ProviderScope } from 'src/app/proxy/training/enums/provider-scope.enum';
import { GeographicalLocationService } from 'src/app/proxy/training/hr-integration/geographical-location.service';
import { GeographicalLocationDto } from 'src/app/proxy/training/hr-integration/dtos/models';
import { TrainingLocalizationHelper } from '../../shared';

@Component({
  standalone: true,
  selector: 'app-provider-list',
  templateUrl: './provider-list.component.html',
  styleUrl: './provider-list.component.scss',
  imports: [
    CommonModule, LocalizationPipe,
    DxDataGridModule, DxPopupModule, DxTextBoxModule,
    DxCheckBoxModule, DxSwitchModule, DxSelectBoxModule, DxButtonModule,
  ],
})
export class ProviderListComponent implements OnInit {
  private providerService = inject(TrainingProviderService);
  private geoService = inject(GeographicalLocationService);
  private permissionService = inject(PermissionService);
  private l = inject(TrainingLocalizationHelper);

  ProviderScope = ProviderScope;

  providers = signal<TrainingProviderDto[]>([]);
  countries = signal<GeographicalLocationDto[]>([]);
  isDialogVisible = signal(false);
  isEditMode = signal(false);
  selectedId = signal<string | null>(null);
  editingProvider = signal<TrainingProviderDto | null>(null);
  validationError = signal<string | null>(null);

  formData = signal<CreateUpdateTrainingProviderDto>({
    providerNameAr: '', providerNameEn: '',
    isApproved: false, isActive: true,
    scope: ProviderScope.Local,
    countryId: null,
  });

  scopeOptions = [
    { value: ProviderScope.Internal,      key: '::Training.ProviderScope.Internal' },
    { value: ProviderScope.Local,         key: '::Training.ProviderScope.Local' },
    { value: ProviderScope.International, key: '::Training.ProviderScope.International' },
  ];

  scopeDataSource = computed(() =>
    this.scopeOptions.map(o => ({ value: o.value, text: this.l.t(o.key) }))
  );

  isFromNebras = computed(() => !!this.editingProvider()?.isFromNebras);
  countryRequired = computed(() => this.formData().scope !== ProviderScope.Internal);

  canCreate = false;
  dialogToolbarItems: ToolbarItem[] | undefined;

  get dialogTitle(): string {
    return this.isEditMode() ? this.l.t('::Training.TrainingProvider') : this.l.t('::Training.CreateProvider');
  }

  ngOnInit(): void {
    this.canCreate = this.permissionService.getGrantedPolicy('Training.TrainingProvider.Create');

    this.dialogToolbarItems = [
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Save'), type: 'default', onClick: () => this.onSave() },
      },
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Cancel'), onClick: () => this.isDialogVisible.set(false) },
      },
    ];

    this.loadProviders();
    this.loadCountries();
  }

  async loadProviders(): Promise<void> {
    const result = await firstValueFrom(this.providerService.getList({ maxResultCount: 200 }));
    this.providers.set(result.items ?? []);
  }

  async loadCountries(): Promise<void> {
    const result = await firstValueFrom(this.geoService.getCountries());
    this.countries.set(result.items ?? []);
  }

  scopeLabel(scope: ProviderScope | undefined): string {
    if (scope === undefined || scope === null) return '';
    const opt = this.scopeOptions.find(o => o.value === scope);
    return opt ? this.l.t(opt.key) : '';
  }

  scopeCssClass(scope: ProviderScope | undefined): string {
    switch (scope) {
      case ProviderScope.Internal:      return 'scope-chip scope-internal';
      case ProviderScope.Local:         return 'scope-chip scope-local';
      case ProviderScope.International: return 'scope-chip scope-international';
      default: return 'scope-chip';
    }
  }

  countryLabel(countryId: string | null | undefined): string {
    if (!countryId) return '—';
    const c = this.countries().find(x => x.id === countryId);
    return c?.arabicName ?? c?.englishName ?? '—';
  }

  onAdd(): void {
    this.isEditMode.set(false);
    this.selectedId.set(null);
    this.editingProvider.set(null);
    this.validationError.set(null);
    this.formData.set({
      providerNameAr: '', providerNameEn: '',
      isApproved: false, isActive: true,
      scope: ProviderScope.Local,
      countryId: null,
    });
    this.isDialogVisible.set(true);
  }

  onEdit(provider: TrainingProviderDto): void {
    this.isEditMode.set(true);
    this.selectedId.set(provider.id);
    this.editingProvider.set(provider);
    this.validationError.set(null);
    this.formData.set({
      providerNameAr: provider.providerNameAr ?? '',
      providerNameEn: provider.providerNameEn ?? '',
      contactPerson: provider.contactPerson,
      email: provider.email,
      phone: provider.phone,
      address: provider.address,
      website: provider.website,
      isApproved: provider.isApproved ?? false,
      isActive: provider.isActive ?? true,
      scope: provider.scope ?? ProviderScope.Local,
      countryId: provider.countryId ?? null,
    });
    this.isDialogVisible.set(true);
  }

  async onSave(): Promise<void> {
    const data = this.formData();

    if (data.scope !== ProviderScope.Internal && !data.countryId) {
      this.validationError.set(this.l.t('::Training:TrainingProvider:CountryRequiredForExternal'));
      return;
    }
    if (data.scope === ProviderScope.Internal) {
      data.countryId = null;
    }
    this.validationError.set(null);

    if (this.isEditMode() && this.selectedId()) {
      await firstValueFrom(this.providerService.update(this.selectedId()!, data));
    } else {
      await firstValueFrom(this.providerService.create(data));
    }
    this.isDialogVisible.set(false);
    await this.loadProviders();
  }

  async onDelete(id: string): Promise<void> {
    await firstValueFrom(this.providerService.delete(id));
    await this.loadProviders();
  }

  updateNameAr(value: string): void { this.formData.update(f => ({ ...f, providerNameAr: value })); }
  updateNameEn(value: string): void { this.formData.update(f => ({ ...f, providerNameEn: value })); }
  updateContactPerson(value: string): void { this.formData.update(f => ({ ...f, contactPerson: value })); }
  updateEmail(value: string): void { this.formData.update(f => ({ ...f, email: value })); }
  updatePhone(value: string): void { this.formData.update(f => ({ ...f, phone: value })); }
  updateWebsite(value: string): void { this.formData.update(f => ({ ...f, website: value })); }
  updateIsApproved(value: boolean): void { this.formData.update(f => ({ ...f, isApproved: value })); }
  updateIsActive(value: boolean): void { this.formData.update(f => ({ ...f, isActive: value })); }
  updateScope(value: ProviderScope): void {
    this.formData.update(f => ({
      ...f,
      scope: value,
      countryId: value === ProviderScope.Internal ? null : f.countryId,
    }));
  }
  updateCountryId(value: string | null): void {
    this.formData.update(f => ({ ...f, countryId: value }));
  }
}
