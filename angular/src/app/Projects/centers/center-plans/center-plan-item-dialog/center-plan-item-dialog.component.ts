import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe } from '@abp/ng.core';
import {
  DxPopupModule,
  DxSelectBoxModule,
  DxDateBoxModule,
  DxNumberBoxModule,
  DxTextAreaModule,
  DxButtonModule,
  DxDropDownBoxModule,
  DxTreeViewModule,
} from 'devextreme-angular';
import { ToolbarItem } from 'devextreme/ui/popup';
import { CenterPlanItemService } from 'src/app/proxy/training/centers';
import { BeneficiaryType } from 'src/app/proxy/training/enums';
import {   TrainingLocalizationHelper } from 'src/app/Projects/shared';
 import { OrganizationUnitService } from '@volo/abp.ng.identity/proxy';
import { TenantCourseService } from 'src/app/proxy/training/tenant-courses';
@Component({
  selector: 'app-center-plan-item-dialog',
  standalone: true,
  imports: [
    CommonModule,
    LocalizationPipe,
    DxPopupModule,
    DxSelectBoxModule,
    DxDateBoxModule,
    DxNumberBoxModule,
    DxTextAreaModule,
    DxButtonModule,
    DxDropDownBoxModule,
    DxTreeViewModule,
  ],
  templateUrl: './center-plan-item-dialog.component.html',
  styleUrl: './center-plan-item-dialog.component.scss',
})
export class CenterPlanItemDialogComponent implements OnInit {
  private readonly itemService = inject(CenterPlanItemService);
  private readonly tenantCourseService = inject(TenantCourseService);
private readonly orgUnitService = inject(OrganizationUnitService);
  readonly l = inject(TrainingLocalizationHelper);

  readonly visible = input<boolean>(false);
  readonly planId = input.required<string>();
  readonly itemId = input<string | null>(null);
  readonly visibleChange = output<boolean>();
  readonly saved = output<void>();

  formData = signal({
    tenantCourseId: '' as string,
    beneficiaryType: BeneficiaryType.Internal,
    estimatedStartDate: null as Date | null,
    estimatedEndDate: null as Date | null,
    capacity: 0,
    durationWeeks: 0,
    objective: '',
  });

  tenantCourses = signal<any[]>([]);
  orgUnits = signal<any[]>([]);
  selectedUnitIds = signal<string[]>([]);
  conditions = signal<any[]>([]);

  beneficiaryTypeItems: any[] = [];
  dialogToolbarItems: ToolbarItem[] | undefined;

  get isInternal(): boolean {
    return this.formData().beneficiaryType === BeneficiaryType.Internal;
  }

  get dialogTitle(): string {
    return this.itemId()
      ? this.l.t('::Training.EditCourse')
      : this.l.t('::Training.AddCourseToPlan');
  }

  ngOnInit(): void {
    this.beneficiaryTypeItems = [
      { value: BeneficiaryType.Internal, text: this.l.t('::Training.BeneficiaryType.Internal') },
      { value: BeneficiaryType.Shared, text: this.l.t('::Training.BeneficiaryType.Shared') },
    ];

    this.dialogToolbarItems = [
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: { text: this.l.t('::Save'), type: 'default', stylingMode: 'contained', onClick: () => this.onSave() },
      },
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: { text: this.l.t('::Cancel'), onClick: () => this.close() },
      },
    ];

    this.loadTenantCourses();
    this.loadOrgUnits();

    if (this.itemId()) {
      this.loadExistingItem();
    }
  }

  // --- Form field update methods (called from template) ---

  updateBeneficiaryType(value: BeneficiaryType): void {
    this.formData.update(f => ({ ...f, beneficiaryType: value }));
  }

  updateCapacity(value: number): void {
    this.formData.update(f => ({ ...f, capacity: value }));
  }

  updateEstimatedStartDate(value: Date): void {
    this.formData.update(f => ({ ...f, estimatedStartDate: value }));
  }

  updateEstimatedEndDate(value: Date): void {
    this.formData.update(f => ({ ...f, estimatedEndDate: value }));
  }

  updateDurationWeeks(value: number): void {
    this.formData.update(f => ({ ...f, durationWeeks: value }));
  }

  updateObjective(value: string): void {
    this.formData.update(f => ({ ...f, objective: value }));
  }

  // --- Actions ---

  async onSave(): Promise<void> {
    const form = this.formData();
    const payload = {
      planId: this.planId(),
      tenantCourseId: form.tenantCourseId,
      beneficiaryType: form.beneficiaryType,
      estimatedStartDate: form.estimatedStartDate?.toISOString() ?? '',
      estimatedEndDate: form.estimatedEndDate?.toISOString() ?? '',
      capacity: form.capacity,
      durationWeeks: form.durationWeeks,
      objective: form.objective,
    };

    try {
      let savedItem: any;
      if (this.itemId()) {
        savedItem = await firstValueFrom(this.itemService.update(this.itemId()!, payload));
      } else {
        savedItem = await firstValueFrom(this.itemService.create(payload));
      }

      if (form.beneficiaryType === BeneficiaryType.Internal && this.selectedUnitIds().length > 0) {
        await firstValueFrom(this.itemService.setUnits(savedItem.id, { unitIds: this.selectedUnitIds() }));
      }

      this.saved.emit();
      this.close();
    } catch (e) { /* ABP interceptor */ }
  }

  onTenantCourseChanged(courseId: string): void {
    this.formData.update(f => ({ ...f, tenantCourseId: courseId }));
    this.loadConditions(courseId);
  }

  onUnitSelectionChanged(e: any): void {
    const selected = e.component.getSelectedNodeKeys();
    this.selectedUnitIds.set(selected);
  }

  close(): void {
    this.visibleChange.emit(false);
  }

  private async loadExistingItem(): Promise<void> {
    const item = await firstValueFrom(this.itemService.get(this.itemId()!));
    this.formData.set({
      tenantCourseId: item.tenantCourseId,
      beneficiaryType: item.beneficiaryType,
      estimatedStartDate: new Date(item.estimatedStartDate),
      estimatedEndDate: new Date(item.estimatedEndDate),
      capacity: item.capacity,
      durationWeeks: item.durationWeeks,
      objective: item.objective || '',
    });
    this.selectedUnitIds.set(item.unitIds || []);
    this.loadConditions(item.tenantCourseId);
  }

private async loadTenantCourses(): Promise<void> {
  const result = await firstValueFrom(
    this.tenantCourseService.getList({ isActive: true, maxResultCount: 1000, skipCount: 0 } as any)
  );
  console.log('Loaded tenant courses:', result.items);
  this.tenantCourses.set(result.items ?? []);
}

private async loadOrgUnits(): Promise<void> {
  const result = await firstValueFrom(this.orgUnitService.getList({ maxResultCount: 1000 }));
  this.orgUnits.set(result.items ?? []);
}

private async loadConditions(tenantCourseId: string): Promise<void> {
  var result = await firstValueFrom(this.tenantCourseService.getConditions(tenantCourseId));
  this.conditions.set(result ?? []);
}
}
