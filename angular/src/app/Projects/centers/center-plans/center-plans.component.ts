import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe, PermissionService } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';
import { CenterPlanItemDialogComponent } from './center-plan-item-dialog/center-plan-item-dialog.component';
import { CenterPlanItemService, CenterPlanService, CenterPlanWindowService, TrainingCenterService } from 'src/app/proxy/training/centers';
import { CenterPlanStatus } from 'src/app/proxy/training/enums';
import { TrainingCenterPlanDto } from 'src/app/proxy/training/centers/dtos';
import { TrainingLocalizationHelper, ConfirmDialogComponent } from '../../shared';

interface CenterOption {
  id: string;
  centerNameAr?: string;
  centerNameEn?: string;
}

@Component({
  selector: 'app-center-plans',
  standalone: true,
  imports: [CommonModule, LocalizationPipe, CenterPlanItemDialogComponent, ConfirmDialogComponent],
  templateUrl: './center-plans.component.html',
  styleUrl: './center-plans.component.scss',
})
export class CenterPlansComponent implements OnInit {
  private readonly planService = inject(CenterPlanService);
  private readonly windowService = inject(CenterPlanWindowService);
  private readonly centerService = inject(TrainingCenterService);
  private readonly planItemService = inject(CenterPlanItemService);
  private readonly permissionService = inject(PermissionService);
  private readonly toaster = inject(ToasterService);
  private readonly l = inject(TrainingLocalizationHelper);

  // ── Data ──
  plans = signal<TrainingCenterPlanDto[]>([]);
  centers = signal<CenterOption[]>([]);
  isLoading = signal(false);
  totalCount = signal(0);

  // ── Pagination ──
  pageSize = signal(10);
  currentPage = signal(1);

  // ── Filters ──
  filterCenterId = signal<string | null>(null);
  filterYear = signal<number>(new Date().getFullYear());
  filterStatus = signal<CenterPlanStatus | null>(null);

  // ── Window ──
  windowYear = signal(new Date().getFullYear());
  windowOpenDate = signal<Date | null>(null);
  windowCloseDate = signal<Date | null>(null);
  currentWindow = signal<any>(null);
  isWindowSaving = signal(false);
  isCloseWindowDialogVisible = signal(false);

  // ── Create plan dialog ──
  isPlanDialogVisible = signal(false);
  planFormCenterId = signal<string>('');
  planFormYear = signal(new Date().getFullYear());
  isPlanSaving = signal(false);

  // ── Plan item dialog ──
  isItemDialogVisible = signal(false);
  editingPlanId = signal<string>('');
  editingItemId = signal<string | null>(null);

  // ── Delete dialog ──
  isDeleteDialogVisible = signal(false);
  planToDelete = signal<TrainingCenterPlanDto | null>(null);
  itemToDelete = signal<{ planId: string; itemId: string } | null>(null);

  // ── Reason dialog (Reject / Return) ──
  isReasonDialogVisible = signal(false);
  reasonDialogAction = signal<'reject' | 'return' | null>(null);
  reasonText = signal('');
  reasonTargetPlanId = signal<string>('');
  reasonDialogTitle = computed(() =>
    this.reasonDialogAction() === 'reject'
      ? this.l.t('::Training.RejectionReason')
      : this.l.t('::Training.ReturnReason')
  );

  // ── Expanded rows ──
  expandedPlanIds = signal<Set<string>>(new Set());
  planItemsMap = signal<Map<string, any[]>>(new Map());
  loadingPlanItems = signal<Set<string>>(new Set());

