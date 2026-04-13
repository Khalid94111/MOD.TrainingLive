import { Component, OnInit, signal, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import {
  DxDataGridModule, DxPopupModule, DxSelectBoxModule,
  DxNumberBoxModule, DxTextBoxModule, DxTextAreaModule, DxDateBoxModule,DxButtonModule
} from 'devextreme-angular';
import { ToolbarItem } from 'devextreme/ui/popup';
import { ActivatedRoute } from '@angular/router';
import { PreferredQuarter } from 'src/app/proxy/training/enums';
import { TrainingPlanService, TrainingPlanItemService } from 'src/app/proxy/training/plans';
import { TenantCourseService } from 'src/app/proxy/training/tenant-courses';
import { TrainingPlanDto, TrainingPlanItemDto, CreateUpdateTrainingPlanItemDto } from 'src/app/proxy/training/plans/dtos';
import { PlanStatus } from '../../shared';
import { TrainingLocalizationHelper } from '../../shared';

@Component({
  standalone: true,
  selector: 'app-plan-entry',
  templateUrl: './plan-entry.component.html',
  styleUrl: './plan-entry.component.scss',
  imports: [
    CommonModule, LocalizationPipe,
    DxDataGridModule, DxPopupModule, DxSelectBoxModule,
    DxNumberBoxModule, DxTextBoxModule, DxTextAreaModule, DxDateBoxModule,DxButtonModule
  ],
})
export class PlanEntryComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private planService = inject(TrainingPlanService);
  private planItemService = inject(TrainingPlanItemService);
  private tenantCourseService = inject(TenantCourseService);
  private l = inject(TrainingLocalizationHelper);

  readonly PlanStatus = PlanStatus;


  planId = '';
  plan = signal<TrainingPlanDto | null>(null);
  items = signal<TrainingPlanItemDto[]>([]);
  isDialogVisible = signal(false);
  isEditMode = signal(false);
  selectedItemId = signal<string | null>(null);

  tenantCourses = signal<any[]>([]);

  courseTypes = [
    { value: 0, text: '' },
    { value: 1, text: '' },
    { value: 2, text: '' },
  ];

  quarters = [
    { value: PreferredQuarter.Q1, text: '' },
    { value: PreferredQuarter.Q2, text: '' },
    { value: PreferredQuarter.Q3, text: '' },
    { value: PreferredQuarter.Q4, text: '' },
  ];

  formData = signal<CreateUpdateTrainingPlanItemDto>({
    planId: '',
    tenantCourseId: '',
    courseType: 0,
    preferredQuarter: PreferredQuarter.Q1,
    priority: 1,
    officersCount: 0,
    enlistedCount: 0,
    justification: '',
  });

  dialogToolbarItems: ToolbarItem[] | undefined;

  get isWindowOpen(): boolean {
    const p = this.plan();
    return p?.status === PlanStatus.Open;
  }

  get dialogTitle(): string {
    return this.isEditMode() ? this.l.t('::Training.TrainingPlanItem') : this.l.t('::Training.AddPlanItem');
  }

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('planId') ?? '';

    this.courseTypes = [
      { value: 0, text: this.l.t('::Training.CourseType.Internal') },
      { value: 1, text: this.l.t('::Training.CourseType.ExternalLocal') },
      { value: 2, text: this.l.t('::Training.CourseType.ExternalInternational') },
    ];

    this.quarters = [
      { value: PreferredQuarter.Q1, text: this.l.t('::Training.PreferredQuarter.Q1') },
      { value: PreferredQuarter.Q2, text: this.l.t('::Training.PreferredQuarter.Q2') },
      { value: PreferredQuarter.Q3, text: this.l.t('::Training.PreferredQuarter.Q3') },
      { value: PreferredQuarter.Q4, text: this.l.t('::Training.PreferredQuarter.Q4') },
    ];

    this.dialogToolbarItems = [
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Save'), type: 'default', onClick: () => this.onSaveItem() },
      },
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Cancel'), onClick: () => this.isDialogVisible.set(false) },
      },
    ];

    this.loadPlan();
    this.loadItems();
    this.loadTenantCourses();
  }

  async loadPlan(): Promise<void> {
    const result = await firstValueFrom(this.planService.get(this.planId));
    this.plan.set(result);
  }

  async loadItems(): Promise<void> {
    const result = await firstValueFrom(
      this.planItemService.getList({ planId: this.planId, maxResultCount: 200 })
    );
    this.items.set(result.items ?? []);
  }

  async loadTenantCourses(): Promise<void> {
    const result = await firstValueFrom(
      this.tenantCourseService.getList({ isActive: true, maxResultCount: 1000, skipCount: 0 } as any)
    );
    this.tenantCourses.set(result.items ?? []);
  }


  onAddItem(): void {
    this.isEditMode.set(false);
    this.selectedItemId.set(null);
    this.formData.set({
      planId: this.planId,
      tenantCourseId: '',
      courseType: 0,
      preferredQuarter: PreferredQuarter.Q1,
      priority: 1,
      officersCount: 0,
      enlistedCount: 0,
      justification: '',
    });
    this.isDialogVisible.set(true);
  }

  onEditItem(item: TrainingPlanItemDto): void {
    this.isEditMode.set(true);
    this.selectedItemId.set(item.id);
    this.formData.set({
      planId: item.planId,
      tenantCourseId: item.tenantCourseId,
      courseType: item.courseType,
      preferredQuarter: item.preferredQuarter,
      priority: item.priority,
      officersCount: item.officersCount,
      enlistedCount: item.enlistedCount,
      justification: item.justification,
      descriptionAr: item.descriptionAr,
      descriptionEn: item.descriptionEn,
      objectivesAr: item.objectivesAr,
      objectivesEn: item.objectivesEn,
      durationYears: item.durationYears,
      durationMonths: item.durationMonths,
      durationDays: item.durationDays,
      estimatedDateFrom: item.estimatedDateFrom,
      estimatedDateTo: item.estimatedDateTo,
      fundingSource: item.fundingSource,
    });
    this.isDialogVisible.set(true);
  }

  async onSaveItem(): Promise<void> {
    const data = this.formData();
    if (this.isEditMode() && this.selectedItemId()) {
      await firstValueFrom(this.planItemService.update(this.selectedItemId()!, data));
    } else {
      await firstValueFrom(this.planItemService.create(data));
    }
    this.isDialogVisible.set(false);
    await this.loadItems();
  }

  async onDeleteItem(id: string): Promise<void> {
    await firstValueFrom(this.planItemService.delete(id));
    await this.loadItems();
  }

  async onSubmitItems(): Promise<void> {
    // This triggers the plan to move from Open → Submitted conceptually
    // The actual status change happens on the backend when Staff closes the window
    // UTM just submits their items — no status change at this level
  }

  getDurationText(item: TrainingPlanItemDto): string {
    const years = item.durationYears ?? 0;
    const months = item.durationMonths ?? 0;
    const days = item.durationDays ?? 0;
    const parts: string[] = [];
    const yearShort = this.l.t('::Training.DurationYears');
    const monthShort = this.l.t('::Training.DurationMonths');
    const dayShort = this.l.t('::Training.DurationDays');

    if (years > 0) parts.push(`${years} ${yearShort}`);
    if (months > 0) parts.push(`${months} ${monthShort}`);
    if (days > 0) parts.push(`${days} ${dayShort}`);

    return parts.length > 0 ? parts.join(' ') : `0 ${dayShort}`;
  }

  // Form field update methods — no arrow functions in templates
  updateTenantCourseId(value: string): void { this.formData.update(f => ({ ...f, tenantCourseId: value })); }
  updateCourseType(value: number): void { this.formData.update(f => ({ ...f, courseType: value })); }
  updatePreferredQuarter(value: number): void { this.formData.update(f => ({ ...f, preferredQuarter: value })); }
  updatePriority(value: number): void { this.formData.update(f => ({ ...f, priority: value })); }
  updateOfficersCount(value: number): void { this.formData.update(f => ({ ...f, officersCount: value })); }
  updateEnlistedCount(value: number): void { this.formData.update(f => ({ ...f, enlistedCount: value })); }
  updateJustification(value: string): void { this.formData.update(f => ({ ...f, justification: value })); }
  updateDescriptionAr(value: string): void { this.formData.update(f => ({ ...f, descriptionAr: value })); }
  updateDescriptionEn(value: string): void { this.formData.update(f => ({ ...f, descriptionEn: value })); }
  updateObjectivesAr(value: string): void { this.formData.update(f => ({ ...f, objectivesAr: value })); }
  updateObjectivesEn(value: string): void { this.formData.update(f => ({ ...f, objectivesEn: value })); }
  updateDurationYears(value: number): void { this.formData.update(f => ({ ...f, durationYears: value })); }
  updateDurationMonths(value: number): void { this.formData.update(f => ({ ...f, durationMonths: value })); }
  updateDurationDays(value: number): void { this.formData.update(f => ({ ...f, durationDays: value })); }
  updateEstimatedDateFrom(value: string): void { this.formData.update(f => ({ ...f, estimatedDateFrom: value })); }
  updateEstimatedDateTo(value: string): void { this.formData.update(f => ({ ...f, estimatedDateTo: value })); }
  updateFundingSource(value: string): void { this.formData.update(f => ({ ...f, fundingSource: value })); }
}
