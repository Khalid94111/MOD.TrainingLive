import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import { TrainingProviderService } from 'src/app/proxy/training/finance';
import { TrainingProviderDto } from 'src/app/proxy/training/finance/dtos';
import { ProviderScope } from 'src/app/proxy/training/enums/provider-scope.enum';
import { GeographicalLocationService } from 'src/app/proxy/training/hr-integration/geographical-location.service';
import { GeographicalLocationDto } from 'src/app/proxy/training/hr-integration/dtos/models';
import { TrainingLocalizationHelper, ConfirmDialogComponent } from '../../shared';
import { ProviderFormDialogComponent } from './provider-form-dialog/provider-form-dialog.component';

@Component({
  standalone: true,
  selector: 'app-provider-list',
  templateUrl: './provider-list.component.html',
  styleUrl: './provider-list.component.scss',
  imports: [CommonModule, LocalizationPipe, ProviderFormDialogComponent, ConfirmDialogComponent],
})
export class ProviderListComponent implements OnInit {
  private providerService = inject(TrainingProviderService);
  private geoService = inject(GeographicalLocationService);
  private permissionService = inject(PermissionService);
  readonly l = inject(TrainingLocalizationHelper);

  ProviderScope = ProviderScope;

  providers = signal<TrainingProviderDto[]>([]);
  countries = signal<GeographicalLocationDto[]>([]);
  isLoading = signal(false);

  isDialogVisible = signal(false);
  editingProvider = signal<TrainingProviderDto | null>(null);

  // Delete confirmation modal
  confirmDeleteOpen = signal(false);
  deleteTarget = signal<TrainingProviderDto | null>(null);

  canCreate = false;

  totalCount = computed(() => this.providers().length);
  activeCount = computed(() => this.providers().filter(p => p.isActive).length);
  nebrasCount = computed(() => this.providers().filter(p => p.isFromNebras).length);

  scopeOptions = [
    { value: ProviderScope.Internal, key: '::Training.ProviderScope.Internal' },
    { value: ProviderScope.Local, key: '::Training.ProviderScope.Local' },
    { value: ProviderScope.International, key: '::Training.ProviderScope.International' },
  ];

  ngOnInit(): void {
    this.canCreate = this.permissionService.getGrantedPolicy('Training.TrainingProvider.Create');
    this.loadProviders();
    this.loadCountries();
  }

  async loadProviders(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await firstValueFrom(this.providerService.getList({ maxResultCount: 200 }));
      this.providers.set(result.items ?? []);
    } finally {
      this.isLoading.set(false);
    }
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
      case ProviderScope.Internal: return 'scope-chip scope-internal';
      case ProviderScope.Local: return 'scope-chip scope-local';
      case ProviderScope.International: return 'scope-chip scope-international';
      default: return 'scope-chip';
    }
  }

  onAdd(): void {
    this.editingProvider.set(null);
    this.isDialogVisible.set(true);
  }

  onEdit(provider: TrainingProviderDto): void {
    this.editingProvider.set(provider);
    this.isDialogVisible.set(true);
  }

  onDialogSaved(): void {
    this.isDialogVisible.set(false);
    this.loadProviders();
  }

  onDialogCancelled(): void {
    this.isDialogVisible.set(false);
  }

  onDelete(provider: TrainingProviderDto): void {
    this.deleteTarget.set(provider);
    this.confirmDeleteOpen.set(true);
  }

  async onConfirmDelete(): Promise<void> {
    const target = this.deleteTarget();
    if (!target?.id) { this.onCancelDelete(); return; }
    await firstValueFrom(this.providerService.delete(target.id));
    this.confirmDeleteOpen.set(false);
    this.deleteTarget.set(null);
    await this.loadProviders();
  }

  onCancelDelete(): void {
    this.confirmDeleteOpen.set(false);
    this.deleteTarget.set(null);
  }
}
