import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { TenantCourseService, CourseFieldService, TrainingLocalizationHelper } from '../../shared';
import type { TenantCourseDto, UpdateTenantCourseDto } from '../../shared';
import { ResultType } from '../../shared/models/training-enums';
import { ConfirmationService, ToasterService } from '@abp/ng.theme.shared';
import { AddFromCatalogDialogComponent } from '../add-from-catalog-dialog/add-from-catalog-dialog.component';

@Component({
  selector: 'app-tenant-courses-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe, AddFromCatalogDialogComponent],
  templateUrl: './tenant-courses-list.component.html',
  styleUrl: './tenant-courses-list.component.scss',
})
export class TenantCoursesListComponent implements OnInit {
  private readonly tenantCourseService = inject(TenantCourseService);
  private readonly fieldService = inject(CourseFieldService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly toaster = inject(ToasterService);
  readonly l = inject(TrainingLocalizationHelper);

  readonly ResultType = ResultType;

  tenantCourses = signal<TenantCourseDto[]>([]);
  courseFields = signal<{ id: string; fieldNameAr: string }[]>([]);
  isLoading = signal(false);
  searchText = signal('');
  filterFieldId = signal<string | null>(null);
  filterResultType = signal<ResultType | null>(null);
  filterStatus = signal<boolean | null>(null);

  totalCount = computed(() => this.tenantCourses().length);
  activeCount = computed(() => this.tenantCourses().filter(c => c.isActive).length);
  withCertificateCount = computed(() => this.tenantCourses().filter(c => c.hasCertificate).length);

  // Edit modal
  isEditDialogVisible = signal(false);
  editingCourse = signal<TenantCourseDto | null>(null);
  editForm = signal<UpdateTenantCourseDto>({
    defaultCapacity: undefined, defaultDurationWeeks: undefined,
    resultType: ResultType.AttendanceOnly, requiresEvaluation: false,
    requiresProviderEvaluation: false, hasCertificate: false,
    evaluationBlocksCertificate: false, isActive: true,
  });
  isSaving = signal(false);
  editValidationErrors = signal<string[]>([]);

  // Add modal
  isAddDialogVisible = signal(false);

  resultTypeOptions = computed(() => this.l.resultTypeDataSource());
  statusFilterOptions = computed(() => [
    { value: null, text: this.l.t('::Training.AllStatuses') },
    { value: true, text: this.l.t('::Training.Active') },
    { value: false, text: this.l.t('::Training.Inactive') },
  ]);

  get showEvaluationBlocks(): boolean { return this.editForm().requiresEvaluation; }

  ngOnInit(): void {
    this.loadFields();
    this.loadCourses();
  }

  async loadCourses(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await this.tenantCourseService.getList({
        skipCount: 0, maxResultCount: 1000,
        filter: this.searchText() || undefined,
        fieldId: this.filterFieldId() || undefined,
        resultType: this.filterResultType() ?? undefined,
        isActive: this.filterStatus() ?? undefined,
      });
      this.tenantCourses.set(result.items ?? []);
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
    } finally {
      this.isLoading.set(false);
    }
  }

  private async loadFields(): Promise<void> {
    try {
      const fields = await this.fieldService.getAllActive();
      this.courseFields.set(fields.map(f => ({ id: f.id, fieldNameAr: f.fieldNameAr ?? '' })));
    } catch { /* silently fail */ }
  }

  onSearch(): void { this.loadCourses(); }
  onFilterChange(): void { this.loadCourses(); }
  onOpenAddDialog(): void { this.isAddDialogVisible.set(true); }
  onCoursesAdded(): void { this.isAddDialogVisible.set(false); this.loadCourses(); }

  async onEditCourse(course: TenantCourseDto): Promise<void> {
    this.editValidationErrors.set([]);
    this.editingCourse.set(course);
    this.editForm.set({
      defaultCapacity: course.defaultCapacity ?? undefined,
      defaultDurationWeeks: course.defaultDurationWeeks ?? undefined,
      resultType: course.resultType, requiresEvaluation: course.requiresEvaluation,
      requiresProviderEvaluation: course.requiresProviderEvaluation,
      hasCertificate: course.hasCertificate,
      evaluationBlocksCertificate: course.evaluationBlocksCertificate,
      isActive: course.isActive,
    });
    this.isEditDialogVisible.set(true);
  }

  updateEditField<K extends keyof UpdateTenantCourseDto>(key: K, value: UpdateTenantCourseDto[K]): void {
    this.editForm.update(f => ({ ...f, [key]: value }));
    if (this.editValidationErrors().length > 0) {
      this.validateEditForm();
    }
  }

  validateEditForm(): boolean {
    const errors: string[] = [];
    const data = this.editForm();
    if (data.defaultCapacity !== undefined && data.defaultCapacity !== null && (data.defaultCapacity < 1 || data.defaultCapacity > 9999)) {
      errors.push(this.l.t('::Training.TenantCourse.Validation.CapacityRange'));
    }
    if (data.defaultDurationWeeks !== undefined && data.defaultDurationWeeks !== null && (data.defaultDurationWeeks < 1 || data.defaultDurationWeeks > 999)) {
      errors.push(this.l.t('::Training.TenantCourse.Validation.DurationRange'));
    }
    this.editValidationErrors.set(errors);
    return errors.length === 0;
  }

  async onSaveEdit(): Promise<void> {
    if (!this.validateEditForm()) return;
    const course = this.editingCourse();
    if (!course) return;
    this.isSaving.set(true);
    try {
      await this.tenantCourseService.update(course.id, this.editForm());
      this.toaster.success(this.l.t('::Training.TenantCourse.UpdateSuccess'));
      this.isEditDialogVisible.set(false);
      await this.loadCourses();
    } catch {
      this.toaster.error(this.l.t('::Training.Common.SaveError'));
    } finally {
      this.isSaving.set(false);
    }
  }

  onCancelEdit(): void {
    this.isEditDialogVisible.set(false);
    this.editValidationErrors.set([]);
  }

  onDeleteCourse(course: TenantCourseDto): void {
    this.confirmation.warn(
      this.l.t('::Training.TenantCourse.DeleteConfirm'),
      this.l.t('::Training.ConfirmDelete')
    ).subscribe(async (status) => {
      if (status === 'confirm') {
        try {
          await this.tenantCourseService.delete(course.id);
          this.toaster.success(this.l.t('::Training.TenantCourse.DeleteSuccess'));
          await this.loadCourses();
        } catch {
          this.toaster.error(this.l.t('::Training.Common.DeleteError'));
        }
      }
    });
  }

  getResultTypeText = (value: ResultType): string => this.l.resultType(value);
}
