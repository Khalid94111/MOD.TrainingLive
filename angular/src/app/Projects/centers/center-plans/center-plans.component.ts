import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe } from '@abp/ng.core';
import {
  DxDataGridModule,
  DxPopupModule,
  DxSelectBoxModule,
  DxNumberBoxModule,
  DxDateBoxModule,
  DxButtonModule,
  DxDataGridComponent,
} from 'devextreme-angular';
import { ToolbarItem } from 'devextreme/ui/popup';
import { createAbpStore } from '../../shared/helpers/create-abp-store';
import { CenterPlanItemDialogComponent } from './center-plan-item-dialog/center-plan-item-dialog.component';
import { CenterPlanItemService, CenterPlanService, CenterPlanWindowService, TrainingCenterService } from 'src/app/proxy/training/centers';
import { CenterPlanStatus } from 'src/app/proxy/training/enums';
import { TrainingLocalizationHelper } from '../../shared';
 
const centerPlanStatusKeyMap: Record<CenterPlanStatus, string> = {
  [CenterPlanStatus.Draft]: '::Training.CenterPlanStatus.Draft',
  [CenterPlanStatus.Submitted]: '::Training.CenterPlanStatus.Submitted',
  [CenterPlanStatus.Approved]: '::Training.CenterPlanStatus.Approved',
  [CenterPlanStatus.Rejected]: '::Training.CenterPlanStatus.Rejected',
};

@Component({
  selector: 'app-center-plans',
  standalone: true,
  imports: [
    CommonModule,
    LocalizationPipe,
    DxDataGridModule,
    DxPopupModule,
    DxSelectBoxModule,
    DxNumberBoxModule,
    DxDateBoxModule,
    DxButtonModule,
    CenterPlanItemDialogComponent,
  ],
  templateUrl: './center-plans.component.html',
  styleUrl: './center-plans.component.scss',
})
export class CenterPlansComponent implements OnInit {
  private readonly planService = inject(CenterPlanService);
  private readonly windowService = inject(CenterPlanWindowService);
  private readonly centerService = inject(TrainingCenterService);
  private readonly planItemService = inject(CenterPlanItemService);

  readonly l = inject(TrainingLocalizationHelper);

  readonly plansGrid = viewChild<DxDataGridComponent>('plansGrid');

  dataSource!: ReturnType<typeof createAbpStore>;

  filterCenterId = signal<string | null>(null);
  filterYear = signal<number>(new Date().getFullYear());
  filterStatus = signal<CenterPlanStatus | null>(null);

  centers = signal<any[]>([]);

  windowYear = signal(new Date().getFullYear());
  windowOpenDate = signal<Date | null>(null);
  windowCloseDate = signal<Date | null>(null);
  currentWindow = signal<any>(null);

  isPlanDialogVisible = signal(false);
  planFormCenterId = signal<string>('');
  planFormYear = signal(new Date().getFullYear());
  planDialogToolbarItems: ToolbarItem[] | undefined;

  isItemDialogVisible = signal(false);
  editingPlanId = signal<string>('');
  editingItemId = signal<string | null>(null);

  statusFilterItems: any[] = [];
planItemsMap = signal<Map<string, any[]>>(new Map());

  readonly CenterPlanStatus = CenterPlanStatus;

  ngOnInit(): void {
    
    this.statusFilterItems = [
      { value: null, text: this.l.t('::All') },
      { value: CenterPlanStatus.Draft, text: this.l.t('::Training.CenterPlanStatus.Draft') },
      { value: CenterPlanStatus.Submitted, text: this.l.t('::Training.CenterPlanStatus.Submitted') },
      { value: CenterPlanStatus.Approved, text: this.l.t('::Training.CenterPlanStatus.Approved') },
      { value: CenterPlanStatus.Rejected, text: this.l.t('::Training.CenterPlanStatus.Rejected') },
    ];

    this.planDialogToolbarItems = [
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: { text: this.l.t('::Save'), type: 'default', stylingMode: 'contained', onClick: () => this.onCreatePlan() },
      },
      {
        widget: 'dxButton',
        location: 'after',
        toolbar: 'bottom',
        options: { text: this.l.t('::Cancel'), onClick: () => this.isPlanDialogVisible.set(false) },
      },
    ];

