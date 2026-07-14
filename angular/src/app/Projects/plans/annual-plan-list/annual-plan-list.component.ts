import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import {  PermissionService } from '@abp/ng.core';
import { Router } from '@angular/router';
import { TrainingPlanService } from 'src/app/proxy/training/plans';
import { TrainingPlanDto, CreateUpdateTrainingPlanDto } from 'src/app/proxy/training/plans/dtos';
import { PlanStatus, TrainingLocalizationHelper } from '../../shared';
 

@Component({
  standalone: true,
  selector: 'app-annual-plan-list',
  templateUrl: './annual-plan-list.component.html',
  styleUrls: ['./annual-plan-list.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule],
})
export class AnnualPlanListComponent implements OnInit {
  private planService = inject(TrainingPlanService);
  private permissionService = inject(PermissionService);
  private router = inject(Router);
  l = inject(TrainingLocalizationHelper);

  plans = signal<TrainingPlanDto[]>([]);
  isDialogOpen = signal(false);
  isEditMode = signal(false);
  selectedPlanId = signal<string | null>(null);

  formYear = signal(new Date().getFullYear() + 1);
  formOpenDate = signal('');
  formCloseDate = signal('');

  isReopenDialogOpen = signal(false);
  reopenPlanId = signal<string | null>(null);
  reopenOpenDate = signal('');
  reopenCloseDate = signal('');

  // Filters and stats for the modern redesign
  searchText = signal('');
  filterStatus = signal<string>('');

  totalPlans = computed(() => this.plans().length);
  openPlansCount = computed(() => this.plans().filter(p => p.status === PlanStatus.Open).length);
  submittedPlansCount = computed(() => this.plans().filter(p => p.status === PlanStatus.Submitted || p.status === PlanStatus.UnderReview).length);
  approvedPlansCount = computed(() => this.plans().filter(p => p.status === PlanStatus.TDApproved || p.status === PlanStatus.THApproved).length);
  returnedPlansCount = computed(() => this.plans().filter(p => p.status === PlanStatus.ReturnedToCreator).length);

  filteredPlans = computed(() => {
    const q = this.searchText().trim().toLowerCase();
    const st = this.filterStatus();
    return this.plans().filter(p => {
      if (q) {
        const yearMatch = (p.year?.toString() ?? '').includes(q);
        const statusMatch = this.getStatusText(p.status).toLowerCase().includes(q);
        if (!yearMatch && !statusMatch) return false;
      }
      if (st !== '' && p.status !== +st) return false;
      return true;
    });
  });

  canCreate = false;
  canReview = false;
  canApprove = false;
  canFinalApprove = false;

  PlanStatus = PlanStatus;

  ngOnInit(): void {
    this.canCreate = this.permissionService.getGrantedPolicy('Training.TrainingPlan.Create');
    this.canReview = this.permissionService.getGrantedPolicy('Training.TrainingPlan.Review');
    this.canApprove = this.permissionService.getGrantedPolicy('Training.TrainingPlan.Approve');
    this.canFinalApprove = this.permissionService.getGrantedPolicy('Training.TrainingPlan.FinalApprove');
    this.loadPlans();
  }

  async loadPlans(): Promise<void> {
    const result = await firstValueFrom(this.planService.getList({ maxResultCount: 100 }));
    this.plans.set(result.items ?? []);
  }

  openCreateDialog(): void {
    this.isEditMode.set(false);
    this.selectedPlanId.set(null);
    this.formYear.set(new Date().getFullYear() + 1);
    this.formOpenDate.set('');
    this.formCloseDate.set('');
    this.isDialogOpen.set(true);
  }

  async onSave(): Promise<void> {
    const data: CreateUpdateTrainingPlanDto = {
      year: this.formYear(),
      openDate: this.formOpenDate() || undefined,
      closeDate: this.formCloseDate() || undefined,
    };

    if (this.isEditMode() && this.selectedPlanId()) {
      await firstValueFrom(this.planService.update(this.selectedPlanId()!, data));
    } else {
      await firstValueFrom(this.planService.create(data));
    }
    this.isDialogOpen.set(false);
    await this.loadPlans();
  }

  async onDelete(id: string): Promise<void> {
    if (!confirm('هل أنت متأكد من حذف هذه الخطة؟ لا يمكن التراجع عن هذا الإجراء.')) return;
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

  openReopenDialog(plan: TrainingPlanDto): void {
    this.reopenPlanId.set(plan.id);
    this.reopenOpenDate.set(plan.openDate ?? this.formatDateInput(new Date()));
    this.reopenCloseDate.set(plan.closeDate ?? '');
    this.isReopenDialogOpen.set(true);
  }

  async onReopenWindow(): Promise<void> {
    const id = this.reopenPlanId();
    if (!id) return;

    await firstValueFrom(this.planService.reopenSubmissionWindow(id, {
      openDate: this.reopenOpenDate(),
      closeDate: this.reopenCloseDate(),
    }));

    this.isReopenDialogOpen.set(false);
    this.reopenPlanId.set(null);
    await this.loadPlans();
  }

  onReopenCancelled(): void {
    this.isReopenDialogOpen.set(false);
    this.reopenPlanId.set(null);
  }

  formatDateInput(date: Date): string {
    return date.toISOString().split('T')[0];
  }

  navigateToEntry(planId: string): void {
    this.router.navigate(['/training/plans', planId, 'entry']);
  }

  navigateToReview(planId: string): void {
    this.router.navigate(['/training/plans', planId, 'review']);
  }

  navigateToApproval(planId: string): void {
    this.router.navigate(['/training/plans', planId, 'approve']);
  }

  getStatusBadgeClass(status: PlanStatus): string {
    return ({
      [PlanStatus.Draft]: 'badge-draft',
      [PlanStatus.Open]: 'badge-open',
      [PlanStatus.Submitted]: 'badge-submitted',
      [PlanStatus.UnderReview]: 'badge-review',
      [PlanStatus.ReturnedToCreator]: 'badge-returned',
      [PlanStatus.TDApproved]: 'badge-td-approved',
      [PlanStatus.THApproved]: 'badge-th-approved',
      [PlanStatus.Rejected]: 'badge-rejected',
    } as Record<number, string>)[status] ?? 'badge-draft';
  }

  getStatusText(status: PlanStatus): string {
    return ({
      [PlanStatus.Draft]: 'مسودة',
      [PlanStatus.Open]: 'مفتوحة',
      [PlanStatus.Submitted]: 'مُرسلة',
      [PlanStatus.UnderReview]: 'قيد المراجعة',
      [PlanStatus.ReturnedToCreator]: 'مُعادة للمُنشئ',
      [PlanStatus.TDApproved]: 'معتمدة TD',
      [PlanStatus.THApproved]: 'معتمدة نهائياً',
      [PlanStatus.Rejected]: 'مرفوضة',
    } as Record<number, string>)[status] ?? '';
  }

  onSearchInput(event: Event): void { this.searchText.set((event.target as HTMLInputElement).value); }

  clearFilters(): void {
    this.searchText.set('');
    this.filterStatus.set('');
  }

  formatDate(date?: string): string {
    if (!date) return '—';
    return new Date(date).toLocaleDateString('ar-OM');
  }

}
