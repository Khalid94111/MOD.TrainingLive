import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import {  PermissionService } from '@abp/ng.core';
import { ActivatedRoute } from '@angular/router';
import { TrainingPlanService, TrainingPlanItemService, PlanItemFinancialItemService } from 'src/app/proxy/training/plans';
import { TrainingPlanDto, TrainingPlanItemDto, PlanItemFinancialItemDto, PlanItemConditionDto } from 'src/app/proxy/training/plans/dtos';
import { PlanStatus } from '../../shared';

@Component({
  standalone: true,
  selector: 'app-plan-approval',
  templateUrl: './plan-approval.component.html',
  styleUrls: ['./plan-approval.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule],
})
export class PlanApprovalComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private planService = inject(TrainingPlanService);
  private itemService = inject(TrainingPlanItemService);
  private fiService = inject(PlanItemFinancialItemService);
  private permissionService = inject(PermissionService);
  l = inject(TrainingPlanItemService);

  planId = '';
  plan = signal<TrainingPlanDto | null>(null);
  items = signal<TrainingPlanItemDto[]>([]);

  // Inline expand
  expandedItemId = signal<string | null>(null);
  financialItemsMap = signal(new Map<string, PlanItemFinancialItemDto[]>());
  conditionsMap = signal(new Map<string, PlanItemConditionDto[]>());
  loadingItemId = signal<string | null>(null);

  // Reject dialog
  isRejectOpen = signal(false);
  rejectReason = signal('');

  canApprove = false;
  canFinalApprove = false;
  PlanStatus = PlanStatus;

  // Stats
  totalItems = computed(() => this.items().length);
  totalCost = computed(() => this.items().reduce((s, i) => s + (i.estimatedCost ?? 0), 0));
  internalCount = computed(() => this.items().filter(i => i.courseType === 0).length);
  extLocalCount = computed(() => this.items().filter(i => i.courseType === 1).length);
  extIntlCount = computed(() => this.items().filter(i => i.courseType === 2).length);
  itemsMissingCost = computed(() => this.items().filter(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0)).length);

  get showActions(): boolean {
    const p = this.plan();
    if (!p) return false;
    return (p.status === PlanStatus.UnderReview && this.canApprove) ||
           (p.status === PlanStatus.TDApproved && this.canFinalApprove);
  }

  get approveLabel(): string {
    return this.plan()?.status === PlanStatus.TDApproved ? '✅ الاعتماد النهائي (TH)' : '✅ اعتماد (TD)';
  }

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('planId') ?? '';
    this.canApprove = this.permissionService.getGrantedPolicy('Training.TrainingPlan.Approve');
    this.canFinalApprove = this.permissionService.getGrantedPolicy('Training.TrainingPlan.FinalApprove');
    this.loadData();
  }

  async loadData(): Promise<void> {
    const [plan, r] = await Promise.all([
      firstValueFrom(this.planService.get(this.planId)),
      firstValueFrom(this.itemService.getList({ planId: this.planId, maxResultCount: 1000 })),
    ]);
    this.plan.set(plan);
    this.items.set(r.items ?? []);
  }

  // ── Inline expand ──
  async toggleItemDetail(itemId: string): Promise<void> {
    if (this.expandedItemId() === itemId) { this.expandedItemId.set(null); return; }
    this.expandedItemId.set(itemId);
    this.loadingItemId.set(itemId);
    if (!this.financialItemsMap().has(itemId)) {
      const [fis, conds] = await Promise.all([
        firstValueFrom(this.fiService.getListByPlanItem(itemId)),
        firstValueFrom(this.itemService.getConditions(itemId)),
      ]);
      this.financialItemsMap.update(m => { const n = new Map(m); n.set(itemId, fis); return n; });
      this.conditionsMap.update(m => { const n = new Map(m); n.set(itemId, conds); return n; });
    }
    this.loadingItemId.set(null);
  }

  isItemExpanded(id: string): boolean { return this.expandedItemId() === id; }
  isItemLoading(id: string): boolean { return this.loadingItemId() === id; }
  getFinancialItemsFor(id: string): PlanItemFinancialItemDto[] { return this.financialItemsMap().get(id) ?? []; }
  getConditionsFor(id: string): PlanItemConditionDto[] { return this.conditionsMap().get(id) ?? []; }
  getFinancialTotalFor(id: string): number { return this.getFinancialItemsFor(id).reduce((s, i) => s + (i.estimatedAmountOMR ?? 0), 0); }
  getFinancialTotalUSDFor(id: string): number { return this.getFinancialItemsFor(id).reduce((s, i) => s + (i.estimatedAmountUSD ?? 0), 0); }

  // ── Workflow ──
  async onApprove(): Promise<void> {
    if (this.plan()?.status === PlanStatus.UnderReview) await firstValueFrom(this.planService.approve(this.planId));
    else await firstValueFrom(this.planService.finalApprove(this.planId));
    await this.loadData();
  }

  async onReturn(): Promise<void> { await firstValueFrom(this.planService.returnToStaff(this.planId, '')); await this.loadData(); }
  openRejectDialog(): void { this.rejectReason.set(''); this.isRejectOpen.set(true); }
  async onConfirmReject(): Promise<void> { await firstValueFrom(this.planService.reject(this.planId, this.rejectReason())); this.isRejectOpen.set(false); await this.loadData(); }

  // ── Helpers ──
  getCourseTypeBadge(t: number): string { return ({ 0: 'badge-internal', 1: 'badge-ext-local', 2: 'badge-ext-intl' } as Record<number, string>)[t] ?? ''; }
  getCourseTypeText(t: number): string { return ({ 0: 'داخلية', 1: 'خارجية محلية', 2: 'خارجية دولية' } as Record<number, string>)[t] ?? ''; }
  getQuarterText(q: number): string { return ({ 1: 'الربع الأول', 2: 'الربع الثاني', 3: 'الربع الثالث', 4: 'الربع الرابع' } as Record<number, string>)[q] ?? ''; }
  getConditionTypeName(t: number): string { return ({ 0: 'الرتبة', 1: 'العمر', 2: 'سنوات الخدمة', 3: 'المؤهل', 4: 'لياقة طبية', 5: 'تصريح أمني', 6: 'لغة', 7: 'دورة سابقة', 8: 'مخصص' } as Record<number, string>)[t] ?? ''; }
  isMissingCost(i: TrainingPlanItemDto): boolean { return i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0); }
  formatCost(n?: number): string { if (!n || n <= 0) return ''; return n.toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 }); }
  formatDate(d?: string): string { if (!d) return '—'; return new Date(d).toLocaleDateString('ar-OM'); }
  getDurationText(i: TrainingPlanItemDto): string { const p: string[] = []; if (i.durationYears > 0) p.push(`${i.durationYears} سنة`); if (i.durationMonths > 0) p.push(`${i.durationMonths} شهر`); if (i.durationDays > 0) p.push(`${i.durationDays} يوم`); return p.length ? p.join(' و ') : '—'; }
  getStatusText(s: PlanStatus): string { return ({ [PlanStatus.Draft]: 'مسودة', [PlanStatus.Open]: 'مفتوحة', [PlanStatus.Submitted]: 'مُرسلة', [PlanStatus.UnderReview]: 'قيد المراجعة', [PlanStatus.TDApproved]: 'معتمدة TD', [PlanStatus.THApproved]: 'معتمدة نهائياً' } as Record<number, string>)[s] ?? ''; }
  getStatusBadge(s: PlanStatus): string { return ({ [PlanStatus.Draft]: 'badge-draft', [PlanStatus.Open]: 'badge-open', [PlanStatus.Submitted]: 'badge-submitted', [PlanStatus.UnderReview]: 'badge-review', [PlanStatus.TDApproved]: 'badge-td-approved', [PlanStatus.THApproved]: 'badge-th-approved' } as Record<number, string>)[s] ?? 'badge-draft'; }
}
