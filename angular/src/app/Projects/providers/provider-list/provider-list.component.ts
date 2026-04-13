import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import {
  DxDataGridModule, DxPopupModule, DxTextBoxModule,
  DxCheckBoxModule, DxSwitchModule,
  DxButtonModule,
} from 'devextreme-angular';
import { ToolbarItem } from 'devextreme/ui/popup';
import { TrainingProviderService } from 'src/app/proxy/training/finance';
import { TrainingProviderDto, CreateUpdateTrainingProviderDto } from 'src/app/proxy/training/finance/dtos';
import { TrainingLocalizationHelper } from '../../shared';
 
@Component({
  standalone: true,
  selector: 'app-provider-list',
  templateUrl: './provider-list.component.html',
  styleUrl: './provider-list.component.scss',
  imports: [CommonModule, LocalizationPipe, DxDataGridModule, DxPopupModule, DxTextBoxModule, DxCheckBoxModule, DxSwitchModule, DxButtonModule],
})
export class ProviderListComponent implements OnInit {
  private providerService = inject(TrainingProviderService);
  private permissionService = inject(PermissionService);
    private l = inject(TrainingLocalizationHelper);


  providers = signal<TrainingProviderDto[]>([]);
  isDialogVisible = signal(false);
  isEditMode = signal(false);
  selectedId = signal<string | null>(null);

  formData = signal<CreateUpdateTrainingProviderDto>({
    providerNameAr: '', providerNameEn: '',
    isApproved: false, isActive: true,
  });

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
  }

  async loadProviders(): Promise<void> {
    const result = await firstValueFrom(this.providerService.getList({ maxResultCount: 200 }));
    this.providers.set(result.items ?? []);
  }

  onAdd(): void {
    this.isEditMode.set(false);
    this.selectedId.set(null);
    this.formData.set({ providerNameAr: '', providerNameEn: '', isApproved: false, isActive: true });
    this.isDialogVisible.set(true);
  }

  onEdit(provider: TrainingProviderDto): void {
    this.isEditMode.set(true);
    this.selectedId.set(provider.id);
    this.formData.set({
      providerNameAr: provider.providerNameAr,
      providerNameEn: provider.providerNameEn,
      contactPerson: provider.contactPerson,
      email: provider.email,
      phone: provider.phone,
      address: provider.address,
      website: provider.website,
      isApproved: provider.isApproved,
      isActive: provider.isActive,
    });
    this.isDialogVisible.set(true);
  }

  async onSave(): Promise<void> {
    const data = this.formData();
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
}
