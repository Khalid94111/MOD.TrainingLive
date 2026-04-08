import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import {
  DxPopupModule,
  DxTextBoxModule,
  DxSwitchModule,
  DxButtonModule,
} from 'devextreme-angular';
import { CourseFieldService, TrainingLocalizationHelper } from '../../shared';
import type { CourseFieldDto, CreateUpdateCourseFieldDto } from '../../shared';

/** Color palette cycled per card — matches GTMS-Complete-UI-v2.html */
const CARD_THEMES = [
  { border: 'border-blue-200',   bg: 'bg-blue-50',   iconBg: 'bg-blue-500',   btnBorder: 'border-blue-300',   btnText: 'text-blue-600',   btnHover: 'hover:bg-blue-100',   emoji: '📊' },
  { border: 'border-indigo-200', bg: 'bg-indigo-50', iconBg: 'bg-indigo-500', btnBorder: 'border-indigo-300', btnText: 'text-indigo-600', btnHover: 'hover:bg-indigo-100', emoji: '💻' },
  { border: 'border-green-200',  bg: 'bg-green-50',  iconBg: 'bg-green-500',  btnBorder: 'border-green-300',  btnText: 'text-green-600',  btnHover: 'hover:bg-green-100',  emoji: '💰' },
  { border: 'border-amber-200',  bg: 'bg-amber-50',  iconBg: 'bg-amber-500',  btnBorder: 'border-amber-300',  btnText: 'text-amber-600',  btnHover: 'hover:bg-amber-100',  emoji: '⚖️' },
  { border: 'border-purple-200', bg: 'bg-purple-50', iconBg: 'bg-purple-500', btnBorder: 'border-purple-300', btnText: 'text-purple-600', btnHover: 'hover:bg-purple-100', emoji: '🗣️' },
  { border: 'border-rose-200',   bg: 'bg-rose-50',   iconBg: 'bg-rose-500',   btnBorder: 'border-rose-300',   btnText: 'text-rose-600',   btnHover: 'hover:bg-rose-100',   emoji: '🔧' },
  { border: 'border-teal-200',   bg: 'bg-teal-50',   iconBg: 'bg-teal-500',   btnBorder: 'border-teal-300',   btnText: 'text-teal-600',   btnHover: 'hover:bg-teal-100',   emoji: '🛡️' },
  { border: 'border-cyan-200',   bg: 'bg-cyan-50',   iconBg: 'bg-cyan-500',   btnBorder: 'border-cyan-300',   btnText: 'text-cyan-600',   btnHover: 'hover:bg-cyan-100',   emoji: '🏥' },
];

@Component({
  selector: 'app-course-fields',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LocalizationPipe,
    DxPopupModule,
    DxTextBoxModule,
    DxSwitchModule,
    DxButtonModule,
  ],
  templateUrl: './course-fields.component.html',
})
export class CourseFieldsComponent implements OnInit {
  private readonly fieldService = inject(CourseFieldService);
  readonly l = inject(TrainingLocalizationHelper);

  fields = signal<CourseFieldDto[]>([]);
  isLoading = signal(false);

  // Dialog
  isDialogVisible = signal(false);
  editingField = signal<CourseFieldDto | null>(null);
  formData = signal<CreateUpdateCourseFieldDto>({
    fieldNameAr: '',
    fieldNameEn: '',
    isActive: true,
  });
  isSaving = signal(false);
  popupToolbarItems: ({ widget: string; location: string; toolbar: string; options: { text: string; type: string; stylingMode: string; disabled: boolean; onClick: () => Promise<void>; }; } | { widget: string; location: string; toolbar: string; options: { text: string; stylingMode: string; onClick: () => void; type?: undefined; disabled?: undefined; }; })[] | undefined;

  get isEditMode(): boolean {
    return !!this.editingField();
  }

 
  ngOnInit(): void {
    this.popupToolbarItems = [
    {
      widget: 'dxButton',
      location: 'after',
      toolbar: 'bottom',
      options: {
        text: this.l.t('::Training.Save'),
        type: 'default',
        stylingMode: 'contained',
        disabled: this.isSaving(),
        onClick: () => this.onSave()
      }
    },
    {
      widget: 'dxButton',
      location: 'after',
      toolbar: 'bottom',
      options: {
        text: this.l.t('::Training.Cancel'),
        stylingMode: 'outlined',
        onClick: () => this.onCancel()
      }
    }
  ];;
    this.loadFields();
  }

  async loadFields(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await this.fieldService.getList({ skipCount: 0, maxResultCount: 100 });
      this.fields.set(result.items ?? []);
    } finally {
      this.isLoading.set(false);
    }
  }

  getTheme(index: number) {
    return CARD_THEMES[index % CARD_THEMES.length];
  }

  onAdd(): void {
    this.editingField.set(null);
    this.formData.set({ fieldNameAr: '', fieldNameEn: '', isActive: true });
    this.isDialogVisible.set(true);
  }

  onEdit(field: CourseFieldDto): void {
    this.editingField.set(field);
    this.formData.set({
      fieldNameAr: field.fieldNameAr,
      fieldNameEn: field.fieldNameEn,
      isActive: field.isActive,
    });
    this.isDialogVisible.set(true);
  }

  async onSave(): Promise<void> {
    this.isSaving.set(true);
    try {
      if (this.isEditMode) {
        await this.fieldService.update(this.editingField()!.id, this.formData());
      } else {
        await this.fieldService.create(this.formData());
      }
      this.isDialogVisible.set(false);
      await this.loadFields();
    } finally {
      this.isSaving.set(false);
    }
  }

  onCancel(): void {
    this.isDialogVisible.set(false);
  }

  async onDelete(field: CourseFieldDto): Promise<void> {
    await this.fieldService.delete(field.id);
    await this.loadFields();
  }

  updateField<K extends keyof CreateUpdateCourseFieldDto>(
    key: K,
    value: CreateUpdateCourseFieldDto[K],
  ): void {
    this.formData.update(f => ({ ...f, [key]: value }));
  }
}
