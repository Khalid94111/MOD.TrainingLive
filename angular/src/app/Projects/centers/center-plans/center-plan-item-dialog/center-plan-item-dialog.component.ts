import { Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe, PermissionService, SessionStateService } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';
import { DxDropDownBoxModule, DxSelectBoxModule, DxTreeViewModule } from 'devextreme-angular';
import { CenterPlanItemService } from 'src/app/proxy/training/centers';
import { BeneficiaryType } from 'src/app/proxy/training/enums';
import { TenantCourseService } from 'src/app/proxy/training/tenant-courses';
import { OrganizationUnitService } from '@volo/abp.ng.identity/proxy';
import { TrainingLocalizationHelper } from 'src/app/Projects/shared';

interface TenantCourseOption {
  id: string;
  catalogCourseNameAr?: string;
  catalogCourseNameEn?: string;
  defaultCapacity?: number | null;
  displayName?: string;
}

interface OrgUnitNode {
  id: string;
  parentId?: string | null;
  displayName: string;
  selected?: boolean;
}

@Component({
  selector: 'app-center-plan-item-dialog',
  standalone: true,
  imports: [CommonModule, LocalizationPipe, DxDropDownBoxModule, DxSelectBoxModule, DxTreeViewModule],
  templateUrl: './center-plan-item-dialog.component.html',
  styleUrl: './center-plan-item-dialog.component.scss',
})
export class CenterPlanItemDialogComponent implements OnInit {
  private readonly itemService = inject(CenterPlanItemService);
  private readonly tenantCourseService = inject(TenantCourseService);
  private readonly orgUnitService = inject(OrganizationUnitService);
  private readonly permissionService = inject(PermissionService);
  private readonly sessionState = inject(SessionStateService);
  private readonly toaster = inject(ToasterService);
  private readonly l = inject(TrainingLocalizationHelper);

  readonly visible = input<boolean>(false);
  readonly planId = input.required<string>();
  readonly itemId = input<string | null>(null);
  readonly visibleChange = output<boolean>();
  readonly saved = output<void>();

  formData = signal({
    tenantCourseId: '',
    beneficiaryType: BeneficiaryType.Internal,
    estimatedStartDate: null as Date | string | null,
    estimatedEndDate: null as Date | string | null,
    capacity: 0,
    durationWeeks: 0,
    objective: '',
  });

  tenantCourses = signal<TenantCourseOption[]>([]);
  orgUnits = signal<OrgUnitNode[]>([]);
  selectedUnitIds = signal<string[]>([]);
  isOrgUnitDropDownOpen = signal(false);
  isSaving = signal(false);
  isLoading = signal(false);

  beneficiaryTypeItems: { value: BeneficiaryType; text: string }[] = [];

  currentLang = computed(() => this.sessionState.getLanguage() ?? 'en');

  courseOptions = computed(() => {
    const lang = this.currentLang();
    return this.tenantCourses().map(c => ({
      ...c,
      displayName: lang === 'ar' ? (c.catalogCourseNameAr ?? '') : (c.catalogCourseNameEn ?? ''),
    }));
  });

  canSetUnits = computed(() => this.permissionService.getGrantedPolicy('Training.CenterPlanItems.SetUnits'));

  get isInternal(): boolean {
    return this.formData().beneficiaryType === BeneficiaryType.Internal;
  }

  dialogTitle = computed(() =>
    this.itemId() ? this.l.t('::Training.EditCourse') : this.l.t('::Training.AddCourseToPlan')
  );

  async ngOnInit(): Promise<void> {
    this.beneficiaryTypeItems = [
      { value: BeneficiaryType.Internal, text: this.l.t('::Training.BeneficiaryType.Internal') },
      { value: BeneficiaryType.Shared, text: this.l.t('::Training.BeneficiaryType.Shared') },
    ];

    this.isLoading.set(true);
    try {
      await this.loadOrgUnits();
      if (this.itemId()) {
        await this.loadExistingItem();
      }
      await this.loadTenantCourses();
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
    } finally {
      this.isLoading.set(false);
    }
  }

  private async loadTenantCourses(): Promise<void> {
    const result = await firstValueFrom(
      this.tenantCourseService.getList({ isActive: true, maxResultCount: 1000, skipCount: 0 } as any)
    );
    this.tenantCourses.set((result.items ?? []) as TenantCourseOption[]);
  }

  private async loadOrgUnits(): Promise<void> {
    const result = await firstValueFrom(this.orgUnitService.getList({ maxResultCount: 1000 }));
    this.orgUnits.set((result.items ?? []).map((u: any) => ({ ...u, selected: false })) as OrgUnitNode[]);
  }

  private syncOrgUnitSelection(): void {
    const selectedIds = new Set(this.selectedUnitIds());
    this.orgUnits.update(list => list.map(u => ({ ...u, selected: selectedIds.has(u.id) })));
  }

