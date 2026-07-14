import { Component, inject, input, OnInit, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { CourseCatalogService, TrainingLocalizationHelper } from '../../../shared';
import type { CourseCatalogDto, CourseFieldDto, CreateUpdateCourseCatalogDto } from '../../../shared';
import { ResultType } from '../../../shared/models/training-enums';

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
  isSaving = signal(false);
  validationErrors = signal<string[]>([]);

  categoryOptions: { value: string; label: string }[] = [];
  natureOptions: { value: string; label: string }[] = [];
  resultTypeOptions: { value: number; label: string }[] = [];

  get isEditMode(): boolean { return !!this.course(); }
  get dialogTitle(): string { return this.isEditMode ? this.l.t('::Training.EditCourse') : this.l.t('::Training.AddNewCourse'); }
  get showEvaluationBlocks(): boolean { return this.formData().requiresEvaluation; }

  ngOnInit(): void {
    this.categoryOptions = this.l.categoryDataSource().map(o => ({ value: o.value, label: o.text }));
    this.natureOptions = this.l.natureDataSource().map(o => ({ value: o.value, label: o.text }));
    this.resultTypeOptions = this.l.resultTypeDataSource().map(o => ({ value: o.value, label: o.text }));

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
    }
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
        await this.catalogService.create(data);
      }
      this.saved.emit();
    } finally { this.isSaving.set(false); }
  }

  onCancel(): void { this.cancelled.emit(); }
}
