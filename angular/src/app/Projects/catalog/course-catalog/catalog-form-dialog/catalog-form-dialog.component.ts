import { Component, inject, input, OnInit, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import {
  DxPopupModule,
  DxTextBoxModule,
  DxTextAreaModule,
  DxSelectBoxModule,
  DxCheckBoxModule,
  DxSwitchModule,
  DxButtonModule,
} from 'devextreme-angular';
import { CourseCatalogService, TrainingLocalizationHelper } from '../../../shared';
import type {
  CourseCatalogDto,
  CourseFieldDto,
  CreateUpdateCourseCatalogDto,
  CatalogEnrollmentConditionDto,
} from '../../../shared';
import { ConditionType, ResultType } from '../../../shared/models/training-enums';

@Component({
  selector: 'app-catalog-form-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule, LocalizationPipe,
    DxPopupModule, DxTextBoxModule, DxTextAreaModule,
    DxSelectBoxModule, DxCheckBoxModule, DxSwitchModule, DxButtonModule,
  ],
  templateUrl: './catalog-form-dialog.component.html',
})
export class CatalogFormDialogComponent implements OnInit {
  private readonly catalogService = inject(CourseCatalogService);
  readonly l = inject(TrainingLocalizationHelper);

  readonly visible = input.required<boolean>();
  readonly course = input<CourseCatalogDto | null>(null);
  readonly courseFields = input<CourseFieldDto[]>([]);

  readonly saved = output<void>();
  readonly cancelled = output<void>();

  formData = signal<CreateUpdateCourseCatalogDto>({
    courseNameAr: '', courseNameEn: '',
    descriptionAr: '', descriptionEn: '',
    category: '', nature: '', fieldId: '',
    resultType: ResultType.AttendanceOnly,
    requiresEvaluation: false, requiresProviderEvaluation: false,
    hasCertificate: false, evaluationBlocksCertificate: false,
    isActive: true,
  });

  conditions = signal<(CatalogEnrollmentConditionDto & { _isNew?: boolean })[]>([]);
  isSaving = signal(false);

  categoryDataSource: any[] = [];
  natureDataSource: any[] = [];
  resultTypeDataSource: any[] = [];
  conditionTypeDataSource: any[] = [];

  get isEditMode(): boolean { return !!this.course(); }
  get dialogTitle(): string { return this.l.t(this.isEditMode ? '::Training.EditCourse' : '::Training.AddNewCourse'); }
  get showEvaluationBlocks(): boolean { return this.formData().requiresEvaluation; }

  ngOnInit(): void {
    this.categoryDataSource = this.l.categoryDataSource();
    this.natureDataSource = this.l.natureDataSource();
    this.resultTypeDataSource = this.l.resultTypeDataSource();
    this.conditionTypeDataSource = this.l.conditionTypeDataSource();

    const c = this.course();
    if (c) {
      this.formData.set({
        courseNameAr: c.courseNameAr, courseNameEn: c.courseNameEn,
        descriptionAr: c.descriptionAr ?? '', descriptionEn: c.descriptionEn ?? '',
        category: c.category, nature: c.nature, fieldId: c.fieldId,
        resultType: c.resultType,
        requiresEvaluation: c.requiresEvaluation,
        requiresProviderEvaluation: c.requiresProviderEvaluation,
        hasCertificate: c.hasCertificate,
        evaluationBlocksCertificate: c.evaluationBlocksCertificate,
        isActive: c.isActive,
      });
      this.loadConditions(c.id);
    }
  }

  private async loadConditions(catalogCourseId: string): Promise<void> {
    const conds = await this.catalogService.getConditions(catalogCourseId);
    this.conditions.set(conds);
  }

  updateField<K extends keyof CreateUpdateCourseCatalogDto>(key: K, value: CreateUpdateCourseCatalogDto[K]): void {
    this.formData.update(f => ({ ...f, [key]: value }));
  }

  async onSave(): Promise<void> {
    this.isSaving.set(true);
    try {
      const data = this.formData();
      if (this.isEditMode) {
        await this.catalogService.update(this.course()!.id, data);
      } else {
        const created = await this.catalogService.create(data);
        for (const cond of this.conditions()) {
          if (cond._isNew) {
            await this.catalogService.addCondition(created.id, {
              conditionType: cond.conditionType,
              conditionValue: cond.conditionValue,
            });
          }
        }
      }
      this.saved.emit();
    } finally {
      this.isSaving.set(false);
    }
  }

  onCancel(): void { this.cancelled.emit(); }

  onAddCondition(): void {
    this.conditions.update(list => [...list, {
      id: crypto.randomUUID(),
      catalogCourseId: this.course()?.id ?? '',
      conditionType: ConditionType.Rank,
      conditionValue: '',
      _isNew: true,
    }]);
  }

  async onRemoveCondition(index: number): Promise<void> {
    const cond = this.conditions()[index];
    if (!cond._isNew && cond.id) await this.catalogService.removeCondition(cond.id);
    this.conditions.update(list => list.filter((_, i) => i !== index));
  }

  async onSaveCondition(cond: CatalogEnrollmentConditionDto & { _isNew?: boolean }): Promise<void> {
    if (this.isEditMode && cond._isNew && this.course()?.id) {
      const saved = await this.catalogService.addCondition(this.course()!.id, {
        conditionType: cond.conditionType, conditionValue: cond.conditionValue,
      });
      this.conditions.update(list => list.map(c => (c.id === cond.id ? { ...saved, _isNew: false } : c)));
    }
  }

  updateCondition(index: number, field: string, value: any): void {
    this.conditions.update(list => list.map((c, i) => (i === index ? { ...c, [field]: value } : c)));
  }
}
