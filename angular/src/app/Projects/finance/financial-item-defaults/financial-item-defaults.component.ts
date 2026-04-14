// ──────────────────────────────────────────────────────────────
// financial-item-defaults.component.ts
// PAGE 2.2: Course Type Financial Item Defaults
// Two-panel layout: Left = CourseType selector, Right = assigned items grid.
// Fixed: drag-and-drop sort order persistence + grouped add dropdown.
// ──────────────────────────────────────────────────────────────

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

  // ── State ──
  selectedCourseType = signal<CourseType>(CourseType.ExternalInternational);
  defaults = signal<any[]>([]);
  subItems = signal<FinancialItemSubItemDto[]>([]);
  groupedSubItems = signal<Array<{ key: string; items: FinancialItemSubItemDto[] }>>([]);
  isAddDialogVisible = signal(false);
  selectedFinancialItemId = signal<string | null>(null);
  isLoading = signal(false);

  // ── Course type options (initialized once) ──
  courseTypeOptions: { value: CourseType; label: string; icon: string }[] = [];

  // ── Toolbar items (initialized in ngOnInit) ──
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

  // ── Data loading ──

  async loadDefaults(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await firstValueFrom(
        this.defaultService.getList(this.selectedCourseType())
      );
      // Sort by SortOrder for display
      const sorted = (result.items ?? []).sort((a: any, b: any) => a.sortOrder - b.sortOrder);
      this.defaults.set(sorted);
    } finally {
      this.isLoading.set(false);
    }
  }

  async loadSubItems(): Promise<void> {
    const items = await firstValueFrom(this.financialItemService.getSubItems());
    this.subItems.set(items);
    this.groupedSubItems.set(this.buildGroupedSubItems(items ?? []));
  }

  private buildGroupedSubItems(items: FinancialItemSubItemDto[]): Array<{ key: string; items: FinancialItemSubItemDto[] }> {
    const groups = new Map<string, FinancialItemSubItemDto[]>();
    for (const item of items) {
      const groupName = item.parentNameAr || this.l.t('::Training.CourseTypeDefaults.Ungrouped');
      const list = groups.get(groupName) ?? [];
      list.push(item);
      groups.set(groupName, list);
    }

    return Array.from(groups.entries()).map(([key, groupedItems]) => ({
      key,
      items: groupedItems,
    }));
  }

  // ── Course type selection ──

  onCourseTypeSelect(type: CourseType): void {
    this.selectedCourseType.set(type);
    this.loadDefaults();
  }

  isCourseTypeSelected(type: CourseType): boolean {
    return this.selectedCourseType() === type;
  }

  // ── Add item ──

  onAddItem(): void {
    this.selectedFinancialItemId.set(null);
    this.isAddDialogVisible.set(true);
  }

  async onAddConfirm(): Promise<void> {
    if (!this.selectedFinancialItemId()) return;

    // Auto-assign SortOrder = max + 1
    const currentMax = this.defaults().reduce(
      (max: number, d: any) => Math.max(max, d.sortOrder ?? 0),
      0
    );

    await firstValueFrom(
      this.defaultService.create({
        courseType: this.selectedCourseType(),
        financialItemId: this.selectedFinancialItemId()!,
        
      })
    );

    this.isAddDialogVisible.set(false);
    await this.loadDefaults();
  }

  // ── Delete item ──

  async onDeleteDefault(id: string): Promise<void> {
    await firstValueFrom(this.defaultService.delete(id));
    await this.loadDefaults();
  }

  // ── Drag-and-drop reorder (THE FIX) ──

  async onReorder(e: any): Promise<void> {
    e.promise = this.applyReorder(e);
  }

  private async applyReorder(e: any): Promise<void> {
    const items = [...this.defaults()];
    const movedId = e.itemData?.id;
    if (!movedId) {
      return;
    }

    const fromIndex = items.findIndex(x => x.id === movedId);
    if (fromIndex < 0) {
      return;
    }

    const visibleRows = e.component?.getVisibleRows?.() ?? [];
    const targetRowData = visibleRows[e.toIndex]?.data;
    let toIndex = targetRowData?.id
      ? items.findIndex(x => x.id === targetRowData.id)
      : items.length - 1;

    if (toIndex < 0) {
      toIndex = items.length - 1;
    }

    if (fromIndex === toIndex) {
      return;
    }

    const movedItem = items.splice(fromIndex, 1)[0];
    items.splice(toIndex, 0, movedItem);

    // Reassign sequential SortOrder values
    const sortOrderUpdates = items.map((item: any, index: number) => ({
      id: item.id,
      sortOrder: index + 1,
    }));

    // Optimistic UI update
    const updatedItems = items.map((item: any, index: number) => ({
      ...item,
      sortOrder: index + 1,
    }));
    this.defaults.set(updatedItems);

    // Persist to backend
    try {
      await firstValueFrom(
        this.defaultService.updateSortOrder({ items: sortOrderUpdates })
      );
    } catch {
      // Revert on failure
      await this.loadDefaults();
      throw new Error('Failed to persist sort order.');
    }
  }
}