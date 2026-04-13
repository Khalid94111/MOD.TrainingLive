import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe } from '@abp/ng.core';
import {
  DxDataGridModule, DxPopupModule, DxSelectBoxModule,
  DxNumberBoxModule, DxTextBoxModule, DxButtonModule,
} from 'devextreme-angular';
import { ToolbarItem } from 'devextreme/ui/popup';
import { ActivatedRoute } from '@angular/router';
import { TrainingPlanService, TrainingPlanItemService, PlanItemFinancialItemService } from 'src/app/proxy/training/plans';
import { TrainingPlanDto, TrainingPlanItemDto, PlanItemFinancialItemDto, CreateUpdatePlanItemFinancialItemDto } from 'src/app/proxy/training/plans/dtos';
 import { TrainingLocalizationHelper } from '../../shared';

@Component({
  standalone: true,
  selector: 'app-plan-review',
  templateUrl: './plan-review.component.html',
  styleUrl: './plan-review.component.scss',
  imports: [
    CommonModule, LocalizationPipe,
    DxDataGridModule, DxPopupModule, DxSelectBoxModule,
    DxNumberBoxModule, DxTextBoxModule, DxButtonModule,
  ],
})
export class PlanReviewComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private planService = inject(TrainingPlanService);
  private planItemService = inject(TrainingPlanItemService);
  private financialItemService = inject(PlanItemFinancialItemService);
    private l = inject(TrainingLocalizationHelper);


  planId = '';
  plan = signal<TrainingPlanDto | null>(null);
  items = signal<TrainingPlanItemDto[]>([]);
  selectedPlanItemId = signal<string | null>(null);
  financialItems = signal<PlanItemFinancialItemDto[]>([]);

  // Financial item assignment dialog
  isFinancialDialogVisible = signal(false);
  financialFormData = signal<CreateUpdatePlanItemFinancialItemDto>({
    planItemId: '',
    financialItemId: '',
    estimatedAmountOMR: 0,
  });

  // Cost entry dialog
  isCostDialogVisible = signal(false);
  costValue = signal(0);
  costItemId = signal<string | null>(null);

  // TODO: load from FinancialItemService
  availableFinancialItems = signal<any[]>([]);

  financialDialogToolbarItems: ToolbarItem[] | undefined;
  costDialogToolbarItems: ToolbarItem[] | undefined;

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('planId') ?? '';

    this.financialDialogToolbarItems = [
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Save'), type: 'default', onClick: () => this.onSaveFinancialItem() },
      },
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Cancel'), onClick: () => this.isFinancialDialogVisible.set(false) },
      },
    ];

    this.costDialogToolbarItems = [
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Save'), type: 'default', onClick: () => this.onSaveCost() },
      },
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Cancel'), onClick: () => this.isCostDialogVisible.set(false) },
      },
    ];

    this.loadPlan();
    this.loadItems();
  }

  async loadPlan(): Promise<void> {
    const result = await firstValueFrom(this.planService.get(this.planId));
    this.plan.set(result);
  }

  async loadItems(): Promise<void> {
    const result = await firstValueFrom(
      this.planItemService.getList({ planId: this.planId, maxResultCount: 500 })
    );
    this.items.set(result.items ?? []);
  }

  async onItemRowClick(e: any): Promise<void> {
    const planItemId = e.data.id as string;
    this.selectedPlanItemId.set(planItemId);
    await this.loadFinancialItems(planItemId);
  }

  async loadFinancialItems(planItemId: string): Promise<void> {
    const result = await firstValueFrom(
      this.financialItemService.getListByPlanItem(planItemId)
    );
    this.financialItems.set(result);
  }

  onAddFinancialItem(): void {
    const planItemId = this.selectedPlanItemId();
    if (!planItemId) return;

    this.financialFormData.set({
      planItemId,
      financialItemId: '',
      estimatedAmountOMR: 0,
    });
    this.isFinancialDialogVisible.set(true);
  }

  async onSaveFinancialItem(): Promise<void> {
    const data = this.financialFormData();
    await firstValueFrom(this.financialItemService.create(data));
    this.isFinancialDialogVisible.set(false);
    if (this.selectedPlanItemId()) {
      await this.loadFinancialItems(this.selectedPlanItemId()!);
    }
  }

  async onDeleteFinancialItem(id: string): Promise<void> {
    await firstValueFrom(this.financialItemService.delete(id));
    if (this.selectedPlanItemId()) {
      await this.loadFinancialItems(this.selectedPlanItemId()!);
    }
  }

  async onAutoFill(): Promise<void> {
    const planItemId = this.selectedPlanItemId();
    if (!planItemId) return;

    await firstValueFrom(this.financialItemService.autoFillFromDefaults(planItemId));
    await this.loadFinancialItems(planItemId);
  }

  onEditCost(item: TrainingPlanItemDto): void {
    this.costItemId.set(item.id);
    this.costValue.set(item.estimatedCost ?? 0);
    this.isCostDialogVisible.set(true);
  }

  async onSaveCost(): Promise<void> {
    const id = this.costItemId();
    if (!id) return;

    await firstValueFrom(
      this.planItemService.updateEstimatedCost(id, { estimatedCost: this.costValue() })
    );
    this.isCostDialogVisible.set(false);
    await this.loadItems();
  }

  async onSubmitForReview(): Promise<void> {
    await firstValueFrom(this.planService.submitForReview(this.planId));
    await this.loadPlan();
  }

  async onCloseWindow(): Promise<void> {
    await firstValueFrom(this.planService.closeSubmissionWindow(this.planId));
    await this.loadPlan();
  }

  updateFinancialItemId(value: string): void {
    this.financialFormData.update(f => ({ ...f, financialItemId: value }));
  }

  updateEstimatedAmountOMR(value: number): void {
    this.financialFormData.update(f => ({ ...f, estimatedAmountOMR: value }));
  }

  updateEstimatedAmountUSD(value: number): void {
    this.financialFormData.update(f => ({ ...f, estimatedAmountUSD: value }));
  }

  updateNotes(value: string): void {
    this.financialFormData.update(f => ({ ...f, notes: value }));
  }

  updateCostValue(value: number): void {
    this.costValue.set(value);
  }
}