    this.loadDataSource();
    this.loadCenters();
    this.loadCurrentWindow();
  }

  getStatusText(status: CenterPlanStatus): string {
    const key = centerPlanStatusKeyMap[status];
    return key ? this.l.t(key) : '';
  }

  getStatusClass(status: CenterPlanStatus): string {
    switch (status) {
      case CenterPlanStatus.Draft: return 'status-draft';
      case CenterPlanStatus.Submitted: return 'status-pending';
      case CenterPlanStatus.Approved: return 'status-approved';
      case CenterPlanStatus.Rejected: return 'status-rejected';
      default: return '';
    }
  }

  get windowStatusText(): string {
    const w = this.currentWindow();
    if (!w) return this.l.t('::Training.WindowNotSet');
    return w.isOpen ? this.l.t('::Training.WindowOpen') : this.l.t('::Training.WindowClosed');
  }

  get windowStatusClass(): string {
    const w = this.currentWindow();
    if (!w) return 'status-draft';
    return w.isOpen ? 'status-approved' : 'status-rejected';
  }

  private loadDataSource(): void {
    this.dataSource = createAbpStore({
      loadFn: (params) => firstValueFrom(this.planService.getList({
        ...params,
        centerId: this.filterCenterId() || undefined,
        year: this.filterYear() || undefined,
        status: this.filterStatus() ?? undefined,
      })),
    });
  }

  private async loadCenters(): Promise<void> {
    const result = await firstValueFrom(this.centerService.getList({ maxResultCount: 100, skipCount: 0 }));
    this.centers.set(result.items || []);
  }

  private async loadCurrentWindow(): Promise<void> {
    const result = await firstValueFrom(this.windowService.getList({ year: this.filterYear(), maxResultCount: 1, skipCount: 0 }));
    if (result.items && result.items.length > 0) {
      const w = result.items[0];
      this.currentWindow.set(w);
      this.windowYear.set(w.year);
      this.windowOpenDate.set(new Date(w.openDate));
      this.windowCloseDate.set(new Date(w.closeDate));
    }
  }

  onFilterChanged(): void {
    this.loadDataSource();
    this.plansGrid()?.instance.refresh();
  }

  async onOpenWindow(): Promise<void> {
    if (!this.windowOpenDate() || !this.windowCloseDate()) return;
    try {
      const payload = { year: this.windowYear(), openDate: this.windowOpenDate()!.toISOString(), closeDate: this.windowCloseDate()!.toISOString() };
      const existing = this.currentWindow();
      if (existing) {
        await firstValueFrom(this.windowService.update(existing.id, payload));
      } else {
        await firstValueFrom(this.windowService.create(payload));
      }
      await this.loadCurrentWindow();
    } catch (e) { /* ABP interceptor */ }
  }

  onAddPlan(): void {
    this.planFormCenterId.set('');
    this.planFormYear.set(this.filterYear());
    this.isPlanDialogVisible.set(true);
  }

  async onCreatePlan(): Promise<void> {
    try {
      await firstValueFrom(this.planService.create({ centerId: this.planFormCenterId(), year: this.planFormYear() }));
      this.isPlanDialogVisible.set(false);
      this.plansGrid()?.instance.refresh();
    } catch (e) { /* ABP interceptor */ }
  }

  async onDeletePlan(planId: string): Promise<void> {
    await firstValueFrom(this.planService.delete(planId));
    this.plansGrid()?.instance.refresh();
  }

  async onSubmit(planId: string): Promise<void> {
    await firstValueFrom(this.planService.submit(planId));
    this.plansGrid()?.instance.refresh();
  }

  async onApprove(planId: string): Promise<void> {
    await firstValueFrom(this.planService.approve(planId));
    this.plansGrid()?.instance.refresh();
  }

  async onReject(planId: string): Promise<void> {
    await firstValueFrom(this.planService.reject(planId));
    this.plansGrid()?.instance.refresh();
  }

  async onReturn(planId: string): Promise<void> {
    await firstValueFrom(this.planService.return(planId));
    this.plansGrid()?.instance.refresh();
  }

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

  onItemSaved(): void {
    this.isItemDialogVisible.set(false);
    this.plansGrid()?.instance.refresh();
  }
async onRowExpanding(e: any): Promise<void> {
  const planId = e.key;
  const result = await firstValueFrom(
    this.planItemService.getList({ planId, maxResultCount: 100, skipCount: 0 } as any)
  );
  console.log('Items for plan', planId, result);
  this.planItemsMap.update(m => {
    const updated = new Map(m);
    updated.set(planId, result.items ?? []);
    return updated;
  });
}

getPlanItems(planId: string): any[] {
  return this.planItemsMap().get(planId) ?? [];
}
}