  // ── Permissions ──
  canCreatePlan = computed(() => this.permissionService.getGrantedPolicy('Training.CenterPlans.Create'));
  canEditPlan = computed(() => this.permissionService.getGrantedPolicy('Training.CenterPlans.Edit'));
  canDeletePlan = computed(() => this.permissionService.getGrantedPolicy('Training.CenterPlans.Delete'));
  canSubmitPlan = computed(() => this.permissionService.getGrantedPolicy('Training.CenterPlans.Submit'));
  canApprovePlan = computed(() => this.permissionService.getGrantedPolicy('Training.CenterPlans.Approve'));
  canManagePlanItems = computed(() => this.permissionService.getGrantedPolicy('Training.CenterPlanItems.Create'));
  canDeletePlanItems = computed(() => this.permissionService.getGrantedPolicy('Training.CenterPlanItems.Delete'));
  canManageWindows = computed(() => this.permissionService.getGrantedPolicy('Training.Centers.ManageWindows'));

  statusFilterItems: { value: CenterPlanStatus | null; text: string }[] = [];

  readonly CenterPlanStatus = CenterPlanStatus;

  // ── Computed stats ──
  draftCount = computed(() => this.plans().filter(p => p.status === CenterPlanStatus.Draft).length);
  submittedCount = computed(() => this.plans().filter(p => p.status === CenterPlanStatus.Submitted).length);
  approvedCount = computed(() => this.plans().filter(p => p.status === CenterPlanStatus.Approved).length);
  rejectedCount = computed(() => this.plans().filter(p => p.status === CenterPlanStatus.Rejected).length);

  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()) || 1);
  pageNumbers = computed(() => {
    const pages: number[] = [];
    for (let i = 1; i <= this.totalPages(); i++) pages.push(i);
    return pages;
  });

  async ngOnInit(): Promise<void> {
    this.statusFilterItems = [
      { value: null, text: this.l.t('::All') },
      { value: CenterPlanStatus.Draft, text: this.l.t('::Training.CenterPlanStatus.Draft') },
      { value: CenterPlanStatus.Submitted, text: this.l.t('::Training.CenterPlanStatus.Submitted') },
      { value: CenterPlanStatus.Approved, text: this.l.t('::Training.CenterPlanStatus.Approved') },
      { value: CenterPlanStatus.Rejected, text: this.l.t('::Training.CenterPlanStatus.Rejected') },
    ];

    await Promise.all([this.loadCenters(), this.loadCurrentWindow()]);
    await this.loadPlans();
  }

  async loadPlans(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await firstValueFrom(
        this.planService.getList({
          maxResultCount: this.pageSize(),
          skipCount: (this.currentPage() - 1) * this.pageSize(),
          centerId: this.filterCenterId() || undefined,
          year: this.filterYear() || undefined,
          status: this.filterStatus() ?? undefined,
        })
      );
      this.plans.set(result.items ?? []);
      this.totalCount.set(result.totalCount ?? 0);
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
    } finally {
      this.isLoading.set(false);
    }
  }

  private async loadCenters(): Promise<void> {
    try {
      const result = await firstValueFrom(this.centerService.getList({ maxResultCount: 1000, skipCount: 0 }));
      this.centers.set((result.items ?? []) as CenterOption[]);
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
    }
  }

  private async loadCurrentWindow(): Promise<void> {
    try {
      const result = await firstValueFrom(
        this.windowService.getList({ year: this.filterYear(), maxResultCount: 1, skipCount: 0 })
      );
      if (result.items && result.items.length > 0) {
        const w = result.items[0];
        this.currentWindow.set(w);
        this.windowYear.set(w.year ?? this.filterYear());
        this.windowOpenDate.set(w.openDate ? new Date(w.openDate) : null);
        this.windowCloseDate.set(w.closeDate ? new Date(w.closeDate) : null);
      } else {
        this.currentWindow.set(null);
        this.windowYear.set(this.filterYear());
        this.windowOpenDate.set(null);
        this.windowCloseDate.set(null);
      }
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
    }
  }

  // ── Filters ──

  async onSearch(): Promise<void> {
    this.currentPage.set(1);
    await Promise.all([this.loadPlans(), this.loadCurrentWindow()]);
  }

  async onPageChange(page: number): Promise<void> {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    await this.loadPlans();
  }

  // ── Window ──

  setWindowOpenDate(v: Date | string | null): void {
    this.windowOpenDate.set(v ? new Date(v as any) : null);
  }

  setWindowCloseDate(v: Date | string | null): void {
    this.windowCloseDate.set(v ? new Date(v as any) : null);
  }

  async onOpenWindow(): Promise<void> {
    if (!this.windowOpenDate() || !this.windowCloseDate()) {
      this.toaster.warn(this.l.t('::Training.Validation.WindowDatesRequired'));
      return;
    }
    if (this.windowCloseDate()! <= this.windowOpenDate()!) {
      this.toaster.warn(this.l.t('::Training.Validation.CloseDateAfterOpenDate'));
      return;
    }

    this.isWindowSaving.set(true);
    try {
      const payload = {
        year: this.windowYear(),
        openDate: this.windowOpenDate()!.toISOString(),
        closeDate: this.windowCloseDate()!.toISOString(),
      };
      const existing = this.currentWindow();
      if (existing?.id) {
        await firstValueFrom(this.windowService.update(existing.id, payload));
      } else {
        await firstValueFrom(this.windowService.create(payload));
      }
      this.toaster.success(this.l.t('::Training.Common.Save'));
      await this.loadCurrentWindow();
    } catch {
      this.toaster.error(this.l.t('::Training.Errors.Generic'));
    } finally {
      this.isWindowSaving.set(false);
    }
  }

  onCloseWindow(): void {
    this.isCloseWindowDialogVisible.set(true);
  }

  onCancelCloseWindow(): void {
    this.isCloseWindowDialogVisible.set(false);
  }

  async onConfirmCloseWindow(): Promise<void> {
    const window = this.currentWindow();
    if (!window?.id) return;

    this.isWindowSaving.set(true);
    try {
      await firstValueFrom(this.windowService.close(window.id));
      this.toaster.success(this.l.t('::Training.CloseWindowSuccess'));
      this.isCloseWindowDialogVisible.set(false);
      await this.loadCurrentWindow();
    } catch {
      this.toaster.error(this.l.t('::Training.Errors.Generic'));
    } finally {
      this.isWindowSaving.set(false);
    }
  }

  // ── Plan CRUD ──

  onAddPlan(): void {
    this.planFormCenterId.set('');
    this.planFormYear.set(this.filterYear());
    this.isPlanDialogVisible.set(true);
  }

  async onCreatePlan(): Promise<void> {
    if (!this.planFormCenterId()) {
      this.toaster.warn(this.l.t('::Training.Validation.CenterRequired'));
      return;
    }
    this.isPlanSaving.set(true);
    try {
      await firstValueFrom(
        this.planService.create({ centerId: this.planFormCenterId(), year: this.planFormYear() })
      );
      this.toaster.success(this.l.t('::Training.Common.Save'));
      this.isPlanDialogVisible.set(false);
      await this.loadPlans();
    } catch {
      this.toaster.error(this.l.t('::Training.Errors.Generic'));
    } finally {
      this.isPlanSaving.set(false);
    }
  }

  onDeletePlan(plan: TrainingCenterPlanDto): void {
    this.planToDelete.set(plan);
    this.isDeleteDialogVisible.set(true);
  }

  async onConfirmDeletePlan(): Promise<void> {
    const plan = this.planToDelete();
    if (!plan?.id) return;
    try {
      await firstValueFrom(this.planService.delete(plan.id));
      this.toaster.success(this.l.t('::Training.Common.Delete'));
      this.isDeleteDialogVisible.set(false);
      this.planToDelete.set(null);
      await this.loadPlans();
    } catch {
      this.toaster.error(this.l.t('::Training.Errors.Generic'));
    }
  }

  onCancelDelete(): void {
    this.isDeleteDialogVisible.set(false);
    this.planToDelete.set(null);
    this.itemToDelete.set(null);
  }

  // ── Plan actions ──

  async onSubmit(planId: string): Promise<void> {
    try {
      await firstValueFrom(this.planService.submit(planId));
      this.toaster.success(this.l.t('::Training.SubmitForApproval'));
      await this.loadPlans();
    } catch {
      this.toaster.error(this.l.t('::Training.Errors.Generic'));
    }
  }

  async onApprove(planId: string): Promise<void> {
    try {
      await firstValueFrom(this.planService.approve(planId));
      this.toaster.success(this.l.t('::Training.Approve'));
      await this.loadPlans();
    } catch {
      this.toaster.error(this.l.t('::Training.Errors.Generic'));
    }
  }

  onReject(plan: TrainingCenterPlanDto): void {
    if (!plan.id) return;
    this.reasonDialogAction.set('reject');
    this.reasonTargetPlanId.set(plan.id);
    this.reasonText.set(plan.rejectionReason ?? '');
    this.isReasonDialogVisible.set(true);
  }

  onReturn(plan: TrainingCenterPlanDto): void {
    if (!plan.id) return;
    this.reasonDialogAction.set('return');
    this.reasonTargetPlanId.set(plan.id);
    this.reasonText.set(plan.returnReason ?? '');
    this.isReasonDialogVisible.set(true);
  }

  updateReasonText(value: string): void {
    this.reasonText.set(value);
  }

  onCancelReason(): void {
    this.isReasonDialogVisible.set(false);
    this.reasonDialogAction.set(null);
    this.reasonTargetPlanId.set('');
    this.reasonText.set('');
  }

  async onConfirmReason(): Promise<void> {
    const action = this.reasonDialogAction();
    const planId = this.reasonTargetPlanId();
    const reason = this.reasonText().trim();

    if (!action || !planId) return;
    if (!reason) {
      this.toaster.warn(this.l.t('::Training.Validation.ReasonRequired'));
      return;
    }

    try {
      if (action === 'reject') {
        await firstValueFrom(this.planService.reject(planId, { reason }));
        this.toaster.success(this.l.t('::Training.Reject'));
      } else {
        await firstValueFrom(this.planService.return(planId, { reason }));
        this.toaster.success(this.l.t('::Training.Return'));
      }
      this.onCancelReason();
      await this.loadPlans();
    } catch {
      this.toaster.error(this.l.t('::Training.Errors.Generic'));
    }
  }

  // ── Plan items (master-detail) ──

  togglePlanItems(plan: TrainingCenterPlanDto): void {
    if (!plan.id) return;
    this.expandedPlanIds.update(set => {
      const next = new Set(set);
      if (next.has(plan.id!)) {
        next.delete(plan.id!);
      } else {
        next.add(plan.id!);
        this.loadPlanItems(plan.id!);
      }
      return next;
    });
  }

  isPlanExpanded(planId?: string): boolean {
    return !!planId && this.expandedPlanIds().has(planId);
  }

  private async loadPlanItems(planId: string): Promise<void> {
    if (this.planItemsMap().has(planId)) return;
    this.loadingPlanItems.update(s => new Set(s).add(planId));
    try {
      const result = await firstValueFrom(
        this.planItemService.getList({ planId, maxResultCount: 1000, skipCount: 0 } as any)
      );
      this.planItemsMap.update(m => {
        const updated = new Map(m);
        updated.set(planId, result.items ?? []);
        return updated;
      });
    } catch {
      this.toaster.error(this.l.t('::Training.Common.LoadError'));
    } finally {
      this.loadingPlanItems.update(s => {
        const next = new Set(s);
        next.delete(planId);
        return next;
      });
    }
  }

  getPlanItems(planId?: string): any[] {
    return this.planItemsMap().get(planId ?? '') ?? [];
  }

  isPlanItemsLoading(planId?: string): boolean {
    return !!planId && this.loadingPlanItems().has(planId);
  }

  // ── Plan item actions ──

  onAddItem(planId: string): void {
    this.editingPlanId.set(planId);
    this.editingItemId.set(null);
    this.isItemDialogVisible.set(true);
  }

  onEditItem(planId: string, itemId: string): void {
    this.editingPlanId.set(planId);
    this.editingItemId.set(itemId);
    this.isItemDialogVisible.set(true);
  }

  onDeleteItem(planId: string, itemId: string): void {
    this.itemToDelete.set({ planId, itemId });
    this.isDeleteDialogVisible.set(true);
  }

  async onConfirmDeleteItem(): Promise<void> {
    const item = this.itemToDelete();
    if (!item?.itemId) return;
    try {
      await firstValueFrom(this.planItemService.delete(item.itemId));
      this.toaster.success(this.l.t('::Training.Common.Delete'));
      this.isDeleteDialogVisible.set(false);
      this.planItemsMap.update(m => {
        const updated = new Map(m);
        const items = updated.get(item.planId) ?? [];
        updated.set(item.planId, items.filter((i: any) => i.id !== item.itemId));
        return updated;
      });
      this.itemToDelete.set(null);
      await this.loadPlans();
    } catch {
      this.toaster.error(this.l.t('::Training.Errors.Generic'));
    }
  }

  onItemSaved(): void {
    this.isItemDialogVisible.set(false);
    const planId = this.editingPlanId();
    if (planId) {
      this.planItemsMap.update(m => {
        const updated = new Map(m);
        updated.delete(planId);
        return updated;
      });
      if (this.expandedPlanIds().has(planId)) {
        this.loadPlanItems(planId);
      }
    }
    this.loadPlans();
  }

  // ── Display helpers ──

  getStatusText(status?: CenterPlanStatus): string {
    switch (status) {
      case CenterPlanStatus.Draft:
        return this.l.t('::Training.CenterPlanStatus.Draft');
      case CenterPlanStatus.Submitted:
        return this.l.t('::Training.CenterPlanStatus.Submitted');
      case CenterPlanStatus.Approved:
        return this.l.t('::Training.CenterPlanStatus.Approved');
      case CenterPlanStatus.Rejected:
        return this.l.t('::Training.CenterPlanStatus.Rejected');
      default:
        return '';
    }
  }

  getStatusClass(status?: CenterPlanStatus): string {
    switch (status) {
      case CenterPlanStatus.Draft:
        return 'status-pill status-draft';
      case CenterPlanStatus.Submitted:
        return 'status-pill status-pending';
      case CenterPlanStatus.Approved:
        return 'status-pill status-approved';
      case CenterPlanStatus.Rejected:
        return 'status-pill status-rejected';
      default:
        return 'status-pill';
    }
  }

  get windowStatusText(): string {
    const w = this.currentWindow();
    if (!w) return this.l.t('::Training.WindowNotSet');
    return w.isOpen ? this.l.t('::Training.WindowOpen') : this.l.t('::Training.WindowClosed');
  }

  get windowStatusClass(): string {
    const w = this.currentWindow();
    if (!w) return 'status-pill status-draft';
    return w.isOpen ? 'status-pill status-approved' : 'status-pill status-rejected';
  }

  get windowStatusBannerClass(): string {
    const w = this.currentWindow();
    if (!w) return 'banner-notset';
    return w.isOpen ? 'banner-open' : 'banner-closed';
  }

  deleteDialogTitle(): string {
    return this.planToDelete()
      ? this.l.t('::Training.DeletePlanConfirm')
      : this.l.t('::Training.DeletePlanItemConfirm');
  }

  deleteDialogMessage(): string {
    return this.planToDelete()
      ? this.l.t('::Training.DeletePlanConfirmMessage')
      : this.l.t('::Training.DeletePlanItemConfirmMessage');
  }

  deleteTargetLabel(): string {
    const plan = this.planToDelete();
    if (plan) return plan.centerName ?? '';
    return '';
  }

  getCenterName(centerId?: string): string {
    return this.centers().find(c => c.id === centerId)?.centerNameAr ?? '—';
  }
}
