import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { DxPopupModule, DxTextBoxModule, DxSwitchModule, DxButtonModule } from 'devextreme-angular';
import { CourseFieldService, TrainingLocalizationHelper } from '../../shared';
import type { CourseFieldDto, CreateUpdateCourseFieldDto } from '../../shared';

const CARD_THEMES = [
  { css: 'field-card--blue',   emoji: '📊' },
  { css: 'field-card--indigo', emoji: '💻' },
  { css: 'field-card--green',  emoji: '💰' },
  { css: 'field-card--amber',  emoji: '⚖️' },
  { css: 'field-card--purple', emoji: '🗣️' },
  { css: 'field-card--rose',   emoji: '🔧' },
  { css: 'field-card--teal',   emoji: '🛡️' },
  { css: 'field-card--cyan',   emoji: '🏥' },
];

@Component({
  selector: 'app-course-fields',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe, DxPopupModule, DxTextBoxModule, DxSwitchModule, DxButtonModule],
  templateUrl: './course-fields.component.html',
  styleUrl: './course-fields.component.scss',
})
export class CourseFieldsComponent implements OnInit {
  private readonly fieldService = inject(CourseFieldService);
  readonly l = inject(TrainingLocalizationHelper);

  fields = signal<CourseFieldDto[]>([]);
  isDialogVisible = signal(false);
  editingField = signal<CourseFieldDto | null>(null);
  formData = signal<CreateUpdateCourseFieldDto>({ fieldNameAr: '', fieldNameEn: '', isActive: true });
  isSaving = signal(false);

  dialogToolbarItems: any[] = [];

  get isEditMode(): boolean { return !!this.editingField(); }
  get dialogTitle(): string { return this.isEditMode ? this.l.t('::Training.EditField') : this.l.t('::Training.AddField'); }

  ngOnInit(): void {
    this.dialogToolbarItems = [
      { widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Training.Save'), icon: 'save', type: 'default', stylingMode: 'contained', onClick: () => this.onSave() } },
      { widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Training.Cancel'), stylingMode: 'outlined', onClick: () => this.onCancel() } },
    ];
    this.loadFields();
  }

  async loadFields(): Promise<void> {
    const result = await this.fieldService.getList({ skipCount: 0, maxResultCount: 100 });
    this.fields.set(result.items ?? []);
  }

  getTheme(index: number) { return CARD_THEMES[index % CARD_THEMES.length]; }

  onAdd(): void {
    this.editingField.set(null);
    this.formData.set({ fieldNameAr: '', fieldNameEn: '', isActive: true });
    this.isDialogVisible.set(true);
  }

  onEdit(field: CourseFieldDto): void {
    this.editingField.set(field);
    this.formData.set({ fieldNameAr: field.fieldNameAr, fieldNameEn: field.fieldNameEn, isActive: field.isActive });
    this.isDialogVisible.set(true);
  }

  async onSave(): Promise<void> {
    this.isSaving.set(true);
    try {
      if (this.isEditMode) await this.fieldService.update(this.editingField()!.id, this.formData());
      else await this.fieldService.create(this.formData());
      this.isDialogVisible.set(false);
      await this.loadFields();
    } finally { this.isSaving.set(false); }
  }

  onCancel(): void { this.isDialogVisible.set(false); }

  async onDelete(field: CourseFieldDto): Promise<void> {
    await this.fieldService.delete(field.id);
    await this.loadFields();
  }

  updateField<K extends keyof CreateUpdateCourseFieldDto>(key: K, value: CreateUpdateCourseFieldDto[K]): void {
    this.formData.update(f => ({ ...f, [key]: value }));
  }
}
