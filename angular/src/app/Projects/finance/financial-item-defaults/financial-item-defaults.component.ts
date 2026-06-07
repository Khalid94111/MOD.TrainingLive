import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { DxPopupModule } from 'devextreme-angular/ui/popup';
import { DxSelectBoxModule } from 'devextreme-angular/ui/select-box';
import { ToolbarItem } from 'devextreme/ui/popup';
import { firstValueFrom } from 'rxjs';
import { CourseTypeFinancialDefaultService, FinancialItemService } from 'src/app/proxy/training/finance';
import type { CourseTypeFinancialItemDefaultDto, FinancialItemDto } from 'src/app/proxy/training/finance/dtos';
import { CourseType, TrainingLocalizationHelper } from '../../shared';

@Component({
  standalone: true,
  selector: 'app-financial-item-defaults',
  templateUrl: './financial-item-defaults.component.html',
  styleUrl: './financial-item-defaults.component.scss',
  imports: [CommonModule, LocalizationPipe, DxPopupModule, DxSelectBoxModule],
})
export class FinancialItemDefaultsComponent implements OnInit {
  private readonly defaultService = inject(CourseTypeFinancialDefaultService);
  private readonly financialItemService = inject(FinancialItemService);
  private readonly l = inject(TrainingLocalizationHelper);

  readonly CourseType = CourseType;

  selectedCourseType = signal<CourseType>(CourseType.ExternalInternational);
  defaults = signal<CourseTypeFinancialItemDefaultDto[]>([]);
  availableItems = signal<FinancialItemDto[]>([]);
  isAddDialogVisible = signal(false);
  selectedFinancialItemId = signal<string | null>(null);
  isLoading = signal(false);
  isSaving = signal(false);

  externalIntlCount = signal(0);
  externalLocalCount = signal(0);
  itemCount = computed(() => this.defaults().length);

  courseTypeOptions: { value: CourseType; label: string; icon: string }[] = [];
  addDialogToolbarItems: ToolbarItem[] | undefined;

  ngOnInit(): void {
    this.courseTypeOptions = [
      {
        value: CourseType.ExternalInternational,
        label: this.l.t('::Training.CourseType.ExternalInternational'),
        icon: '🌍',
      },
      {
        value: CourseType.ExternalLocal,
        label: this.l.t('::Training.CourseType.ExternalLocal'),
        icon: '🏢',
      },
    ];

    this.addDialogToolbarItems = [
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: {
          text: this.l.t('::Training.CourseTypeDefaults.AddItem'),
          type: 'default',
          stylingMode: 'contained',
          disabled: false,
          onClick: () => this.onAddConfirm(),
        },
      },
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: {
          text: this.l.t('::Training.Common.Cancel'),
          stylingMode: 'outlined',
          onClick: () => this.isAddDialogVisible.set(false),
        },
      },
    ];

    this.loadDefaults();
    this.loadAvailableItems();
    this.loadAllCounts();
  }

  async loadDefaults(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await firstValueFrom(
        this.defaultService.getList(this.selectedCourseType())
      );
      const sorted = (result.items ?? []).sort((a: any, b: any) => a.sortOrder - b.sortOrder);
      this.defaults.set(sorted);
    } finally {
      this.isLoading.set(false);
    }
  }

  async loadAvailableItems(): Promise<void> {
    const result = await firstValueFrom(
      this.financialItemService.getList({ maxResultCount: 1000, isActive: true })
    );
    const assignedIds = new Set(this.defaults().map((d) => d.financialItemId));
    this.availableItems.set((result.items ?? []).filter((i) => !assignedIds.has(i.id)));
  }

  async loadAllCounts(): Promise<void> {
    try {
      const [intlResult, localResult] = await Promise.all([
        firstValueFrom(this.defaultService.getList(CourseType.ExternalInternational)),
        firstValueFrom(this.defaultService.getList(CourseType.ExternalLocal)),
      ]);
      this.externalIntlCount.set((intlResult.items ?? []).length);
      this.externalLocalCount.set((localResult.items ?? []).length);
    } catch {
      // silently fail — counts are cosmetic
    }
  }

  onCourseTypeSelect(type: CourseType): void {
    this.selectedCourseType.set(type);
    this.loadDefaults();
    this.loadAvailableItems();
  }

  isCourseTypeSelected(type: CourseType): boolean {
    return this.selectedCourseType() === type;
  }

  onAddItem(): void {
    this.selectedFinancialItemId.set(null);
    this.isAddDialogVisible.set(true);
  }

  async onAddConfirm(): Promise<void> {
    if (!this.selectedFinancialItemId() || this.isSaving()) return;
    this.isSaving.set(true);
    try {
      await firstValueFrom(
        this.defaultService.create({
          courseType: this.selectedCourseType(),
          financialItemId: this.selectedFinancialItemId()!,
        })
      );
      this.isAddDialogVisible.set(false);
      await this.loadDefaults();
      await this.loadAvailableItems();
    } finally {
      this.isSaving.set(false);
    }
  }

  async onDeleteDefault(id: string): Promise<void> {
    if (!confirm(this.l.t('::Training.CourseTypeDefaults.DeleteConfirm'))) return;
    await firstValueFrom(this.defaultService.delete(id));
    await this.loadDefaults();
    await this.loadAvailableItems();
    await this.loadAllCounts();
  }

  async moveUp(index: number): Promise<void> {
    if (index <= 0) return;
    await this.swapOrder(index, index - 1);
  }

  async moveDown(index: number): Promise<void> {
    const items = this.defaults();
    if (index >= items.length - 1) return;
    await this.swapOrder(index, index + 1);
  }

  private async swapOrder(fromIndex: number, toIndex: number): Promise<void> {
    const items = [...this.defaults()];
    const temp = items[fromIndex];
    items[fromIndex] = items[toIndex];
    items[toIndex] = temp;

    const updated = items.map((item, index) => ({
      ...item,
      sortOrder: index + 1,
    }));
    this.defaults.set(updated);

    try {
      await firstValueFrom(
        this.defaultService.updateSortOrder({
          items: updated.map((x) => ({ id: x.id!, sortOrder: x.sortOrder })),
        })
      );
    } catch {
      await this.loadDefaults();
    }
  }

  selectBoxDisplayExpr(item: FinancialItemDto): string {
    if (!item) return '';
    const code = item.voteCode ? `(${item.voteCode})` : '';
    return `${item.nameAr ?? ''} ${code}`.trim();
  }
}
