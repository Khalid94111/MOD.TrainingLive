import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import {  PermissionService } from '@abp/ng.core';
import { TrainingProviderService } from 'src/app/proxy/training/finance';
import { TrainingProviderDto, CreateUpdateTrainingProviderDto } from 'src/app/proxy/training/finance/dtos';
import { TrainingLocalizationHelper } from '../shared';

@Component({
  standalone: true,
  selector: 'app-provider-list',
  templateUrl: './provider-list.component.html',
  styleUrls: ['./provider-list.component.scss', '../shared/gtms-design.scss'],
  imports: [CommonModule, ],
})
export class ProviderListComponent implements OnInit {
  private providerService = inject(TrainingProviderService);
  private permissionService = inject(PermissionService);
  l = inject(TrainingLocalizationHelper);

  providers = signal<TrainingProviderDto[]>([]);
  isDialogOpen = signal(false);
  isEditMode = signal(false);
  editId = signal<string | null>(null);

  fNameAr = signal('');
  fNameEn = signal('');
  fContact = signal('');
  fEmail = signal('');
  fPhone = signal('');
  fWebsite = signal('');
  fIsApproved = signal(false);
  fIsActive = signal(true);

  canCreate = false;

  get dialogTitle(): string {
    return this.isEditMode() ? '✏️ تعديل جهة تدريب' : '➕ إضافة جهة تدريب';
  }

  ngOnInit(): void {
    this.canCreate = this.permissionService.getGrantedPolicy('Training.TrainingProvider.Create');
    this.loadProviders();
  }

  async loadProviders(): Promise<void> {
    const r = await firstValueFrom(this.providerService.getList({ maxResultCount: 200 }));
    this.providers.set(r.items ?? []);
  }

  openAddDialog(): void {
    this.isEditMode.set(false);
    this.editId.set(null);
    this.fNameAr.set(''); this.fNameEn.set('');
    this.fContact.set(''); this.fEmail.set(''); this.fPhone.set(''); this.fWebsite.set('');
    this.fIsApproved.set(false); this.fIsActive.set(true);
    this.isDialogOpen.set(true);
  }

  openEditDialog(p: TrainingProviderDto): void {
    this.isEditMode.set(true);
    this.editId.set(p.id);
    this.fNameAr.set(p.providerNameAr); this.fNameEn.set(p.providerNameEn);
    this.fContact.set(p.contactPerson ?? ''); this.fEmail.set(p.email ?? '');
    this.fPhone.set(p.phone ?? ''); this.fWebsite.set(p.website ?? '');
    this.fIsApproved.set(p.isApproved); this.fIsActive.set(p.isActive);
    this.isDialogOpen.set(true);
  }

  async onSave(): Promise<void> {
    const data: CreateUpdateTrainingProviderDto = {
      providerNameAr: this.fNameAr(),
      providerNameEn: this.fNameEn(),
      contactPerson: this.fContact() || undefined,
      email: this.fEmail() || undefined,
      phone: this.fPhone() || undefined,
      website: this.fWebsite() || undefined,
      isApproved: this.fIsApproved(),
      isActive: this.fIsActive(),
    };

    if (this.isEditMode() && this.editId()) {
      await firstValueFrom(this.providerService.update(this.editId()!, data));
    } else {
      await firstValueFrom(this.providerService.create(data));
    }
    this.isDialogOpen.set(false);
    await this.loadProviders();
  }

  async onDelete(id: string): Promise<void> {
    if (!confirm('هل أنت متأكد؟')) return;
    await firstValueFrom(this.providerService.delete(id));
    await this.loadProviders();
  }

  getRatingStars(rating: number): string {
    return '⭐'.repeat(Math.round(rating));
  }
}
