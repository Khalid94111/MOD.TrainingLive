import { Component, computed, inject, input, OnInit, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { TenantCourseService, TrainingLocalizationHelper } from '../../shared';
import type { CourseCatalogDto } from '../../shared';
import { ToasterService } from '@abp/ng.theme.shared';

@Component({
  selector: 'app-add-from-catalog-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe],
  templateUrl: './add-from-catalog-dialog.component.html',
  styleUrl: './add-from-catalog-dialog.component.scss',
})
export class AddFromCatalogDialogComponent implements OnInit {
  private readonly tenantCourseService = inject(TenantCourseService);
  private readonly toaster = inject(ToasterService);
  readonly l = inject(TrainingLocalizationHelper);

  readonly visible = input.required<boolean>();
  readonly saved = output<void>();
  readonly cancelled = output<void>();

  catalogCourses = signal<CourseCatalogDto[]>([]);
  isLoading = signal(false);
  selectedIds = signal<Set<string>>(new Set());
  searchText = signal('');
  isSaving = signal(false);

  filteredCourses = computed(() => {
    const term = this.searchText().trim().toLowerCase();
    if (!term) return this.catalogCourses();
    return this.catalogCourses().filter(c =>
      (c.courseNameAr?.toLowerCase().includes(term) ?? false) ||
      (c.courseNameEn?.toLowerCase().includes(term) ?? false)
    );
  });

  selectedCount = computed(() => this.selectedIds().size);
  selectedCountText = computed(() => {
    return this.l.t('::Training.SelectedCount').replace('{0}', String(this.selectedCount()));
  });

  ngOnInit(): void {
    this.loadCatalogCourses();
  }

  async loadCatalogCourses(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await this.tenantCourseService.getAvailableCatalogCourses({
        skipCount: 0, maxResultCount: 1000,
      });
      this.catalogCourses.set(result.items ?? []);
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
    } finally {
      this.isLoading.set(false);
    }
  }

  toggleSelection(id: string): void {
    this.selectedIds.update(set => {
      const next = new Set(set);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  isSelected(id: string): boolean {
    return this.selectedIds().has(id);
  }

  toggleAll(): void {
    const allIds = this.filteredCourses().map(c => c.id);
    const current = this.selectedIds();
    const allSelected = allIds.every(id => current.has(id));
    if (allSelected) {
      this.selectedIds.update(set => {
        const next = new Set(set);
        allIds.forEach(id => next.delete(id));
        return next;
      });
    } else {
      this.selectedIds.update(set => {
        const next = new Set(set);
        allIds.forEach(id => next.add(id));
        return next;
      });
    }
  }

  isAllSelected(): boolean {
    const courses = this.filteredCourses();
    if (courses.length === 0) return false;
    return courses.every(c => this.selectedIds().has(c.id));
  }

  isPartialSelected(): boolean {
    const courses = this.filteredCourses();
    if (courses.length === 0) return false;
    const selected = courses.filter(c => this.selectedIds().has(c.id)).length;
    return selected > 0 && selected < courses.length;
  }

  async onConfirm(): Promise<void> {
    const ids = Array.from(this.selectedIds());
    if (ids.length === 0) return;
    this.isSaving.set(true);
    try {
      await this.tenantCourseService.addFromCatalog({ catalogCourseIds: ids });
      this.toaster.success(this.l.t('::Training.TenantCourse.AddSuccess'));
      this.saved.emit();
    } catch {
      this.toaster.error(this.l.t('::Training.Common.SaveError'));
    } finally {
      this.isSaving.set(false);
    }
  }

  onCancel(): void { this.cancelled.emit(); }
}
