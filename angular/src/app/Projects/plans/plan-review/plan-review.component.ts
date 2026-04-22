import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { LocalizationPipe } from '@abp/ng.core';
import { ActivatedRoute } from '@angular/router';
import { FinancialItemService } from 'src/app/proxy/training/finance';
import { TrainingPlanService, TrainingPlanItemService, PlanItemFinancialItemService } from 'src/app/proxy/training/plans';
import { TrainingPlanItemDto, TrainingPlanDto, PlanItemFinancialItemDto, PlanItemConditionDto } from 'src/app/proxy/training/plans/dtos';
import { PlanStatus, TrainingLocalizationHelper } from '../../shared';

interface FinancialItemGroup {
  parentNameAr: string;
  parentCode: string;
  children: { id: string; nameAr: string; code: string }[];
}

interface UnitGroup {
  unitId: string;
  unitName: string;
  allItems: TrainingPlanItemDto[];
  totalItems: number;
  itemsWithCost: number;
  itemsMissingCost: number;
  totalCost: number;
  isComplete: boolean;
}

@Component({
  standalone: true,
  selector: 'app-plan-review',
  templateUrl: './plan-review.component.html',
  styleUrls: ['./plan-review.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, LocalizationPipe],
})
export class PlanReviewComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private planService = inject(TrainingPlanService);
  private itemService = inject(TrainingPlanItemService);
  private fiService = inject(PlanItemFinancialItemService);
  private financialItemService = inject(FinancialItemService);
  l = inject(TrainingLocalizationHelper);

  planId = '';
  plan = signal<TrainingPlanDto | null>(null);
  allItems = signal<TrainingPlanItemDto[]>([]);

  // Expanded item detail — only one at a time
  expandedItemId = signal<string | null>(null);
  // Cache: itemId → financial items (so we don't reload on re-expand)
  financialItemsMap = signal(new Map<string, PlanItemFinancialItemDto[]>());
  conditionsMap = signal(new Map<string, PlanItemConditionDto[]>());
  loadingItemId = signal<string | null>(null);

  // Accordion state
  expandedUnits = signal(new Set<string>());
  unitPages = signal(new Map<string, number>());
  unitCostFilter = signal(new Map<string, string>());
  readonly PAGE_SIZE = 10;

  // Global filters
  filterUnit = signal('');
  filterCourseType = signal<string>('');
  filterCostStatus = signal<string>('');

  // Add financial item dialog
  isAddFiOpen = signal(false);
  fiFiId = signal('');
  fiAmountOMR = signal(0);
  fiNotes = signal('');

  // Batch
  batchFilling = signal(false);
  batchProgress = signal('');

  // Grouped dropdown
  groupedFinancialItems = signal<FinancialItemGroup[]>([]);

  PlanStatus = PlanStatus;

  // ── Computed ──
  unitGroups = computed<UnitGroup[]>(() => {
    let items = this.allItems();
    if (this.filterCourseType()) items = items.filter(i => i.courseType === +this.filterCourseType());
    if (this.filterCostStatus() === 'missing') items = items.filter(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0));
    else if (this.filterCostStatus() === 'assigned') items = items.filter(i => i.courseType === 0 || (i.estimatedCost && i.estimatedCost > 0));
    if (this.filterUnit()) items = items.filter(i => i.unitId === this.filterUnit());

    const map = new Map<string, UnitGroup>();
    for (const item of items) {
      const uid = item.unitId ?? 'unknown';
      const uname = item.unitName ?? 'غير محدد';
      if (!map.has(uid)) map.set(uid, { unitId: uid, unitName: uname, allItems: [], totalItems: 0, itemsWithCost: 0, itemsMissingCost: 0, totalCost: 0, isComplete: true });
      const g = map.get(uid)!;
      g.allItems.push(item);
      g.totalItems++;
      if (item.courseType !== 0) {
        if (item.estimatedCost && item.estimatedCost > 0) { g.itemsWithCost++; g.totalCost += item.estimatedCost; }
        else { g.itemsMissingCost++; g.isComplete = false; }
      } else { g.itemsWithCost++; }
    }
    return Array.from(map.values()).sort((a, b) => a.unitName.localeCompare(b.unitName, 'ar'));
  });

  totalItems = computed(() => this.allItems().length);
  itemsWithCostCount = computed(() => this.allItems().filter(i => i.courseType === 0 || (i.estimatedCost && i.estimatedCost > 0)).length);
  itemsMissingCostCount = computed(() => this.allItems().filter(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0)).length);
  totalCost = computed(() => this.allItems().reduce((s, i) => s + (i.estimatedCost ?? 0), 0));
  completionPercent = computed(() => { const t = this.totalItems(); return t === 0 ? 0 : Math.round((this.itemsWithCostCount() / t) * 100); });
  uniqueUnits = computed(() => {
    const u = new Map<string, string>();
    for (const i of this.allItems()) if (i.unitId && i.unitName) u.set(i.unitId, i.unitName);
    return Array.from(u.entries()).map(([id, name]) => ({ id, name }));
  });

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('planId') ?? '';
    this.loadPlan();
    this.loadItems();
    this.loadAvailableFinancialItems();
  }

  async loadPlan(): Promise<void> { this.plan.set(await firstValueFrom(this.planService.get(this.planId))); }

  async loadItems(): Promise<void> {
    const r = await firstValueFrom(this.itemService.getList({ planId: this.planId, maxResultCount: 1000 }));
    this.allItems.set(r.items ?? []);
  }

  async loadAvailableFinancialItems(): Promise<void> {
    const r = await firstValueFrom(this.financialItemService.getList({ maxResultCount: 200, isActive: true }));
    const all = r.items ?? [];
    const parents = all.filter((fi: any) => !fi.parentId);
    const children = all.filter((fi: any) => fi.parentId);
    this.groupedFinancialItems.set(
      parents.map((p: any) => ({
        parentNameAr: p.nameAr, parentCode: p.code,
        children: children.filter((c: any) => c.parentId === p.id).map((c: any) => ({ id: c.id, nameAr: c.nameAr, code: c.code })),
      })).filter(g => g.children.length > 0)
    );
  }

  // ── Toggle item detail (inline expand/collapse) ──
  async toggleItemDetail(itemId: string): Promise<void> {
    if (this.expandedItemId() === itemId) {
      this.expandedItemId.set(null);
      return;
    }

    this.expandedItemId.set(itemId);
    this.loadingItemId.set(itemId);

    // Load data if not cached
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

  isItemExpanded(itemId: string): boolean { return this.expandedItemId() === itemId; }
  isItemLoading(itemId: string): boolean { return this.loadingItemId() === itemId; }

  getFinancialItemsFor(itemId: string): PlanItemFinancialItemDto[] {
    return this.financialItemsMap().get(itemId) ?? [];
  }

  getConditionsFor(itemId: string): PlanItemConditionDto[] {
    return this.conditionsMap().get(itemId) ?? [];
  }

  getItemById(itemId: string): TrainingPlanItemDto | undefined {
    return this.allItems().find(i => i.id === itemId);
  }

  getFinancialTotalFor(itemId: string): number {
    return this.getFinancialItemsFor(itemId).reduce((s, i) => s + (i.estimatedAmountOMR ?? 0), 0);
  }

  getFinancialTotalUSDFor(itemId: string): number {
    return this.getFinancialItemsFor(itemId).reduce((s, i) => s + (i.estimatedAmountUSD ?? 0), 0);
  }

  // ── Refresh financial items for expanded item ──
  private async refreshExpandedItem(itemId: string): Promise<void> {
    const fis = await firstValueFrom(this.fiService.getListByPlanItem(itemId));
    this.financialItemsMap.update(m => { const n = new Map(m); n.set(itemId, fis); return n; });
    await this.loadItems(); // Refresh totals
  }

  // ── Inline editing ──
  async onAmountChanged(fiId: string, val: number, itemId: string): Promise<void> {
    await firstValueFrom(this.fiService.updateAmount(fiId, { estimatedAmountOMR: val }));
    await this.refreshExpandedItem(itemId);
  }

  async onNotesChanged(fiId: string, val: string): Promise<void> {
    await firstValueFrom(this.fiService.updateNotes(fiId, { notes: val }));
  }

  // ── Auto-fill single item ──
  async onAutoFill(itemId: string): Promise<void> {
    await firstValueFrom(this.fiService.autoFillFromDefaults(itemId));
    await this.refreshExpandedItem(itemId);
  }

  // ── Delete financial item ──
  async onDeleteFi(fiId: string, itemId: string): Promise<void> {
    await firstValueFrom(this.fiService.delete(fiId));
    await this.refreshExpandedItem(itemId);
  }

  // ── Add financial item dialog ──
  private addFiTargetItemId = '';

  openAddFiDialog(itemId: string): void {
    this.addFiTargetItemId = itemId;
    this.fiFiId.set('');
    this.fiAmountOMR.set(0);
    this.fiNotes.set('');
    this.isAddFiOpen.set(true);
  }

  async onSaveFi(): Promise<void> {
    if (!this.addFiTargetItemId || !this.fiFiId()) return;
    await firstValueFrom(this.fiService.create({
      planItemId: this.addFiTargetItemId,
      financialItemId: this.fiFiId(),
      estimatedAmountOMR: this.fiAmountOMR(),
      notes: this.fiNotes() || undefined,
    }));
    this.isAddFiOpen.set(false);
    await this.refreshExpandedItem(this.addFiTargetItemId);
  }

  // ── Unit accordion ──
  toggleUnit(unitId: string): void {
    const s = new Set(this.expandedUnits());
    if (s.has(unitId)) s.delete(unitId); else s.add(unitId);
    this.expandedUnits.set(s);
  }
  isUnitExpanded(unitId: string): boolean { return this.expandedUnits().has(unitId); }
  expandAll(): void { this.expandedUnits.set(new Set(this.unitGroups().map(g => g.unitId))); }
  collapseAll(): void { this.expandedUnits.set(new Set()); this.expandedItemId.set(null); }

  // ── Pagination ──
  getUnitPage(unitId: string): number { return this.unitPages().get(unitId) ?? 0; }
  setUnitPage(unitId: string, page: number): void { this.unitPages.update(m => { const n = new Map(m); n.set(unitId, page); return n; }); }

  getPagedItems(items: TrainingPlanItemDto[], unitId: string): TrainingPlanItemDto[] {
    const cf = this.unitCostFilter().get(unitId) ?? '';
    let filtered = items;
    if (cf === 'missing') filtered = items.filter(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0));
    else if (cf === 'assigned') filtered = items.filter(i => i.courseType === 0 || (i.estimatedCost && i.estimatedCost > 0));
    const page = this.getUnitPage(unitId);
    return filtered.slice(page * this.PAGE_SIZE, (page + 1) * this.PAGE_SIZE);
  }

  getFilteredCount(items: TrainingPlanItemDto[], unitId: string): number {
    const cf = this.unitCostFilter().get(unitId) ?? '';
    if (cf === 'missing') return items.filter(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0)).length;
    if (cf === 'assigned') return items.filter(i => i.courseType === 0 || (i.estimatedCost && i.estimatedCost > 0)).length;
    return items.length;
  }

  getTotalPages(items: TrainingPlanItemDto[], unitId: string): number {
    return Math.ceil(this.getFilteredCount(items, unitId) / this.PAGE_SIZE);
  }

  setUnitCostFilter(unitId: string, value: string): void {
    this.unitCostFilter.update(m => { const n = new Map(m); n.set(unitId, value); return n; });
    this.setUnitPage(unitId, 0);
  }

  // ── Jump to next missing ──
  async jumpToNextMissing(): Promise<void> {
    const missing = this.allItems().find(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0));
    if (!missing) return;
    if (missing.unitId) {
      this.expandedUnits.update(s => { const n = new Set(s); n.add(missing.unitId!); return n; });
      const group = this.unitGroups().find(g => g.unitId === missing.unitId);
      if (group) { const idx = group.allItems.indexOf(missing); if (idx >= 0) this.setUnitPage(missing.unitId!, Math.floor(idx / this.PAGE_SIZE)); }
    }
    await this.toggleItemDetail(missing.id);
    setTimeout(() => document.getElementById('item-' + missing.id)?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 150);
  }

  // ── Batch auto-fill unit ──
  async batchAutoFillUnit(unitId: string, items: TrainingPlanItemDto[]): Promise<void> {
    const ext = items.filter(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0));
    if (ext.length === 0) return;
    this.batchFilling.set(true);
    let done = 0;
    for (const item of ext) {
      done++;
      this.batchProgress.set(`${done} / ${ext.length}`);
      await firstValueFrom(this.fiService.autoFillFromDefaults(item.id));
      // Clear cache so it reloads on next expand
      this.financialItemsMap.update(m => { const n = new Map(m); n.delete(item.id); return n; });
    }
    this.batchFilling.set(false);
    this.batchProgress.set('');
    await this.loadItems();
  }

  // ── Workflow ──
  async onCloseWindow(): Promise<void> { await firstValueFrom(this.planService.closeSubmissionWindow(this.planId)); await this.loadPlan(); }
  async onSubmitForReview(): Promise<void> { await firstValueFrom(this.planService.submitForReview(this.planId)); await this.loadPlan(); }

  // ── Helpers ──
  getCourseTypeBadge(t: number): string { return ({ 0: 'badge-internal', 1: 'badge-ext-local', 2: 'badge-ext-intl' } as Record<number, string>)[t] ?? ''; }
  getCourseTypeText(t: number): string { return ({ 0: 'داخلية', 1: 'خارجية محلية', 2: 'خارجية دولية' } as Record<number, string>)[t] ?? ''; }
  getQuarterText(q: number): string { return ({ 1: 'الربع الأول', 2: 'الربع الثاني', 3: 'الربع الثالث', 4: 'الربع الرابع' } as Record<number, string>)[q] ?? ''; }
  getConditionTypeName(t: number): string { return ({ 0: 'الرتبة', 1: 'العمر', 2: 'سنوات الخدمة', 3: 'المؤهل', 4: 'لياقة طبية', 5: 'تصريح أمني', 6: 'لغة', 7: 'دورة سابقة', 8: 'مخصص' } as Record<number, string>)[t] ?? ''; }
  isMissingCost(i: TrainingPlanItemDto): boolean { return i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0); }
  formatCost(n?: number): string { if (!n || n <= 0) return ''; return n.toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 }); }
  formatDate(d?: string): string { if (!d) return '—'; return new Date(d).toLocaleDateString('ar-OM'); }
  getDurationText(i: TrainingPlanItemDto): string { const p: string[] = []; if (i.durationYears > 0) p.push(`${i.durationYears} سنة`); if (i.durationMonths > 0) p.push(`${i.durationMonths} شهر`); if (i.durationDays > 0) p.push(`${i.durationDays} يوم`); return p.length ? p.join(' و ') : '—'; }
  getPageArray(count: number): number[] { return Array.from({ length: count }, (_, i) => i); }
}
