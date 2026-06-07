import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationService, LocalizationPipe } from '@abp/ng.core';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { DxSelectBoxModule } from 'devextreme-angular/ui/select-box';
import { DxPopupModule } from 'devextreme-angular/ui/popup';
import { ToolbarItem } from 'devextreme/ui/popup';
import { firstValueFrom } from 'rxjs';
import { CourseTypeFinancialDefaultService, FinancialItemService } from 'src/app/proxy/training/finance';
import type { FinancialItemSubItemDto } from 'src/app/proxy/training/finance/dtos';
import { CourseType, TrainingLocalizationHelper } from '../../shared';

@Component({
  standalone: true,
  selector: 'app-financial-item-defaults',
  templateUrl: './financial-item-defaults.component.html',
  styleUrl: './financial-item-defaults.component.scss',
  imports: [
    CommonModule,
    LocalizationPipe,
    DxDataGridModule,
    DxSelectBoxModule,
    DxPopupModule,
  ],
})
export class FinancialItemDefaultsComponent implements OnInit {
  private readonly defaultService = inject(CourseTypeFinancialDefaultService);
  private readonly financialItemService = inject(FinancialItemService);
  private readonly l = inject(TrainingLocalizationHelper);

  selectedCourseType = signal<CourseType>(CourseType.ExternalInternational);
  defaults = signal<any[]>([]);
  subItems = signal<FinancialItemSubItemDto[]>([]);
  isAddDialogVisible = signal(false);
  selectedFinancialItemId = signal<string | null>(null);
  isLoading = signal(false);

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
    this.loadSubItems();
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

  async loadSubItems(): Promise<void> {
    const items = await firstValueFrom(this.financialItemService.getSubItems());
    // Filter out already-assigned items
    const assignedIds = new Set(this.defaults().map((d: any) => d.financialItemId));
    this.subItems.set((items ?? []).filter(i => !assignedIds.has(i.id)));
  }

  onCourseTypeSelect(type: CourseType): void {
    this.selectedCourseType.set(type);
    this.loadDefaults();
    this.loadSubItems();
  }

  isCourseTypeSelected(type: CourseType): boolean {
    return this.selectedCourseType() === type;
  }

  onAddItem(): void {
    this.selectedFinancialItemId.set(null);
    this.isAddDialogVisible.set(true);
  }

  async onAddConfirm(): Promise<void> {
    if (!this.selectedFinancialItemId()) return;

    await firstValueFrom(
      this.defaultService.create({
        courseType: this.selectedCourseType(),
        financialItemId: this.selectedFinancialItemId()!,
      })
    );

    this.isAddDialogVisible.set(false);
    await this.loadDefaults();
    await this.loadSubItems();
  }

  async onDeleteDefault(id: string): Promise<void> {
    await firstValueFrom(this.defaultService.delete(id));
    await this.loadDefaults();
    await this.loadSubItems();
  }

  async onReorder(e: any): Promise<void> {
    e.promise = this.applyReorder(e);
  }

  private async applyReorder(e: any): Promise<void> {
    const items = [...this.defaults()];
    const movedId = e.itemData?.id;
    if (!movedId) return;

    const fromIndex = items.findIndex((x: any) => x.id === movedId);
    if (fromIndex < 0) return;

    const visibleRows = e.component?.getVisibleRows?.() ?? [];
    const targetRowData = visibleRows[e.toIndex]?.data;
    let toIndex = targetRowData?.id
      ? items.findIndex((x: any) => x.id === targetRowData.id)
      : items.length - 1;

    if (toIndex < 0) toIndex = items.length - 1;
    if (fromIndex === toIndex) return;

    const movedItem = items.splice(fromIndex, 1)[0];
    items.splice(toIndex, 0, movedItem);

    const sortOrderUpdates = items.map((item: any, index: number) => ({
      id: item.id,
      sortOrder: index + 1,
    }));

    const updatedItems = items.map((item: any, index: number) => ({
      ...item,
      sortOrder: index + 1,
    }));
    this.defaults.set(updatedItems);

    try {
      await firstValueFrom(
        this.defaultService.updateSortOrder({ items: sortOrderUpdates })
      );
    } catch {
      await this.loadDefaults();
    }
  }
}
