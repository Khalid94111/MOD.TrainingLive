import { Component, inject, OnInit, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { DxDataGridModule, DxButtonModule, DxTextBoxModule, DxSelectBoxModule, DxDataGridComponent } from 'devextreme-angular';
import { CourseCatalogService, CourseFieldService, TrainingLocalizationHelper, createAbpStore } from '../../shared';
import type { CourseCatalogDto, CourseFieldDto } from '../../shared';
import { CatalogFormDialogComponent } from './catalog-form-dialog/catalog-form-dialog.component';

@Component({
  selector: 'app-course-catalog',
  standalone: true,
  imports: [CommonModule, LocalizationPipe, DxDataGridModule, DxButtonModule, DxTextBoxModule, DxSelectBoxModule, CatalogFormDialogComponent],
  templateUrl: './course-catalog.component.html',
  styleUrl: './course-catalog.component.scss',
})
export class CourseCatalogComponent implements OnInit {
  private readonly catalogService = inject(CourseCatalogService);
  private readonly fieldService = inject(CourseFieldService);
  readonly l = inject(TrainingLocalizationHelper);
  readonly grid = viewChild<DxDataGridComponent>('catalogGrid');

  dataSource!: ReturnType<typeof createAbpStore<CourseCatalogDto>>;
  courseFields = signal<CourseFieldDto[]>([]);
  searchText = signal('');
  filterFieldId = signal<string | null>(null);
  filterCategory = signal<string | null>(null);
  isDialogVisible = signal(false);
  editingCourse = signal<CourseCatalogDto | null>(null);
  categoryDataSource: any[] = [];
  resultTypeDataSource: any[] = [];
  searchButtonOptions: any = {};

  get pagerInfoText(): string { return this.l.t('::Training.PagerInfo'); }

  ngOnInit(): void {
    this.categoryDataSource = this.l.categoryDataSource();
    this.resultTypeDataSource = this.l.resultTypeDataSource();
    this.searchButtonOptions = { icon: 'search', onClick: () => this.onSearch() };
    this.loadFields();
    this.initDataSource();
  }

  private initDataSource(): void {
    this.dataSource = createAbpStore<CourseCatalogDto>({
      loadFn: params => this.catalogService.getList({
        ...params, filter: this.searchText() || undefined,
        fieldId: this.filterFieldId() || undefined, category: this.filterCategory() || undefined,
      }),
      removeFn: key => this.catalogService.delete(key),
    });
  }

  private async loadFields(): Promise<void> {
    this.courseFields.set(await this.fieldService.getAllActive());
  }

  onSearch(): void { this.grid()?.instance.refresh(); }
  onFilterChange(): void { this.grid()?.instance.refresh(); }
  onAddCourse(): void { this.editingCourse.set(null); this.isDialogVisible.set(true); }
  onEditCourse(data: CourseCatalogDto): void { this.editingCourse.set(data); this.isDialogVisible.set(true); }
  onDialogSaved(): void { this.isDialogVisible.set(false); this.grid()?.instance.refresh(); }
  onDialogCancelled(): void { this.isDialogVisible.set(false); }

  getResultTypeText = (rowData: any): string => this.l.resultType(rowData.resultType);
  getCategoryText = (rowData: any): string => this.l.category(rowData.category);
}
