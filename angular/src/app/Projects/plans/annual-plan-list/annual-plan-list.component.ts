import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import { DxDataGridModule, DxPopupModule, DxNumberBoxModule, DxDateBoxModule, DxButtonModule } from 'devextreme-angular';
import { ToolbarItem } from 'devextreme/ui/popup';
import { TrainingPlanService } from 'src/app/proxy/training/plans';
import { TrainingPlanDto, CreateUpdateTrainingPlanDto } from 'src/app/proxy/training/plans/dtos';
import { PlanStatus, TrainingLocalizationHelper } from '../../shared';
 
@Component({
  standalone: true,
  selector: 'app-annual-plan-list',
  templateUrl: './annual-plan-list.component.html',
  styleUrl: './annual-plan-list.component.scss',
  imports: [CommonModule, LocalizationPipe, DxDataGridModule, DxPopupModule, DxNumberBoxModule, DxDateBoxModule, DxButtonModule],
})
export class AnnualPlanListComponent implements OnInit {
  private planService = inject(TrainingPlanService);
  private permissionService = inject(PermissionService);
  private l = inject(TrainingLocalizationHelper);

  readonly PlanStatus = PlanStatus;


  plans = signal<TrainingPlanDto[]>([]);
  totalCount = signal(0);
  isDialogVisible = signal(false);
  isEditMode = signal(false);
  selectedPlanId = signal<string | null>(null);

  formData = signal<CreateUpdateTrainingPlanDto>({
    year: new Date().getFullYear() + 1,
  });

  canCreate: boolean = false;
  canApprove: boolean = false;

  dialogToolbarItems: ToolbarItem[] | undefined;

  get dialogTitle(): string {
    return this.isEditMode() ? this.l.t('::Training.TrainingPlan') : this.l.t('::Training.CreatePlan');
  }

  ngOnInit(): void {
    this.canCreate = this.permissionService.getGrantedPolicy('Training.TrainingPlan.Create');
    this.canApprove = this.permissionService.getGrantedPolicy('Training.TrainingPlan.Approve');
 
    this.dialogToolbarItems = [
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Save'), type: 'default', onClick: () => this.onSave() },
      },
      {
        widget: 'dxButton', location: 'after', toolbar: 'bottom',
        options: { text: this.l.t('::Cancel'), onClick: () => this.isDialogVisible.set(false) },
      },
    ];

    this.loadPlans();
  }

  async loadPlans(): Promise<void> {
    const result = await firstValueFrom(
      this.planService.getList({ maxResultCount: 100 })
    );
    this.plans.set(result.items ?? []);
    this.totalCount.set(result.totalCount);
  }

  onAdd(): void {
    this.isEditMode.set(false);
    this.selectedPlanId.set(null);
    this.formData.set({ year: new Date().getFullYear() + 1 });
    this.isDialogVisible.set(true);
  }

  onEdit(plan: TrainingPlanDto): void {
    this.isEditMode.set(true);
    this.selectedPlanId.set(plan.id);
    this.formData.set({
      year: plan.year,
      openDate: plan.openDate,
      closeDate: plan.closeDate,
    });
    this.isDialogVisible.set(true);
  }

  async onSave(): Promise<void> {
    const data = this.formData();
    if (this.isEditMode() && this.selectedPlanId()) {
      await firstValueFrom(this.planService.update(this.selectedPlanId()!, data));
    } else {
      await firstValueFrom(this.planService.create(data));
    }
    this.isDialogVisible.set(false);
    await this.loadPlans();
  }

  async onDelete(id: string): Promise<void> {
    await firstValueFrom(this.planService.delete(id));
    await this.loadPlans();
  }

  async onOpenWindow(id: string): Promise<void> {
    await firstValueFrom(this.planService.openSubmissionWindow(id));
    await this.loadPlans();
  }

  async onCloseWindow(id: string): Promise<void> {
    await firstValueFrom(this.planService.closeSubmissionWindow(id));
    await this.loadPlans();
  }

  async onApprove(id: string): Promise<void> {
    await firstValueFrom(this.planService.approve(id));
    await this.loadPlans();
  }

  async onFinalApprove(id: string): Promise<void> {
    await firstValueFrom(this.planService.finalApprove(id));
    await this.loadPlans();
  }

  getStatusBadgeClass(status: PlanStatus): string {
    const map: Record<number, string> = {
      [PlanStatus.Draft]: 'badge-draft',
      [PlanStatus.Open]: 'badge-open',
      [PlanStatus.Submitted]: 'badge-submitted',
      [PlanStatus.UnderReview]: 'badge-review',
      [PlanStatus.TDApproved]: 'badge-approved',
      [PlanStatus.THApproved]: 'badge-final',
    };
    return map[status] ?? 'badge-draft';
  }

  updateYear(value: number): void {
    this.formData.update(f => ({ ...f, year: value }));
  }

  updateOpenDate(value: string): void {
    this.formData.update(f => ({ ...f, openDate: value }));
  }

  updateCloseDate(value: string): void {
    this.formData.update(f => ({ ...f, closeDate: value }));
  }

  navigateToEntry(planId: string): void {
    // Navigate to plan entry page
    window.location.href = `/training/plans/${planId}/entry`;
  }

  navigateToReview(planId: string): void {
    window.location.href = `/training/plans/${planId}/review`;
  }

  navigateToApproval(planId: string): void {
    window.location.href = `/training/plans/${planId}/approve`;
  }
}
