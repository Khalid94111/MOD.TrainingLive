import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import {
  DxTreeListModule,
  DxTreeListComponent,
} from 'devextreme-angular/ui/tree-list';
import { DxPopupModule } from 'devextreme-angular/ui/popup';
import { DxTextBoxModule } from 'devextreme-angular/ui/text-box';
import { DxSelectBoxModule } from 'devextreme-angular/ui/select-box';
import { DxCheckBoxModule } from 'devextreme-angular/ui/check-box';
import { DxSwitchModule } from 'devextreme-angular/ui/switch';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { ToolbarItem } from 'devextreme/ui/popup';
import { FinancialItemService } from '../../shared/services/finance-proxy.service';
import { FinancialItemDto, CreateUpdateFinancialItemDto } from 'src/app/proxy/training/finance/dtos';
import { TrainingLocalizationHelper } from '../../shared';

@Component({
  selector: 'app-financial-items',
  standalone: true,
  imports: [
    CommonModule,
    LocalizationPipe,
    DxTreeListModule,
    DxPopupModule,
    DxTextBoxModule,
    DxSelectBoxModule,
    DxCheckBoxModule,
    DxSwitchModule,
    DxButtonModule,
  ],
  templateUrl: './financial-items.component.html',
  styleUrl: './financial-items.component.scss',
})
export class FinancialItemsComponent implements OnInit {
  private readonly service = inject(FinancialItemService);
  readonly l = inject(TrainingLocalizationHelper);
  readonly treeList = viewChild<DxTreeListComponent>('treeList');

  items = signal<FinancialItemDto[]>([]);
  parentItems = signal<FinancialItemDto[]>([]);
  isDialogVisible = signal(false);
  isEditMode = signal(false);
  searchText = signal('');
  filterActive = signal<boolean | undefined>(undefined);

  editingId: string | null = null;
  formData: CreateUpdateFinancialItemDto = this.getEmptyForm();

  dialogToolbarItems: ToolbarItem[] | undefined;
  activeFilterOptions: { value: boolean | undefined; text: string }[] | undefined;

  get dialogTitle(): string {
    return this.isEditMode()
      ? this.l.t('::Training.FinancialItems.Edit')
      : this.l.t('::Training.FinancialItems.Add');
  }

  ngOnInit(): void {
    this.dialogToolbarItems = [
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: {
          text: this.l.t('::Training.Common.Save'),
          type: 'default',
          stylingMode: 'contained',
          onClick: () => this.onSave(),
        },
      },
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: {
          text: this.l.t('::Training.Common.Cancel'),
          onClick: () => this.isDialogVisible.set(false),
        },
      },
    ];

    this.activeFilterOptions = [
      { value: undefined, text: this.l.t('::Training.FinancialItems.All') },
      { value: true, text: this.l.t('::Training.FinancialItems.ActiveOnly') },
      { value: false, text: this.l.t('::Training.FinancialItems.InactiveOnly') },
    ];

    this.loadData();
  }

  async loadData(): Promise<void> {
    const result = await this.service.getList({
      filter: this.searchText() || undefined,
      isActive: this.filterActive(),
      maxResultCount: 1000,
      skipCount: 0,
      sorting: '',
    });
    this.items.set(result.items ?? []);
    this.parentItems.set(
      (result.items ?? []).filter(x => !x.parentId)
    );
  }

  onAdd(): void {
    this.isEditMode.set(false);
    this.editingId = null;
    this.formData = this.getEmptyForm();
    this.isDialogVisible.set(true);
  }

  onAddSubItem(parentId: string): void {
    this.isEditMode.set(false);
    this.editingId = null;
    this.formData = this.getEmptyForm();
    this.formData.parentId = parentId;
    this.isDialogVisible.set(true);
  }

  onEdit(item: FinancialItemDto): void {
    this.isEditMode.set(true);
    this.editingId = item.id;
    this.formData = {
      parentId: item.parentId ?? undefined,
      nameAr: item.nameAr,
      nameEn: item.nameEn,
      voteCode: item.voteCode,
      isActive: item.isActive,
    };
    this.isDialogVisible.set(true);
  }

  async onSave(): Promise<void> {
    if (this.isEditMode() && this.editingId) {
      await this.service.update(this.editingId, this.formData);
    } else {
      await this.service.create(this.formData);
    }
    this.isDialogVisible.set(false);
    await this.loadData();
  }

  async onDelete(id: string): Promise<void> {
    await this.service.delete(id);
    await this.loadData();
  }

  onSearch(value: string): void {
    this.searchText.set(value);
    this.loadData();
  }

  onFilterActive(value: boolean | undefined): void {
    this.filterActive.set(value);
    this.loadData();
  }

  private getEmptyForm(): CreateUpdateFinancialItemDto {
    return {
      parentId: undefined,
      nameAr: '',
      nameEn: '',
      voteCode: '',
      isActive: true,
    };
  }
}
