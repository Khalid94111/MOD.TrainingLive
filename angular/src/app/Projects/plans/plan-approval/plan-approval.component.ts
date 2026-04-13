import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import { DxDataGridModule, DxPopupModule, DxTextAreaModule, DxButtonModule } from 'devextreme-angular';
import { ToolbarItem } from 'devextreme/ui/popup';
import { ActivatedRoute } from '@angular/router';
import { TrainingPlanService, TrainingPlanItemService } from 'src/app/proxy/training/plans';
import { TrainingPlanDto, TrainingPlanItemDto } from 'src/app/proxy/training/plans/dtos';
import { PlanStatus } from '../../shared';
 import { TrainingLocalizationHelper } from '../../shared';

@Component({
  standalone: true,
  selector: 'app-plan-approval',
  templateUrl: './plan-approval.component.html',
  styleUrl: './plan-approval.component.scss',
  imports: [CommonModule, LocalizationPipe, DxDataGridModule, DxPopupModule, DxTextAreaModule, DxButtonModule],
})
export class PlanApprovalComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private planService = inject(TrainingPlanService);
  private planItemService = inject(TrainingPlanItemService);
  private permissionService = inject(PermissionService);
    private l = inject(TrainingLocalizationHelper);


  planId = '';
  plan = signal<TrainingPlanDto | null>(null);
  items = signal<TrainingPlanItemDto[]>([]);

  isRejectDialogVisible = signal(false);
  rejectReason = signal('');

  canApprove = false;
  canFinalApprove = false;

  // Summary stats
  totalItems = signal(0);
  totalEstimatedCost = signal(0);
  internalCount = signal(0);
  externalLocalCount = signal(0);
  externalIntlCount = signal(0);
  hasMissingCosts = signal(false);

  rejectDialogToolbarItems: ToolbarItem[] | undefined;

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('planId') ?? '';
    this.canApprove = this.permissionService.getGrantedPolicy('Training.TrainingPlan.Approve');
    this.canFinalApprove = this.permissionService.getGrantedPolicy('Training.TrainingPlan.FinalApprove');

    this.rejectDialogToolbarItems = [
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Training.RejectPlan'), type: 'danger', onClick: () => this.onConfirmReject() },
      },
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Cancel'), onClick: () => this.isRejectDialogVisible.set(false) },
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
    const allItems = result.items ?? [];
    this.items.set(allItems);
    this.totalItems.set(allItems.length);

    const total = allItems.reduce((sum, i) => sum + (i.estimatedCost ?? 0), 0);
    this.totalEstimatedCost.set(total);
    this.internalCount.set(allItems.filter(i => i.courseType === 0).length);
    this.externalLocalCount.set(allItems.filter(i => i.courseType === 1).length);
    this.externalIntlCount.set(allItems.filter(i => i.courseType === 2).length);

    // Cost gate check — external items must have EstimatedCost
    const missing = allItems.some(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0));
    this.hasMissingCosts.set(missing);
  }

  get showApproveButton(): boolean {
    const p = this.plan();
    if (!p) return false;
    if (p.status === PlanStatus.UnderReview && this.canApprove) return true;
    if (p.status === PlanStatus.TDApproved && this.canFinalApprove) return true;
    return false;
  }

  get approveButtonText(): string {
    const p = this.plan();
    if (p?.status === PlanStatus.TDApproved) return this.l.t('::Training.ApprovePlan') + ' (TH)';
    return this.l.t('::Training.ApprovePlan') + ' (TD)';
  }

  async onApprove(): Promise<void> {
    const p = this.plan();
    if (!p) return;

    if (p.status === PlanStatus.UnderReview) {
      await firstValueFrom(this.planService.approve(this.planId));
    } else if (p.status === PlanStatus.TDApproved) {
      await firstValueFrom(this.planService.finalApprove(this.planId));
    }
    await this.loadPlan();
  }

  onReject(): void {
    this.rejectReason.set('');
    this.isRejectDialogVisible.set(true);
  }

  async onConfirmReject(): Promise<void> {
    await firstValueFrom(this.planService.reject(this.planId, this.rejectReason()));
    this.isRejectDialogVisible.set(false);
    await this.loadPlan();
  }

  async onReturn(): Promise<void> {
    await firstValueFrom(this.planService.returnToStaff(this.planId, ''));
    await this.loadPlan();
  }

  updateRejectReason(value: string): void {
    this.rejectReason.set(value);
  }
}
