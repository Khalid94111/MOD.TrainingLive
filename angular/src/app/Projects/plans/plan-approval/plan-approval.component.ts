import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { PermissionService } from '@abp/ng.core';
import { ActivatedRoute } from '@angular/router';
import {
  TrainingPlanService,
  TrainingPlanItemService,
  PlanItemFinancialItemService,
  PlanItemFinancialItemRankService,
} from 'src/app/proxy/training/plans';
import {
  TrainingPlanDto,
  TrainingPlanItemDto,
  PlanItemFinancialItemDto,
  PlanItemConditionDto,
  PlanItemFinancialItemRankDto,
} from 'src/app/proxy/training/plans/dtos';
import { NominationService } from 'src/app/proxy/training/nominations/nomination.service';
import { NominationDto } from 'src/app/proxy/training/nominations/dtos';
import { FinancialItemService } from 'src/app/proxy/training/finance/financial-item.service';
import { FinancialItemDto } from 'src/app/proxy/training/finance/dtos';
import { PlanNoteEntityType } from 'src/app/proxy/training/enums/plan-note-entity-type.enum';
import {
  NotesDrawerComponent,
  PlanStatus,
  ReturnModalComponent,
} from '../../shared';

@Component({
  standalone: true,
  selector: 'app-plan-approval',
  templateUrl: './plan-approval.component.html',
  styleUrls: ['./plan-approval.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, NotesDrawerComponent, ReturnModalComponent],
})
export class PlanApprovalComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private planService = inject(TrainingPlanService);
  private itemService = inject(TrainingPlanItemService);
  private fiService = inject(PlanItemFinancialItemService);
  private rankBreakdownService = inject(PlanItemFinancialItemRankService);
  private nominationService = inject(NominationService);
  private financialItemService = inject(FinancialItemService);
  private permissionService = inject(PermissionService);

  planId = '';
  plan = signal<TrainingPlanDto | null>(null);
  items = signal<TrainingPlanItemDto[]>([]);
  allFinancialItems = signal<Map<string, FinancialItemDto>>(new Map());

  // Expand state
  expandedItemId = signal<string | null>(null);
  financialItemsMap = signal(new Map<string, PlanItemFinancialItemDto[]>());
  conditionsMap = signal(new Map<string, PlanItemConditionDto[]>());
  ranksMap = signal(new Map<string, PlanItemFinancialItemRankDto[]>());
  nominationsMap = signal(new Map<string, NominationDto[]>());
  loadingItemId = signal<string | null>(null);

  // Reject
  isRejectOpen = signal(false);
  rejectReason = signal('');

  // Return modal
  returnModalOpen = signal(false);

  // Notes drawer
  notesOpen = signal(false);
  notesEntityType = signal<PlanNoteEntityType>(PlanNoteEntityType.Plan);
  notesEntityId = signal<string>('');
  notesTitle = signal<string>('');

  approving = signal(false);
  approveError = signal<string | null>(null);

  canApprove = false;
  canFinalApprove = false;
  PlanStatus = PlanStatus;
  PlanNoteEntityType = PlanNoteEntityType;

  // Stats
  totalItems = computed(() => this.items().length);
  totalCost = computed(() => this.items().reduce((s, i) => s + (i.estimatedCost ?? 0), 0));
  internalCount = computed(() => this.items().filter(i => i.courseType === 0).length);
  extLocalCount = computed(() => this.items().filter(i => i.courseType === 1).length);
  extIntlCount = computed(() => this.items().filter(i => i.courseType === 2).length);
  itemsMissingCost = computed(() => this.items().filter(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0)).length);
  returnedItemsCount = computed(() => this.items().filter(i => i.isReturned).length);
  returnedNominationsCount = computed(() => {
    let count = 0;
    for (const list of this.nominationsMap().values()) count += list.filter(n => n.isReturned).length;
    return count;
  });
  hasUnresolvedReturns = computed(() => this.returnedItemsCount() > 0 || this.returnedNominationsCount() > 0);
  totalNominees = computed(() => this.items().reduce((s, i) => s + (i.nomineesCount ?? 0), 0));

  get showActions(): boolean {
    const p = this.plan();
    if (!p) return false;
    return (p.status === PlanStatus.UnderReview && this.canApprove)
        || (p.status === PlanStatus.TDApproved && this.canFinalApprove);
  }

  get canExecuteApprove(): boolean {
    return this.showActions
      && this.itemsMissingCost() === 0
      && !this.hasUnresolvedReturns();
  }

  get approveLabel(): string {
    return this.plan()?.status === PlanStatus.TDApproved ? '✅ الاعتماد النهائي (TH)' : '✅ اعتماد (TD)';
  }

  get disabledApproveTitle(): string {
    if (this.itemsMissingCost() > 0) return 'يجب إدخال التكلفة لجميع البنود الخارجية';
    if (this.hasUnresolvedReturns()) return 'لا يمكن الاعتماد — يوجد بنود/ترشيحات مُعادة';
    return '';
  }

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('planId') ?? '';
    this.canApprove = this.permissionService.getGrantedPolicy('Training.TrainingPlan.Approve');
    this.canFinalApprove = this.permissionService.getGrantedPolicy('Training.TrainingPlan.FinalApprove');
    this.loadData();
    this.loadAvailableFinancialItems();
  }

  async loadData(): Promise<void> {
    const [plan, r] = await Promise.all([
      firstValueFrom(this.planService.get(this.planId)),
      firstValueFrom(this.itemService.getList({ planId: this.planId, maxResultCount: 1000 })),
    ]);
    this.plan.set(plan);
    this.items.set(r.items ?? []);
  }

  async loadAvailableFinancialItems(): Promise<void> {
    const r = await firstValueFrom(this.financialItemService.getList({ maxResultCount: 500, isActive: true }));
    const byId = new Map<string, FinancialItemDto>();
    for (const fi of (r.items ?? [])) if (fi.id) byId.set(fi.id, fi);
    this.allFinancialItems.set(byId);
  }

  // ── Expand ──
  async toggleItemDetail(itemId: string): Promise<void> {
    if (this.expandedItemId() === itemId) { this.expandedItemId.set(null); return; }
    this.expandedItemId.set(itemId);
    this.loadingItemId.set(itemId);

    if (!this.financialItemsMap().has(itemId)) {
      const [fis, conds, noms] = await Promise.all([
        firstValueFrom(this.fiService.getListByPlanItem(itemId)),
        firstValueFrom(this.itemService.getConditions(itemId)),
        firstValueFrom(this.nominationService.getList({ planItemId: itemId, maxResultCount: 500 })),
      ]);
      this.financialItemsMap.update(m => { const n = new Map(m); n.set(itemId, fis); return n; });
      this.conditionsMap.update(m => { const n = new Map(m); n.set(itemId, conds); return n; });
      this.nominationsMap.update(m => { const n = new Map(m); n.set(itemId, noms.items ?? []); return n; });

      await Promise.all(
        (fis ?? [])
          .filter(fi => this.isPerNominee(fi.financialItemId))
          .map(async fi => {
            const ranks = await firstValueFrom(this.rankBreakdownService.getListByPifi(fi.id!));
            this.ranksMap.update(m => { const n = new Map(m); n.set(fi.id!, ranks ?? []); return n; });
          }),
      );
    }
    this.loadingItemId.set(null);
  }

  isItemExpanded(id: string): boolean { return this.expandedItemId() === id; }
  isItemLoading(id: string): boolean { return this.loadingItemId() === id; }
  getFinancialItemsFor(id: string): PlanItemFinancialItemDto[] { return this.financialItemsMap().get(id) ?? []; }
  getConditionsFor(id: string): PlanItemConditionDto[] { return this.conditionsMap().get(id) ?? []; }
  getRanksFor(pifiId: string): PlanItemFinancialItemRankDto[] { return this.ranksMap().get(pifiId) ?? []; }
  getNominationsFor(id: string): NominationDto[] { return this.nominationsMap().get(id) ?? []; }
  getFinancialTotalFor(id: string): number { return this.getFinancialItemsFor(id).reduce((s, i) => s + (i.estimatedAmountOMR ?? 0), 0); }

  isPerDay(financialItemId?: string): boolean {
    if (!financialItemId) return false;
    return this.allFinancialItems().get(financialItemId)?.isPerDay ?? false;
  }
  isPerNominee(financialItemId?: string): boolean {
    if (!financialItemId) return false;
    return this.allFinancialItems().get(financialItemId)?.isPerNominee ?? false;
  }

  // ── Workflow ──
  async onApprove(): Promise<void> {
    if (!this.canExecuteApprove || this.approving()) return;
    this.approving.set(true);
    this.approveError.set(null);
    try {
      if (this.plan()?.status === PlanStatus.UnderReview) {
        await firstValueFrom(this.planService.approve(this.planId));
      } else {
        await firstValueFrom(this.planService.finalApprove(this.planId));
      }
      await this.loadData();
    } catch (e: any) {
      this.approveError.set(e?.error?.error?.message ?? e?.message ?? 'تعذّر الاعتماد');
    } finally {
      this.approving.set(false);
    }
  }

  // ── Return plan (with modal for reason) ──
  openReturnPlanModal(): void { this.returnModalOpen.set(true); }
  async onReturnConfirmed(): Promise<void> { this.returnModalOpen.set(false); await this.loadData(); }
  onReturnCancelled(): void { this.returnModalOpen.set(false); }

  // ── Reject ──
  openRejectDialog(): void { this.rejectReason.set(''); this.isRejectOpen.set(true); }
  async onConfirmReject(): Promise<void> {
    if (!this.rejectReason().trim()) return;
    await firstValueFrom(this.planService.reject(this.planId, this.rejectReason()));
    this.isRejectOpen.set(false);
    await this.loadData();
  }

  // ── Notes ──
  openPlanNotes(): void {
    this.notesEntityType.set(PlanNoteEntityType.Plan);
    this.notesEntityId.set(this.planId);
    this.notesTitle.set('ملاحظات الخطة');
    this.notesOpen.set(true);
  }

  openItemNotes(item: TrainingPlanItemDto, event: Event): void {
    event.stopPropagation();
    this.notesEntityType.set(PlanNoteEntityType.PlanItem);
    this.notesEntityId.set(item.id!);
    this.notesTitle.set('ملاحظات البند — ' + (item.tenantCourseNameAr ?? ''));
    this.notesOpen.set(true);
  }

  closeNotes(): void { this.notesOpen.set(false); }

  // ── Helpers ──
  getCourseTypeBadge(t?: number): string { return ({ 0: 'badge-internal', 1: 'badge-ext-local', 2: 'badge-ext-intl' } as Record<number, string>)[t as number] ?? ''; }
  getCourseTypeText(t?: number): string { return ({ 0: 'داخلية', 1: 'خارجية محلية', 2: 'خارجية دولية' } as Record<number, string>)[t as number] ?? ''; }
  getQuarterText(q?: number): string { return ({ 1: 'الربع الأول', 2: 'الربع الثاني', 3: 'الربع الثالث', 4: 'الربع الرابع' } as Record<number, string>)[q as number] ?? ''; }
  getConditionTypeName(t?: number): string { return ({ 0: 'الرتبة', 1: 'العمر', 2: 'سنوات الخدمة', 3: 'المؤهل', 4: 'لياقة طبية', 5: 'تصريح أمني', 6: 'لغة', 7: 'دورة سابقة', 8: 'مخصص' } as Record<number, string>)[t as number] ?? ''; }
  isMissingCost(i: TrainingPlanItemDto): boolean { return i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0); }
  formatCost(n?: number): string { if (!n || n <= 0) return ''; return n.toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 }); }
  formatDate(d?: string | null): string { if (!d) return '—'; return new Date(d).toLocaleDateString('ar-OM'); }
  getDurationText(i: TrainingPlanItemDto): string {
    const p: string[] = [];
    if ((i.durationYears ?? 0) > 0) p.push(`${i.durationYears} سنة`);
    if ((i.durationMonths ?? 0) > 0) p.push(`${i.durationMonths} شهر`);
    if ((i.durationDays ?? 0) > 0) p.push(`${i.durationDays} يوم`);
    return p.length ? p.join(' و ') : '—';
  }
  getStatusText(s?: PlanStatus): string {
    return ({
      [PlanStatus.Draft]: 'مسودة',
      [PlanStatus.Open]: 'مفتوحة',
      [PlanStatus.Submitted]: 'مُرسلة',
      [PlanStatus.UnderReview]: 'قيد المراجعة',
      [PlanStatus.ReturnedToCreator]: 'مُعادة للمُنشئ',
      [PlanStatus.TDApproved]: 'معتمدة TD',
      [PlanStatus.THApproved]: 'معتمدة نهائياً',
      [PlanStatus.Closed]: 'مغلقة',
      [PlanStatus.Rejected]: 'مرفوضة',
    } as Record<number, string>)[s as number] ?? '—';
  }
  getStatusBadge(s?: PlanStatus): string {
    return ({
      [PlanStatus.Draft]: 'badge-draft',
      [PlanStatus.Open]: 'badge-open',
      [PlanStatus.Submitted]: 'badge-submitted',
      [PlanStatus.UnderReview]: 'badge-review',
      [PlanStatus.ReturnedToCreator]: 'badge-submitted',
      [PlanStatus.TDApproved]: 'badge-td-approved',
      [PlanStatus.THApproved]: 'badge-th-approved',
      [PlanStatus.Closed]: 'badge-draft',
      [PlanStatus.Rejected]: 'badge-rejected',
    } as Record<number, string>)[s as number] ?? 'badge-draft';
  }

  trackById(_: number, x: { id?: string }): string { return x.id ?? ''; }
}
