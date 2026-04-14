import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import {  PermissionService } from '@abp/ng.core';
import { ActivatedRoute, Router } from '@angular/router';
import { TrainingPlanService, TrainingPlanItemService } from 'src/app/proxy/training/plans';
import { TrainingPlanDto, TrainingPlanItemDto } from 'src/app/proxy/training/plans/dtos';
import { PlanStatus, TrainingLocalizationHelper } from '../../shared';
 

@Component({
  standalone: true,
  selector: 'app-plan-approval',
  templateUrl: './plan-approval.component.html',
  styleUrls: ['./plan-approval.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule],
})
export class PlanApprovalComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private planService = inject(TrainingPlanService);
  private planItemService = inject(TrainingPlanItemService);
  private permissionService = inject(PermissionService);
  l = inject(TrainingLocalizationHelper);

  planId = '';
  plan = signal<TrainingPlanDto | null>(null);
  items = signal<TrainingPlanItemDto[]>([]);

  isRejectOpen = signal(false);
  rejectReason = signal('');

  canApprove = false;
  canFinalApprove = false;

  PlanStatus = PlanStatus;

  // Computed stats
  totalItems = signal(0);
  totalCost = signal(0);
  internalCount = signal(0);
  externalLocalCount = signal(0);
  externalIntlCount = signal(0);
  itemsMissingCost = signal(0);

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('planId') ?? '';
    this.canApprove = this.permissionService.getGrantedPolicy('Training.TrainingPlan.Approve');
    this.canFinalApprove = this.permissionService.getGrantedPolicy('Training.TrainingPlan.FinalApprove');
    this.loadData();
  }

  async loadData(): Promise<void> {
    const [plan, itemsResult] = await Promise.all([
      firstValueFrom(this.planService.get(this.planId)),
      firstValueFrom(this.planItemService.getList({ planId: this.planId, maxResultCount: 500 })),
    ]);

    this.plan.set(plan);
    const allItems = itemsResult.items ?? [];
    this.items.set(allItems);

    this.totalItems.set(allItems.length);
    this.totalCost.set(allItems.reduce((s, i) => s + (i.estimatedCost ?? 0), 0));
    this.internalCount.set(allItems.filter(i => i.courseType === 0).length);
    this.externalLocalCount.set(allItems.filter(i => i.courseType === 1).length);
    this.externalIntlCount.set(allItems.filter(i => i.courseType === 2).length);
    this.itemsMissingCost.set(
      allItems.filter(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0)).length
    );
  }

  get showActions(): boolean {
    const p = this.plan();
    if (!p) return false;
    return (p.status === PlanStatus.UnderReview && this.canApprove) ||
           (p.status === PlanStatus.TDApproved && this.canFinalApprove);
  }

  get approveLabel(): string {
    return this.plan()?.status === PlanStatus.TDApproved
      ? '✅ الاعتماد النهائي (TH)'
      : '✅ اعتماد (TD)';
  }

  async onApprove(): Promise<void> {
    const p = this.plan();
    if (!p) return;
    if (p.status === PlanStatus.UnderReview) {
      await firstValueFrom(this.planService.approve(this.planId));
    } else {
      await firstValueFrom(this.planService.finalApprove(this.planId));
    }
    await this.loadData();
  }

  async onReturn(): Promise<void> {
    await firstValueFrom(this.planService.returnToStaff(this.planId, ''));
    await this.loadData();
  }

  openRejectDialog(): void {
    this.rejectReason.set('');
    this.isRejectOpen.set(true);
  }

  async onConfirmReject(): Promise<void> {
    await firstValueFrom(this.planService.reject(this.planId, this.rejectReason()));
    this.isRejectOpen.set(false);
    await this.loadData();
  }

  getCourseTypeBadge(type: number): string {
    return ({ 0: 'badge-internal', 1: 'badge-ext-local', 2: 'badge-ext-intl' } as Record<number, string>)[type] ?? '';
  }

  getCourseTypeText(type: number): string {
    return ({ 0: 'داخلية', 1: 'خارجية محلية', 2: 'خارجية دولية' } as Record<number, string>)[type] ?? '';
  }

  formatCost(cost?: number): string {
    if (!cost || cost <= 0) return '';
    return cost.toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 });
  }

  isMissingCost(item: TrainingPlanItemDto): boolean {
    return item.courseType !== 0 && (!item.estimatedCost || item.estimatedCost <= 0);
  }

  getStatusText(status: PlanStatus): string {
    return ({
      [PlanStatus.Draft]: 'مسودة',
      [PlanStatus.Open]: 'مفتوحة',
      [PlanStatus.Submitted]: 'مُرسلة',
      [PlanStatus.UnderReview]: 'قيد المراجعة',
      [PlanStatus.TDApproved]: 'معتمدة TD',
      [PlanStatus.THApproved]: 'معتمدة نهائياً',
    } as Record<number, string>)[status] ?? '';
  }

  getStatusBadgeClass(status: PlanStatus): string {
    return ({
      [PlanStatus.Draft]: 'badge-draft',
      [PlanStatus.Open]: 'badge-open',
      [PlanStatus.Submitted]: 'badge-submitted',
      [PlanStatus.UnderReview]: 'badge-review',
      [PlanStatus.TDApproved]: 'badge-td-approved',
      [PlanStatus.THApproved]: 'badge-th-approved',
    } as Record<number, string>)[status] ?? 'badge-draft';
  }
}
