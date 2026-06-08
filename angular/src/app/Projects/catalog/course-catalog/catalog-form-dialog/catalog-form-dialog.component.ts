import { Component, inject, input, OnInit, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { CourseCatalogService, TrainingLocalizationHelper } from '../../../shared';
import type { CourseCatalogDto, CourseFieldDto, CreateUpdateCourseCatalogDto, CatalogEnrollmentConditionDto } from '../../../shared';
import { ConditionType, ResultType } from '../../../shared/models/training-enums';

@Component({
  selector: 'app-catalog-form-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe],
  templateUrl: './catalog-form-dialog.component.html',
  styleUrl: './catalog-form-dialog.component.scss',
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
    courseNameAr: '', courseNameEn: '', descriptionAr: '', descriptionEn: '',
    category: '', nature: '', fieldId: undefined as any, resultType: ResultType.AttendanceOnly,
    requiresEvaluation: false, requiresProviderEvaluation: false,
    hasCertificate: false, evaluationBlocksCertificate: false, isActive: true,
  });
  conditions = signal<(CatalogEnrollmentConditionDto & { _isNew?: boolean })[]>([]);
  isSaving = signal(false);
  validationErrors = signal<string[]>([]);

  categoryOptions: { value: string; label: string }[] = [];
  natureOptions: { value: string; label: string }[] = [];
  resultTypeOptions: { value: number; label: string }[] = [];
  conditionTypeOptions: { value: number; label: string }[] = [];

  get isEditMode(): boolean { return !!this.course(); }
  get dialogTitle(): string { return this.isEditMode ? this.l.t('::Training.EditCourse') : this.l.t('::Training.AddNewCourse'); }
  get showEvaluationBlocks(): boolean { return this.formData().requiresEvaluation; }

  ngOnInit(): void {
    this.categoryOptions = this.l.categoryDataSource().map(o => ({ value: o.value, label: o.text }));
    this.natureOptions = this.l.natureDataSource().map(o => ({ value: o.value, label: o.text }));
    this.resultTypeOptions = this.l.resultTypeDataSource().map(o => ({ value: o.value, label: o.text }));
    this.conditionTypeOptions = this.l.conditionTypeDataSource().map(o => ({ value: o.value, label: o.text }));

    const c = this.course();
    if (c) {
      this.formData.set({
        courseNameAr: c.courseNameAr, courseNameEn: c.courseNameEn,
        descriptionAr: c.descriptionAr ?? '', descriptionEn: c.descriptionEn ?? '',
        category: c.category, nature: c.nature, fieldId: c.fieldId,
        resultType: c.resultType, requiresEvaluation: c.requiresEvaluation,
        requiresProviderEvaluation: c.requiresProviderEvaluation,
        hasCertificate: c.hasCertificate, evaluationBlocksCertificate: c.evaluationBlocksCertificate,
        isActive: c.isActive,
      });
      this.loadConditions(c.id);
    }
  }

  private async loadConditions(id: string): Promise<void> {
    this.conditions.set(await this.catalogService.getConditions(id));
  }

  updateField<K extends keyof CreateUpdateCourseCatalogDto>(key: K, value: CreateUpdateCourseCatalogDto[K]): void {
    this.formData.update(f => ({ ...f, [key]: value }));
  }

  private validateForm(): boolean {
    const errors: string[] = [];
    const data = this.formData();
    if (!data.courseNameAr?.trim()) errors.push(this.l.t('::Training.Validation.CourseNameArRequired'));
    if (!data.courseNameEn?.trim()) errors.push(this.l.t('::Training.Validation.CourseNameEnRequired'));
    if (!data.category) errors.push(this.l.t('::Training.Validation.CategoryRequired'));
    if (!data.nature) errors.push(this.l.t('::Training.Validation.NatureRequired'));
    if (!data.fieldId) errors.push(this.l.t('::Training.Validation.FieldRequired'));
    if (data.resultType === undefined || data.resultType === null) errors.push(this.l.t('::Training.Validation.ResultTypeRequired'));
    this.validationErrors.set(errors);
    return errors.length === 0;
  }

  async onSave(): Promise<void> {
    if (!this.validateForm()) return;
    this.isSaving.set(true);
    try {
      const data = this.formData();
      if (this.isEditMode) {
        await this.catalogService.update(this.course()!.id, data);
      } else {
        const created = await this.catalogService.create(data);
        for (const cond of this.conditions()) {
          if (cond._isNew) await this.catalogService.addCondition(created.id, { conditionType: cond.conditionType, conditionValue: cond.conditionValue });
        }
      }
      this.saved.emit();
    } finally { this.isSaving.set(false); }
  }

  onCancel(): void { this.cancelled.emit(); }

  onAddCondition(): void {
    this.conditions.update(list => [...list, {
      id: crypto.randomUUID(), catalogCourseId: this.course()?.id ?? '',
      conditionType: ConditionType.Rank, conditionValue: '', _isNew: true,
    }]);
  }

  async onRemoveCondition(index: number): Promise<void> {
    const cond = this.conditions()[index];
    if (!cond._isNew && cond.id) await this.catalogService.removeCondition(cond.id);
    this.conditions.update(list => list.filter((_, i) => i !== index));
  }

  async onSaveCondition(cond: CatalogEnrollmentConditionDto & { _isNew?: boolean }): Promise<void> {
    if (this.isEditMode && cond._isNew && this.course()?.id) {
      const saved = await this.catalogService.addCondition(this.course()!.id, { conditionType: cond.conditionType, conditionValue: cond.conditionValue });
      this.conditions.update(list => list.map(c => (c.id === cond.id ? { ...saved, _isNew: false } : c)));
    }
  }

  updateCondition(index: number, field: string, value: any): void {
    this.conditions.update(list => list.map((c, i) => (i === index ? { ...c, [field]: value } : c)));
  }
}
