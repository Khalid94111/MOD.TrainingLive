import { Component, inject, input, OnInit, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { DxDataGridModule, DxButtonModule, DxPopupModule } from 'devextreme-angular';
import { TenantCourseService, TrainingLocalizationHelper, createAbpStore } from '../../shared';
import type { CourseCatalogDto } from '../../shared';

@Component({
  selector: 'app-add-from-catalog-dialog',
  standalone: true,
  imports: [CommonModule, LocalizationPipe, DxDataGridModule, DxButtonModule, DxPopupModule],
  templateUrl: './add-from-catalog-dialog.component.html',
})
export class AddFromCatalogDialogComponent implements OnInit {
  private readonly tenantCourseService = inject(TenantCourseService);
  private readonly localization = inject(LocalizationService);
  readonly l = inject(TrainingLocalizationHelper);

  readonly visible = input.required<boolean>();
  readonly saved = output<void>();
  readonly cancelled = output<void>();

  dataSource!: ReturnType<typeof createAbpStore<CourseCatalogDto>>;
  selectedCourseIds = signal<string[]>([]);
  isSaving = signal(false);

  categoryDataSource: any[] = [];

  get selectedCountText(): string {
    return this.localization.instant('::Training.SelectedCount', this.selectedCourseIds().length.toString());
  }

  ngOnInit(): void {
    this.categoryDataSource = this.l.categoryDataSource();
    this.dataSource = createAbpStore<CourseCatalogDto>({
      loadFn: params => this.tenantCourseService.getAvailableCatalogCourses(params),
    });
  }

  onSelectionChanged(e: any): void {
    this.selectedCourseIds.set(e.selectedRowKeys ?? []);
  }

  async onConfirm(): Promise<void> {
    const ids = this.selectedCourseIds();
    if (ids.length === 0) return;
    this.isSaving.set(true);
    try {
      await this.tenantCourseService.addFromCatalog({ catalogCourseIds: ids });
      this.saved.emit();
    } finally {
      this.isSaving.set(false);
    }
  }

  onCancel(): void { this.cancelled.emit(); }
}
