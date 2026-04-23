import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { FinancialItemService } from 'src/app/proxy/training/finance/financial-item.service';
import {
  TrainingPlanService,
  TrainingPlanItemService,
  PlanItemFinancialItemService,
  PlanItemFinancialItemRankService,
} from 'src/app/proxy/training/plans';
import {
  TrainingPlanItemDto,
  TrainingPlanDto,
  PlanItemFinancialItemDto,
  PlanItemConditionDto,
  PlanItemFinancialItemRankDto,
} from 'src/app/proxy/training/plans/dtos';
import { NominationService } from 'src/app/proxy/training/nominations/nomination.service';
import { NominationDto } from 'src/app/proxy/training/nominations/dtos';
import { FinancialItemDto } from 'src/app/proxy/training/finance/dtos';
import { PlanNoteEntityType } from 'src/app/proxy/training/enums/plan-note-entity-type.enum';
import {
  NotesDrawerComponent,
  PlanStatus,
  ReturnModalComponent,
  TrainingLocalizationHelper,
} from '../../shared';

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
  imports: [CommonModule, NotesDrawerComponent, ReturnModalComponent],
})
export class PlanReviewComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private planService = inject(TrainingPlanService);
  private itemService = inject(TrainingPlanItemService);
  private fiService = inject(PlanItemFinancialItemService);
  private rankBreakdownService = inject(PlanItemFinancialItemRankService);
  private financialItemService = inject(FinancialItemService);
  private nominationService = inject(NominationService);
  l = inject(TrainingLocalizationHelper);

  planId = '';
  plan = signal<TrainingPlanDto | null>(null);
  allItems = signal<TrainingPlanItemDto[]>([]);
  allFinancialItems = signal<Map<string, FinancialItemDto>>(new Map());

  // Expanded item detail
  expandedItemId = signal<string | null>(null);
  financialItemsMap = signal(new Map<string, PlanItemFinancialItemDto[]>());
  conditionsMap = signal(new Map<string, PlanItemConditionDto[]>());
  ranksMap = signal(new Map<string, PlanItemFinancialItemRankDto[]>());
  nominationsMap = signal(new Map<string, NominationDto[]>());
  loadingItemId = signal<string | null>(null);

  // Accordion
  expandedUnits = signal(new Set<string>());
  unitPages = signal(new Map<string, number>());
  unitCostFilter = signal(new Map<string, string>());
  readonly PAGE_SIZE = 10;

  // Filters
  filterUnit = signal('');
  filterCourseType = signal<string>('');
  filterCostStatus = signal<string>('');

  // Add Fi dialog
  isAddFiOpen = signal(false);
  fiFiId = signal('');
  fiAmountOMR = signal(0);
  fiNotes = signal('');
  private addFiTargetItemId = '';

  // Rate update flash
  flashingRankId = signal<string | null>(null);

  // Batch
  batchFilling = signal(false);
  batchProgress = signal('');

  groupedFinancialItems = signal<FinancialItemGroup[]>([]);

  // Return modal
  returnModalOpen = signal(false);
  returnModalEntityType = signal<PlanNoteEntityType>(PlanNoteEntityType.Plan);
  returnModalEntityId = signal<string>('');
  returnModalTitle = signal<string>('');
  returnModalSubtitle = signal<string>('');

  // Notes drawer
  notesOpen = signal(false);
  notesEntityType = signal<PlanNoteEntityType>(PlanNoteEntityType.Plan);
  notesEntityId = signal<string>('');
  notesTitle = signal<string>('');

  // Approving
  approving = signal(false);
  approveError = signal<string | null>(null);

  // Start-review (Submitted → UnderReview)
  startingReview = signal(false);

  PlanStatus = PlanStatus;
  PlanNoteEntityType = PlanNoteEntityType;

  // Plan is read-only for Staff when they can't yet act on it (Submitted) or
  // once it has moved beyond their stage (TD/TH approved). Inline edits,
  // rank rate inputs, return buttons and approve actions all gate on this.
  isReadOnly = computed(() => {
    const s = this.plan()?.status;
    return s === PlanStatus.Submitted
      || s === PlanStatus.TDApproved
      || s === PlanStatus.THApproved;
  });

  // ── Computed ──
  unitGroups = computed<UnitGroup[]>(() => {
    let items = this.allItems();
    if (this.filterCourseType()) items = items.filter(i => i.courseType === +this.filterCourseType());
    if (this.filterCostStatus() === 'missing') items = items.filter(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0));
    else if (this.filterCostStatus() === 'assigned') items = items.filter(i => i.courseType === 0 || ((i.estimatedCost ?? 0) > 0));
    if (this.filterUnit()) items = items.filter(i => i.unitId === this.filterUnit());

    const map = new Map<string, UnitGroup>();
    for (const item of items) {
      console.log('Processing item', item
        , 'with unit', item.unitId, item.unitName);
      const uid = item.unitId ?? 'unknown';
      const uname = item.unitName ?? 'غير محدد';
      if (!map.has(uid)) map.set(uid, { unitId: uid, unitName: uname, allItems: [], totalItems: 0, itemsWithCost: 0, itemsMissingCost: 0, totalCost: 0, isComplete: true });
      const g = map.get(uid)!;
      g.allItems.push(item);
      g.totalItems++;
      if (item.courseType !== 0) {
        if ((item.estimatedCost ?? 0) > 0) { g.itemsWithCost++; g.totalCost += item.estimatedCost!; }
        else { g.itemsMissingCost++; g.isComplete = false; }
      } else { g.itemsWithCost++; }
    }
    var sss= Array.from(map.values()).sort((a, b) => a.unitName.localeCompare(b.unitName, 'ar'));
    console.log(sss); 
    return sss;
  });

  totalItems = computed(() => this.allItems().length);
  itemsWithCostCount = computed(() => this.allItems().filter(i => i.courseType === 0 || ((i.estimatedCost ?? 0) > 0)).length);
  itemsMissingCostCount = computed(() => this.allItems().filter(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0)).length);
  totalCost = computed(() => this.allItems().reduce((s, i) => s + (i.estimatedCost ?? 0), 0));
  completionPercent = computed(() => { const t = this.totalItems(); return t === 0 ? 0 : Math.round((this.itemsWithCostCount() / t) * 100); });
  uniqueUnits = computed(() => {
    const u = new Map<string, string>();
    for (const i of this.allItems()) if (i.unitId && i.unitName) u.set(i.unitId, i.unitName);
    return Array.from(u.entries()).map(([id, name]) => ({ id, name }));
  });

  returnedItemsCount = computed(() => this.allItems().filter(i => i.isReturned).length);
  returnedNominationsCount = computed(() => {
    let count = 0;
    for (const list of this.nominationsMap().values()) count += list.filter(n => n.isReturned).length;
    return count;
  });
  hasUnresolvedReturns = computed(() => this.returnedItemsCount() > 0 || this.returnedNominationsCount() > 0);

  canStaffApprove = computed(() =>
    this.plan()?.status === PlanStatus.UnderReview
    && this.itemsMissingCostCount() === 0
    && !this.hasUnresolvedReturns(),
  );

  canStartReview = computed(() => this.plan()?.status === PlanStatus.Submitted);

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('planId') ?? '';
    this.loadPlan();
    this.loadItems();
    this.loadAvailableFinancialItems();
  }

  async loadPlan(): Promise<void> { this.plan.set(await firstValueFrom(this.planService.get(this.planId))); }

  async loadItems(): Promise<void> {
    const r = await firstValueFrom(this.itemService.getList({ planId: this.planId, maxResultCount: 1000 }));
    console.log('Loaded items', r.items);
    this.allItems.set(r.items ?? []);
  }

  async loadAvailableFinancialItems(): Promise<void> {
    const r = await firstValueFrom(this.financialItemService.getList({ maxResultCount: 500, isActive: true }));
    const all = r.items ?? [];

    // Cache by ID for flag lookup (IsPerDay / IsPerNominee)
    const byId = new Map<string, FinancialItemDto>();
    for (const fi of all) if (fi.id) byId.set(fi.id, fi);
    this.allFinancialItems.set(byId);

    const parents = all.filter(fi => !fi.parentId);
    const children = all.filter(fi => fi.parentId);
    this.groupedFinancialItems.set(
      parents.map(p => ({
        parentNameAr: p.nameAr ?? '',
        parentCode: p.code ?? '',
        children: children.filter(c => c.parentId === p.id).map(c => ({ id: c.id!, nameAr: c.nameAr ?? '', code: c.code ?? '' })),
      })).filter(g => g.children.length > 0),
    );
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

  getItemById(id: string): TrainingPlanItemDto | undefined { return this.allItems().find(i => i.id === id); }

  isPerDay(financialItemId?: string): boolean {
    if (!financialItemId) return false;
    return this.allFinancialItems().get(financialItemId)?.isPerDay ?? false;
  }

  isPerNominee(financialItemId?: string): boolean {
    if (!financialItemId) return false;
    return this.allFinancialItems().get(financialItemId)?.isPerNominee ?? false;
  }

  getExtraDays(financialItemId?: string): { before: number; after: number } {
    const fi = financialItemId ? this.allFinancialItems().get(financialItemId) : undefined;
    return { before: fi?.extraDaysBefore ?? 0, after: fi?.extraDaysAfter ?? 0 };
  }

  computeEffectiveDays(item: TrainingPlanItemDto, financialItemId?: string): number {
    if (!this.isPerDay(financialItemId)) return 1;
    const extra = this.getExtraDays(financialItemId);
    return (item.durationDays ?? 0) + extra.before + extra.after;
  }

  getFinancialTotalFor(itemId: string): number {
    return this.getFinancialItemsFor(itemId).reduce((s, i) => s + (i.estimatedAmountOMR ?? 0), 0);
  }

  // ── Refresh ──
  private async refreshExpandedItem(itemId: string): Promise<void> {
    const fis = await firstValueFrom(this.fiService.getListByPlanItem(itemId));
    this.financialItemsMap.update(m => { const n = new Map(m); n.set(itemId, fis); return n; });

    await Promise.all(
      (fis ?? [])
        .filter(fi => this.isPerNominee(fi.financialItemId))
        .map(async fi => {
          const ranks = await firstValueFrom(this.rankBreakdownService.getListByPifi(fi.id!));
          this.ranksMap.update(m => { const n = new Map(m); n.set(fi.id!, ranks ?? []); return n; });
        }),
    );
    await this.loadItems();
  }

  // ── Inline edits ──
  async onRateChanged(rankRow: PlanItemFinancialItemRankDto, rawValue: string, itemId: string): Promise<void> {
    const newRate = +rawValue || 0;
    if (rankRow.ratePerUnitOMR === newRate) return;
    try {
      await firstValueFrom(this.rankBreakdownService.updateRate(rankRow.id!, { newRate }));
      this.flashingRankId.set(rankRow.id!);
      setTimeout(() => this.flashingRankId.set(null), 700);
      await this.refreshExpandedItem(itemId);
    } catch (e) {
      await this.refreshExpandedItem(itemId);
    }
  }

  async onAmountChanged(fiId: string, val: number, itemId: string): Promise<void> {
    await firstValueFrom(this.fiService.updateAmount(fiId, { estimatedAmountOMR: val }));
    await this.refreshExpandedItem(itemId);
  }

  async onNotesChanged(fiId: string, val: string): Promise<void> {
    await firstValueFrom(this.fiService.updateNotes(fiId, { notes: val }));
  }

  async onAutoFill(itemId: string): Promise<void> {
    await firstValueFrom(this.fiService.autoFillFromDefaults(itemId));
    await this.refreshExpandedItem(itemId);
  }

  async onDeleteFi(fiId: string, itemId: string): Promise<void> {
    if (!confirm('حذف هذا البند المالي؟')) return;
    await firstValueFrom(this.fiService.delete(fiId));
    this.ranksMap.update(m => { const n = new Map(m); n.delete(fiId); return n; });
    await this.refreshExpandedItem(itemId);
  }

  openAddFiDialog(itemId: string): void {
    this.addFiTargetItemId = itemId;
    this.fiFiId.set(''); this.fiAmountOMR.set(0); this.fiNotes.set('');
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

  // ── Return actions ──
  openReturnPlanModal(): void {
    this.returnModalEntityType.set(PlanNoteEntityType.Plan);
    this.returnModalEntityId.set(this.planId);
    this.returnModalTitle.set('إعادة الخطة إلى مُنشئها');
    this.returnModalSubtitle.set('الخطة ستعود للـ UTM للتعديل');
    this.returnModalOpen.set(true);
  }

  openReturnItemModal(item: TrainingPlanItemDto, event: Event): void {
    event.stopPropagation();
    this.returnModalEntityType.set(PlanNoteEntityType.PlanItem);
    this.returnModalEntityId.set(item.id!);
    this.returnModalTitle.set('إعادة بند للمُنشئ');
    this.returnModalSubtitle.set(item.tenantCourseNameAr ?? '');
    this.returnModalOpen.set(true);
  }

  openReturnNominationModal(n: NominationDto, event: Event): void {
    event.stopPropagation();
    this.returnModalEntityType.set(PlanNoteEntityType.Nomination);
    this.returnModalEntityId.set(n.id!);
    this.returnModalTitle.set('إعادة ترشيح');
    this.returnModalSubtitle.set(n.employeeName ?? '');
    this.returnModalOpen.set(true);
  }

  async onReturnConfirmed(): Promise<void> {
    this.returnModalOpen.set(false);
    await this.loadPlan();
    await this.loadItems();
    const expanded = this.expandedItemId();
    if (expanded) await this.refreshExpandedItem(expanded);
  }

  onReturnCancelled(): void { this.returnModalOpen.set(false); }

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

  openNominationNotes(n: NominationDto, event: Event): void {
    event.stopPropagation();
    this.notesEntityType.set(PlanNoteEntityType.Nomination);
    this.notesEntityId.set(n.id!);
    this.notesTitle.set('ملاحظات الترشيح — ' + (n.employeeName ?? ''));
    this.notesOpen.set(true);
  }

  closeNotes(): void { this.notesOpen.set(false); }

  // ── Workflow ──
  async onCloseWindow(): Promise<void> {
    await firstValueFrom(this.planService.closeSubmissionWindow(this.planId));
    await this.loadPlan();
  }

  // ── Staff approve ──
  async onStaffApprove(): Promise<void> {
    if (!this.canStaffApprove() || this.approving()) return;
    this.approving.set(true);
    this.approveError.set(null);
    try {
      await firstValueFrom(this.planService.approve(this.planId));
      await this.loadPlan();
    } catch (e: any) {
      this.approveError.set(e?.error?.error?.message ?? e?.message ?? 'تعذّر اعتماد الخطة');
    } finally {
      this.approving.set(false);
    }
  }

  // ── Start review (Submitted → UnderReview) ──
  async onStartReview(): Promise<void> {
    if (!this.canStartReview() || this.startingReview()) return;
    this.startingReview.set(true);
    this.approveError.set(null);
    try {
      await firstValueFrom(this.planService.submitForReview(this.planId));
      await this.loadPlan();
    } catch (e: any) {
      this.approveError.set(
        e?.error?.error?.message ?? e?.message ?? this.l.t('::Training.Errors.Generic'),
      );
    } finally {
      this.startingReview.set(false);
    }
  }

  // ── Accordion / pagination (unchanged from v5) ──
  toggleUnit(unitId: string): void {
    const s = new Set(this.expandedUnits());
    if (s.has(unitId)) s.delete(unitId); else s.add(unitId);
    this.expandedUnits.set(s);
  }
  isUnitExpanded(unitId: string): boolean { return this.expandedUnits().has(unitId); }
  expandAll(): void { this.expandedUnits.set(new Set(this.unitGroups().map(g => g.unitId))); }
  collapseAll(): void { this.expandedUnits.set(new Set()); this.expandedItemId.set(null); }

  getUnitPage(unitId: string): number { return this.unitPages().get(unitId) ?? 0; }
  setUnitPage(unitId: string, page: number): void { this.unitPages.update(m => { const n = new Map(m); n.set(unitId, page); return n; }); }

  getPagedItems(items: TrainingPlanItemDto[], unitId: string): TrainingPlanItemDto[] {
    const cf = this.unitCostFilter().get(unitId) ?? '';
    let filtered = items;
    if (cf === 'missing') filtered = items.filter(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0));
    else if (cf === 'assigned') filtered = items.filter(i => i.courseType === 0 || ((i.estimatedCost ?? 0) > 0));
    const page = this.getUnitPage(unitId);
    return filtered.slice(page * this.PAGE_SIZE, (page + 1) * this.PAGE_SIZE);
  }

  getFilteredCount(items: TrainingPlanItemDto[], unitId: string): number {
    const cf = this.unitCostFilter().get(unitId) ?? '';
    if (cf === 'missing') return items.filter(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0)).length;
    if (cf === 'assigned') return items.filter(i => i.courseType === 0 || ((i.estimatedCost ?? 0) > 0)).length;
    return items.length;
  }

  getTotalPages(items: TrainingPlanItemDto[], unitId: string): number {
    return Math.ceil(this.getFilteredCount(items, unitId) / this.PAGE_SIZE);
  }

  setUnitCostFilter(unitId: string, value: string): void {
    this.unitCostFilter.update(m => { const n = new Map(m); n.set(unitId, value); return n; });
    this.setUnitPage(unitId, 0);
  }

  async jumpToNextMissing(): Promise<void> {
    const missing = this.allItems().find(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0));
    if (!missing) return;
    if (missing.unitId) {
      this.expandedUnits.update(s => { const n = new Set(s); n.add(missing.unitId!); return n; });
      const group = this.unitGroups().find(g => g.unitId === missing.unitId);
      if (group) { const idx = group.allItems.indexOf(missing); if (idx >= 0) this.setUnitPage(missing.unitId!, Math.floor(idx / this.PAGE_SIZE)); }
    }
    await this.toggleItemDetail(missing.id!);
    setTimeout(() => document.getElementById('item-' + missing.id)?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 150);
  }

  async batchAutoFillUnit(unitId: string, items: TrainingPlanItemDto[]): Promise<void> {
    const ext = items.filter(i => i.courseType !== 0 && (!i.estimatedCost || i.estimatedCost <= 0));
    if (ext.length === 0) return;
    this.batchFilling.set(true);
    let done = 0;
    for (const item of ext) {
      done++;
      this.batchProgress.set(`${done} / ${ext.length}`);
      await firstValueFrom(this.fiService.autoFillFromDefaults(item.id!));
      this.financialItemsMap.update(m => { const n = new Map(m); n.delete(item.id!); return n; });
    }
    this.batchFilling.set(false);
    this.batchProgress.set('');
    await this.loadItems();
  }

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
  getPageArray(count: number): number[] { return Array.from({ length: count }, (_, i) => i); }

  statusText(s?: PlanStatus): string {
    return ({
      [PlanStatus.Draft]: 'مسودة',
      [PlanStatus.Open]: 'مفتوحة',
      [PlanStatus.Submitted]: 'مُرسلة',
      [PlanStatus.UnderReview]: 'قيد المراجعة',
      [PlanStatus.ReturnedToCreator]: 'مُعادة للمُنشئ',
      [PlanStatus.TDApproved]: 'اعتماد TD',
      [PlanStatus.THApproved]: 'اعتماد TH',
      [PlanStatus.Closed]: 'مغلقة',
      [PlanStatus.Rejected]: 'مرفوضة',
    } as Record<number, string>)[s as number] ?? '—';
  }

  statusBadgeClass(s?: PlanStatus): string {
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
