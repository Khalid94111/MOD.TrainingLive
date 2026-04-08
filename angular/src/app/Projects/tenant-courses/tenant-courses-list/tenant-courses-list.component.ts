import { Component, inject, OnInit, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import {
  DxDataGridModule, DxButtonModule, DxTextBoxModule, DxSelectBoxModule,
  DxPopupModule, DxNumberBoxModule, DxCheckBoxModule, DxSwitchModule, DxDataGridComponent,
} from 'devextreme-angular';
import { TenantCourseService, TrainingLocalizationHelper, createAbpStore } from '../../shared';
import type { TenantCourseDto, TenantCourseConditionDto, UpdateTenantCourseDto } from '../../shared';
import { ResultType } from '../../shared/models/training-enums';
import { AddFromCatalogDialogComponent } from '../add-from-catalog-dialog/add-from-catalog-dialog.component';

@Component({
  selector: 'app-tenant-courses-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule, LocalizationPipe,
    DxDataGridModule, DxButtonModule, DxTextBoxModule, DxSelectBoxModule,
    DxPopupModule, DxNumberBoxModule, DxCheckBoxModule, DxSwitchModule,
    AddFromCatalogDialogComponent,
  ],
  templateUrl: './tenant-courses-list.component.html',
})
export class TenantCoursesListComponent implements OnInit {
  private readonly tenantCourseService = inject(TenantCourseService);
  readonly l = inject(TrainingLocalizationHelper);
  readonly grid = viewChild<DxDataGridComponent>('tenantGrid');

  dataSource!: ReturnType<typeof createAbpStore<TenantCourseDto>>;
  searchText = signal('');
  filterResultType = signal<ResultType | null>(null);

  isAddDialogVisible = signal(false);
  isEditDialogVisible = signal(false);
  editingCourse = signal<TenantCourseDto | null>(null);
  conditions = signal<TenantCourseConditionDto[]>([]);

  editForm = signal<UpdateTenantCourseDto>({
    defaultCapacity: undefined, defaultDurationWeeks: undefined,
    resultType: ResultType.AttendanceOnly,
    requiresEvaluation: false, requiresProviderEvaluation: false,
    hasCertificate: false, evaluationBlocksCertificate: false,
    isActive: true,
  });

  resultTypeDataSource: any[] = [];
  get showEvaluationBlocks(): boolean { return this.editForm().requiresEvaluation; }

  ngOnInit(): void {
    this.resultTypeDataSource = this.l.resultTypeDataSource();
    this.initDataSource();
  }

  private initDataSource(): void {
    this.dataSource = createAbpStore<TenantCourseDto>({
      loadFn: params =>
        this.tenantCourseService.getList({
          ...params,
          filter: this.searchText() || undefined,
          resultType: this.filterResultType() ?? undefined,
        }),
      removeFn: key => this.tenantCourseService.delete(key),
    });
  }

  onSearch(): void { this.grid()?.instance.refresh(); }
  onFilterChange(): void { this.grid()?.instance.refresh(); }
  onOpenAddDialog(): void { this.isAddDialogVisible.set(true); }

  onCoursesAdded(): void {
    this.isAddDialogVisible.set(false);
    this.grid()?.instance.refresh();
  }

  async onEditCourse(data: TenantCourseDto): Promise<void> {
    this.editingCourse.set(data);
    this.editForm.set({
      defaultCapacity: data.defaultCapacity ?? undefined,
      defaultDurationWeeks: data.defaultDurationWeeks ?? undefined,
      resultType: data.resultType,
      requiresEvaluation: data.requiresEvaluation,
      requiresProviderEvaluation: data.requiresProviderEvaluation,
      hasCertificate: data.hasCertificate,
      evaluationBlocksCertificate: data.evaluationBlocksCertificate,
      isActive: data.isActive,
    });
    this.conditions.set(await this.tenantCourseService.getConditions(data.id));
    this.isEditDialogVisible.set(true);
  }

  updateEditField<K extends keyof UpdateTenantCourseDto>(key: K, value: UpdateTenantCourseDto[K]): void {
    this.editForm.update(f => ({ ...f, [key]: value }));
  }

  async onSaveEdit(): Promise<void> {
    const course = this.editingCourse();
    if (!course) return;
    await this.tenantCourseService.update(course.id, this.editForm());
    this.isEditDialogVisible.set(false);
    this.grid()?.instance.refresh();
  }

  getResultTypeText = (rowData: any): string => this.l.resultType(rowData.resultType);
}
