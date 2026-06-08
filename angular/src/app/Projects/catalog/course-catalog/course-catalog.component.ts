import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { CourseCatalogService, CourseFieldService, TrainingLocalizationHelper } from '../../shared';
import type { CourseCatalogDto, CourseFieldDto, CreateUpdateCourseCatalogDto } from '../../shared';
import { CatalogFormDialogComponent } from './catalog-form-dialog/catalog-form-dialog.component';

@Component({
  selector: 'app-course-catalog',
  standalone: true,
  imports: [CommonModule, LocalizationPipe, CatalogFormDialogComponent],
  templateUrl: './course-catalog.component.html',
  styleUrl: './course-catalog.component.scss',
})
export class CourseCatalogComponent implements OnInit {
  private readonly catalogService = inject(CourseCatalogService);
  private readonly fieldService = inject(CourseFieldService);
  readonly l = inject(TrainingLocalizationHelper);

  courses = signal<CourseCatalogDto[]>([]);
  isLoading = signal(false);
  courseFields = signal<CourseFieldDto[]>([]);

  searchText = signal('');
  filterFieldId = signal<string | null>(null);
  filterCategory = signal<string | null>(null);
  filterStatus = signal<string | null>(null);

  isDialogVisible = signal(false);
  editingCourse = signal<CourseCatalogDto | null>(null);

  totalCount = computed(() => this.courses().length);
  activeCount = computed(() => this.courses().filter(c => c.isActive).length);
  inactiveCount = computed(() => this.courses().filter(c => !c.isActive).length);
  conditionsCount = computed(() => this.courses().reduce((sum, c) => sum + (c.conditionsCount || 0), 0));

  categoryOptions: { value: string; label: string }[] = [];
  statusOptions: { value: string; label: string }[] = [];

  async ngOnInit(): Promise<void> {
    this.categoryOptions = this.l.categoryDataSource().map(o => ({ value: o.value, label: o.text }));
    this.statusOptions = [
      { value: 'true', label: this.l.t('::Training.Active') },
      { value: 'false', label: this.l.t('::Training.Inactive') },
    ];
    await this.loadFields();
    await this.loadData();
  }

  async loadData(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await this.catalogService.getList({
        maxResultCount: 1000,
        skipCount: 0,
        sorting: '',
        filter: this.searchText() || undefined,
        fieldId: this.filterFieldId() || undefined,
        category: this.filterCategory() || undefined,
        isActive: this.filterStatus() === 'true' ? true : this.filterStatus() === 'false' ? false : undefined,
      });
      this.courses.set(result.items ?? []);
    } finally {
      this.isLoading.set(false);
    }
  }

  async loadFields(): Promise<void> {
    this.courseFields.set(await this.fieldService.getAllActive());
  }

  onSearch(): void { this.loadData(); }
  onFilterChange(): void { this.loadData(); }

  onAddCourse(): void {
    this.editingCourse.set(null);
    this.isDialogVisible.set(true);
  }

  onEditCourse(data: CourseCatalogDto): void {
    this.editingCourse.set(data);
    this.isDialogVisible.set(true);
  }

  onDialogSaved(): void {
    this.isDialogVisible.set(false);
    this.loadData();
  }

  onDialogCancelled(): void {
    this.isDialogVisible.set(false);
  }

  async onDeleteCourse(id: string): Promise<void> {
    if (!confirm(this.l.t('::Training.CourseCatalog.DeleteConfirm'))) return;
    await this.catalogService.delete(id);
    await this.loadData();
  }

  async onToggleActive(data: CourseCatalogDto): Promise<void> {
    const dto: CreateUpdateCourseCatalogDto = {
      courseNameAr: data.courseNameAr,
      courseNameEn: data.courseNameEn,
      descriptionAr: data.descriptionAr ?? '',
      descriptionEn: data.descriptionEn ?? '',
      category: data.category,
      nature: data.nature,
      fieldId: data.fieldId,
      resultType: data.resultType,
      requiresEvaluation: data.requiresEvaluation,
      requiresProviderEvaluation: data.requiresProviderEvaluation,
      hasCertificate: data.hasCertificate,
      evaluationBlocksCertificate: data.evaluationBlocksCertificate,
      isActive: !data.isActive,
    };
    await this.catalogService.update(data.id, dto);
    await this.loadData();
  }

  getResultTypeText(rowData: CourseCatalogDto): string {
    return this.l.resultType(rowData.resultType);
  }

  getCategoryText(rowData: CourseCatalogDto): string {
    return this.l.category(rowData.category);
  }

  getFieldName(id: string): string {
    const field = this.courseFields().find(f => f.id === id);
    return field?.fieldNameAr ?? '';
  }
}
