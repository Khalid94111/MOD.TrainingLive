import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { DxSelectBoxModule } from 'devextreme-angular/ui/select-box';
import { DxPopupModule } from 'devextreme-angular/ui/popup';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { ToolbarItem } from 'devextreme/ui/popup';
import {
  CourseTypeFinancialDefaultService,
  FinancialItemService,
} from '../../shared/services/finance-proxy.service';
import { CourseTypeFinancialItemDefaultDto, FinancialItemDto } from 'src/app/proxy/training/finance/dtos';
import { TrainingLocalizationHelper, CourseType } from '../../shared';


@Component({
  selector: 'app-financial-item-defaults',
  standalone: true,
  imports: [
    CommonModule,
    LocalizationPipe,
    DxDataGridModule,
    DxSelectBoxModule,
    DxPopupModule,
    DxButtonModule,
  ],
  templateUrl: './financial-item-defaults.component.html',
  styleUrl: './financial-item-defaults.component.scss',
})
export class FinancialItemDefaultsComponent implements OnInit {
  private readonly defaultService = inject(CourseTypeFinancialDefaultService);
  private readonly financialItemService = inject(FinancialItemService);
  readonly l = inject(TrainingLocalizationHelper);

  selectedCourseType = signal<CourseType>(CourseType.ExternalInternational);
  defaults = signal<CourseTypeFinancialItemDefaultDto[]>([]);
  subItems = signal<FinancialItemDto[]>([]);
  isAddDialogVisible = signal(false);
  selectedFinancialItemId = signal<string | null>(null);

  courseTypes: { value: CourseType; label: string; itemCount: string }[] = [];
  addDialogToolbarItems: ToolbarItem[] | undefined;

  get selectedTypeLabel(): string {
    return this.selectedCourseType() === CourseType.ExternalInternational
      ? this.l.t('::Training.CourseTypeDefaults.ExternalInternational')
      : this.l.t('::Training.CourseTypeDefaults.ExternalLocal');
  }

  ngOnInit(): void {
    this.courseTypes = [
      {
        value: CourseType.ExternalInternational,
        label: this.l.t('::Training.CourseTypeDefaults.ExternalInternational'),
        itemCount: '6',
      },
      {
        value: CourseType.ExternalLocal,
        label: this.l.t('::Training.CourseTypeDefaults.ExternalLocal'),
        itemCount: '1',
      },
    ];

    this.addDialogToolbarItems = [
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: {
          text: this.l.t('::Training.Common.Save'),
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
          onClick: () => this.isAddDialogVisible.set(false),
        },
      },
    ];

    this.loadSubItems();
    this.loadDefaults();
  }

  async loadSubItems(): Promise<void> {
    const items = await this.financialItemService.getSubItems();
    this.subItems.set(items);
  }

  async loadDefaults(): Promise<void> {
    const items = await this.defaultService.getList(this.selectedCourseType());
    this.defaults.set(items);
  }

  onCourseTypeChange(courseType: CourseType): void {
    this.selectedCourseType.set(courseType);
    this.loadDefaults();
  }

  onAddItem(): void {
    this.selectedFinancialItemId.set(null);
    this.isAddDialogVisible.set(true);
  }

  async onAddConfirm(): Promise<void> {
    if (!this.selectedFinancialItemId()) return;

    await this.defaultService.create({
      courseType: this.selectedCourseType(),
      financialItemId: this.selectedFinancialItemId()!,
    });

    this.isAddDialogVisible.set(false);
    await this.loadDefaults();
  }

  async onDeleteDefault(id: string): Promise<void> {
    await this.defaultService.delete(id);
    await this.loadDefaults();
  }

  isCourseTypeSelected(type: CourseType): boolean {
    return this.selectedCourseType() === type;
  }
}
