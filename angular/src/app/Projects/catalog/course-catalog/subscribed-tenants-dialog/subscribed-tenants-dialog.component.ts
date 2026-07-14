import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { CourseCatalogService, TrainingLocalizationHelper } from '../../../shared';
import type { CourseCatalogSubscribedTenantDto } from '../../../shared';
import { ToasterService } from '@abp/ng.theme.shared';

@Component({
  selector: 'app-subscribed-tenants-dialog',
  standalone: true,
  imports: [CommonModule, LocalizationPipe],
  templateUrl: './subscribed-tenants-dialog.component.html',
  styleUrl: './subscribed-tenants-dialog.component.scss',
})
export class SubscribedTenantsDialogComponent {
  private readonly catalogService = inject(CourseCatalogService);
  private readonly toaster = inject(ToasterService);
  readonly l = inject(TrainingLocalizationHelper);

  readonly visible = input.required<boolean>();
  readonly catalogCourseId = input.required<string>();
  readonly catalogCourseName = input<string>();
  readonly cancelled = output<void>();

  tenants = signal<CourseCatalogSubscribedTenantDto[]>([]);
  isLoading = signal(false);
  searchText = signal('');

  filteredTenants = computed(() => {
    const term = this.searchText().trim().toLowerCase();
    if (!term) return this.tenants();
    return this.tenants().filter(t =>
      (t.tenantName?.toLowerCase().includes(term) ?? false));
  });

  constructor() {
    effect(() => {
      const id = this.catalogCourseId();
      if (this.visible() && id) {
        this.loadTenants(id);
      }
    });
  }

  async loadTenants(id: string): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await this.catalogService.getSubscribedTenants(id);
      this.tenants.set(result ?? []);
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
    } finally {
      this.isLoading.set(false);
    }
  }

  onCancel(): void { this.cancelled.emit(); }
}
