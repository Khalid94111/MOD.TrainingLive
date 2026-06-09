import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { CourseFieldService, TrainingLocalizationHelper } from '../../shared';
import type { CourseFieldDto, CreateUpdateCourseFieldDto } from '../../shared';
import { ConfirmationService, ToasterService } from '@abp/ng.theme.shared';

interface CardTheme {
  gradient: string;
  emoji: string;
}

const CARD_THEMES: CardTheme[] = [
  { gradient: 'linear-gradient(135deg, #4f46e5, #7c3aed)', emoji: '📊' },
  { gradient: 'linear-gradient(135deg, #1d4ed8, #2563eb)', emoji: '💻' },
  { gradient: 'linear-gradient(135deg, #059669, #10b981)', emoji: '💰' },
  { gradient: 'linear-gradient(135deg, #d97706, #f59e0b)', emoji: '⚖️' },
  { gradient: 'linear-gradient(135deg, #7c3aed, #a855f7)', emoji: '🗣️' },
  { gradient: 'linear-gradient(135deg, #be123c, #f43f5e)', emoji: '🔧' },
  { gradient: 'linear-gradient(135deg, #0f766e, #14b8a6)', emoji: '🛡️' },
  { gradient: 'linear-gradient(135deg, #0891b2, #06b6d4)', emoji: '🏥' },
];

@Component({
  selector: 'app-course-fields',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe],
  templateUrl: './course-fields.component.html',
  styleUrl: './course-fields.component.scss',
})
export class CourseFieldsComponent implements OnInit {
  private readonly fieldService = inject(CourseFieldService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly toaster = inject(ToasterService);
  readonly l = inject(TrainingLocalizationHelper);

  fields = signal<CourseFieldDto[]>([]);
  isLoading = signal(false);
  isDialogVisible = signal(false);
  editingField = signal<CourseFieldDto | null>(null);
  formData = signal<CreateUpdateCourseFieldDto>({ fieldNameAr: '', fieldNameEn: '', isActive: true });
  isSaving = signal(false);
  validationErrors = signal<string[]>([]);

  totalCount = computed(() => this.fields().length);
  activeCount = computed(() => this.fields().filter(f => f.isActive).length);
  inactiveCount = computed(() => this.fields().filter(f => !f.isActive).length);

  get isEditMode(): boolean { return !!this.editingField(); }
  get dialogTitle(): string { return this.isEditMode ? this.l.t('::Training.EditField') : this.l.t('::Training.AddField'); }

  ngOnInit(): void {
    this.loadFields();
  }

  async loadFields(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await this.fieldService.getList({ skipCount: 0, maxResultCount: 100 });
      this.fields.set(result.items ?? []);
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
    } finally {
      this.isLoading.set(false);
    }
  }

  getTheme(index: number): CardTheme {
    return CARD_THEMES[index % CARD_THEMES.length];
  }

  onAdd(): void {
    this.validationErrors.set([]);
    this.editingField.set(null);
    this.formData.set({ fieldNameAr: '', fieldNameEn: '', isActive: true });
    this.isDialogVisible.set(true);
  }

  onEdit(field: CourseFieldDto): void {
    this.validationErrors.set([]);
    this.editingField.set(field);
    this.formData.set({ fieldNameAr: field.fieldNameAr, fieldNameEn: field.fieldNameEn, isActive: field.isActive });
    this.isDialogVisible.set(true);
  }

  validateForm(): boolean {
    const errors: string[] = [];
    const data = this.formData();
    if (!data.fieldNameAr || data.fieldNameAr.trim().length < 2) {
      errors.push(this.l.t('::Training.CourseField.Validation.NameArRequired'));
    }
    if (!data.fieldNameEn || data.fieldNameEn.trim().length < 2) {
      errors.push(this.l.t('::Training.CourseField.Validation.NameEnRequired'));
    }
    this.validationErrors.set(errors);
    return errors.length === 0;
  }

  async onSave(): Promise<void> {
    if (!this.validateForm()) return;
    this.isSaving.set(true);
    try {
      if (this.isEditMode) {
        await this.fieldService.update(this.editingField()!.id, this.formData());
        this.toaster.success(this.l.t('::Training.CourseField.UpdateSuccess'));
      } else {
        await this.fieldService.create(this.formData());
        this.toaster.success(this.l.t('::Training.CourseField.CreateSuccess'));
      }
      this.isDialogVisible.set(false);
      await this.loadFields();
    } catch {
      this.toaster.error(this.l.t('::Training.Common.SaveError'));
    } finally {
      this.isSaving.set(false);
    }
  }

  onCancel(): void {
    this.isDialogVisible.set(false);
    this.validationErrors.set([]);
  }

  onDelete(field: CourseFieldDto): void {
    this.confirmation.warn(
      this.l.t('::Training.CourseField.DeleteConfirm'),
      this.l.t('::Training.ConfirmDelete')
    ).subscribe(async (status) => {
      if (status === 'confirm') {
        try {
          await this.fieldService.delete(field.id);
          this.toaster.success(this.l.t('::Training.CourseField.DeleteSuccess'));
          await this.loadFields();
        } catch {
          this.toaster.error(this.l.t('::Training.Common.DeleteError'));
        }
      }
    });
  }

  updateField<K extends keyof CreateUpdateCourseFieldDto>(key: K, value: CreateUpdateCourseFieldDto[K]): void {
    this.formData.update(f => ({ ...f, [key]: value }));
    if (this.validationErrors().length > 0) {
      this.validateForm();
    }
  }
}