  private async loadExistingItem(): Promise<void> {
    const item = await firstValueFrom(this.itemService.get(this.itemId()!));
    this.formData.set({
      tenantCourseId: item.tenantCourseId ?? '',
      beneficiaryType: item.beneficiaryType ?? BeneficiaryType.Internal,
      estimatedStartDate: item.estimatedStartDate ? new Date(item.estimatedStartDate) : null,
      estimatedEndDate: item.estimatedEndDate ? new Date(item.estimatedEndDate) : null,
      capacity: item.capacity ?? 0,
      durationWeeks: item.durationWeeks ?? 0,
      objective: item.objective || '',
    });
    this.recalcDurationWeeks();
    this.selectedUnitIds.set(item.unitIds || []);
    this.syncOrgUnitSelection();
  }

  // ── Form field update methods ──

  updateTenantCourseId(value: string): void {
    const course = this.tenantCourses().find(c => c.id === value);
    this.formData.update(f => ({
      ...f,
      tenantCourseId: value,
      capacity: f.capacity || course?.defaultCapacity || 0,
    }));
  }

  updateBeneficiaryType(value: BeneficiaryType): void {
    this.formData.update(f => ({ ...f, beneficiaryType: value, tenantCourseId: '' }));
    if (value !== BeneficiaryType.Internal) {
      this.selectedUnitIds.set([]);
      this.syncOrgUnitSelection();
    }
  }

  updateCapacity(value: number): void {
    this.formData.update(f => ({ ...f, capacity: value }));
  }

  updateEstimatedStartDate(value: string): void {
    this.formData.update(f => ({ ...f, estimatedStartDate: value ? new Date(value) : null }));
    this.recalcDurationWeeks();
  }

  updateEstimatedEndDate(value: string): void {
    this.formData.update(f => ({ ...f, estimatedEndDate: value ? new Date(value) : null }));
    this.recalcDurationWeeks();
  }

  private recalcDurationWeeks(): void {
    const start = this.formData().estimatedStartDate;
    const end = this.formData().estimatedEndDate;
    if (!start || !end) {
      this.formData.update(f => ({ ...f, durationWeeks: 0 }));
      return;
    }
    const startTime = new Date(start).getTime();
    const endTime = new Date(end).getTime();
    if (isNaN(startTime) || isNaN(endTime) || endTime <= startTime) {
      this.formData.update(f => ({ ...f, durationWeeks: 0 }));
      return;
    }
    const days = (endTime - startTime) / (1000 * 60 * 60 * 24);
    const weeks = Math.max(1, Math.ceil(days / 7));
    this.formData.update(f => ({ ...f, durationWeeks: weeks }));
  }

  updateObjective(value: string): void {
    this.formData.update(f => ({ ...f, objective: value }));
  }

  onUnitSelectionChanged(e: any): void {
    const selected = e.component.getSelectedNodeKeys();
    this.selectedUnitIds.set(selected);
    this.syncOrgUnitSelection();
  }

  // ── Actions ──

  close(): void {
    this.visibleChange.emit(false);
  }

  async onSave(): Promise<void> {
    if (!this.validateForm()) return;

    this.isSaving.set(true);
    try {
      const form = this.formData();
      const payload = {
        planId: this.planId(),
        tenantCourseId: form.tenantCourseId,
        beneficiaryType: form.beneficiaryType,
        estimatedStartDate: form.estimatedStartDate ? new Date(form.estimatedStartDate).toISOString() : '',
        estimatedEndDate: form.estimatedEndDate ? new Date(form.estimatedEndDate).toISOString() : '',
        capacity: form.capacity,
        durationWeeks: form.durationWeeks,
        objective: form.objective,
      };

      let savedItem: any;
      if (this.itemId()) {
        savedItem = await firstValueFrom(this.itemService.update(this.itemId()!, payload));
      } else {
        savedItem = await firstValueFrom(this.itemService.create(payload));
      }

      if (form.beneficiaryType === BeneficiaryType.Internal && this.canSetUnits()) {
        await firstValueFrom(this.itemService.setUnits(savedItem.id, { unitIds: this.selectedUnitIds() }));
      }

      this.toaster.success(this.l.t('::Training.Common.Save'));
      this.saved.emit();
      this.close();
    } catch {
      this.toaster.error(this.l.t('::Training.Errors.Generic'));
    } finally {
      this.isSaving.set(false);
    }
  }

  private validateForm(): boolean {
    const form = this.formData();
    if (!form.tenantCourseId) {
      this.toaster.warn(this.l.t('::Training.Validation.CourseRequired'));
      return false;
    }
    if (!form.estimatedStartDate) {
      this.toaster.warn(this.l.t('::Training.Validation.StartDateRequired'));
      return false;
    }
    if (!form.estimatedEndDate) {
      this.toaster.warn(this.l.t('::Training.Validation.EndDateRequired'));
      return false;
    }
    if (new Date(form.estimatedEndDate) <= new Date(form.estimatedStartDate)) {
      this.toaster.warn(this.l.t('::Training.Validation.EndDateAfterStartDate'));
      return false;
    }
    if (!form.capacity || form.capacity < 1) {
      this.toaster.warn(this.l.t('::Training.Validation.CapacityRequired'));
      return false;
    }
    return true;
  }
}
