import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { FinancialItemService } from 'src/app/proxy/training/finance/financial-item.service';
import { FinancialItemRankAmountService } from 'src/app/proxy/training/finance/financial-item-rank-amount.service';
import {
  FinancialItemDto,
  CreateUpdateFinancialItemDto,
  FinancialItemRankAmountDto,
  CreateUpdateFinancialItemRankAmountDto,
} from 'src/app/proxy/training/finance/dtos';
import { HrLookupService } from 'src/app/proxy/training/hr-integration/hr-lookup.service';
import { RankLookupDto } from 'src/app/proxy/training/hr-integration/models';
import { TrainingLocalizationHelper } from '../../shared';

interface ParentItemView extends FinancialItemDto {
  children: FinancialItemDto[];
}

@Component({
  selector: 'app-financial-items',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './financial-items.component.html',
  styleUrls: ['./financial-items.component.scss', '../../shared/gtms-design.scss'],
})
export class FinancialItemsComponent implements OnInit {
  private fiService = inject(FinancialItemService);
  private rankAmountService = inject(FinancialItemRankAmountService);
  private hrService = inject(HrLookupService);
  readonly l = inject(TrainingLocalizationHelper);

  items = signal<FinancialItemDto[]>([]);
  ranks = signal<RankLookupDto[]>([]);
  loading = signal(false);

  searchText = signal('');
  filterActive = signal<'all' | 'active' | 'inactive'>('active');
  filterPerDay = signal<'all' | 'yes' | 'no'>('all');
  filterPerNominee = signal<'all' | 'yes' | 'no'>('all');

  // Rank-overrides inline panel (any row — parent or child)
  expandedItemId = signal<string | null>(null);
  rankAmountsMap = signal(new Map<string, FinancialItemRankAmountDto[]>());
  loadingItemId = signal<string | null>(null);

  // Parent-children tree expansion (independent of rank-overrides)
  expandedParentIds = signal<Set<string>>(new Set());

  // Item dialog
  isDialogOpen = signal(false);
  isEditMode = signal(false);
  editingId = signal<string | null>(null);

  fParentId = signal<string>('');
  fNameAr = signal('');
  fNameEn = signal('');
  fVoteCode = signal('');
  fIsActive = signal(true);
  fDefaultAmount = signal(0);
  fIsPerDay = signal(false);
  fIsPerNominee = signal(false);
  fExtraDaysBefore = signal(0);
  fExtraDaysAfter = signal(0);

  // Rank amount dialog
  isRankDialogOpen = signal(false);
  rankDialogItemId = signal<string | null>(null);
  editingRankAmountId = signal<string | null>(null);
  rFRankId = signal('');
  rFAmount = signal(0);

  parentItems = computed(() => this.items().filter(i => !i.parentId));

  parentsWithChildren = computed<ParentItemView[]>(() => {
    const all = this.items();
    const parents = all.filter(i => !i.parentId);
    const childrenByParent = new Map<string, FinancialItemDto[]>();
    all.filter(i => i.parentId).forEach(c => {
      const arr = childrenByParent.get(c.parentId!) ?? [];
      arr.push(c);
      childrenByParent.set(c.parentId!, arr);
    });
    return parents.map(p => ({ ...p, children: childrenByParent.get(p.id!) ?? [] }));
  });

  filteredParentsWithChildren = computed<ParentItemView[]>(() => {
    const q = this.searchText().trim().toLowerCase();
    const result: ParentItemView[] = [];
    for (const p of this.parentsWithChildren()) {
      const parentMatchesSearch = this.matchesSearch(p, q);
      const parentPassesFilters = this.matchesNonSearchFilters(p);
      const matchingChildren = p.children.filter(c =>
        this.matchesNonSearchFilters(c) && this.matchesSearch(c, q),
      );

      if (parentPassesFilters && parentMatchesSearch) {
        const visibleChildren = p.children.filter(c => this.matchesNonSearchFilters(c));
        result.push({ ...p, children: visibleChildren });
      } else if (matchingChildren.length > 0) {
        result.push({ ...p, children: matchingChildren });
      }
    }
    return result;
  });

  totalCount = computed(() => this.items().length);
  perDayCount = computed(() => this.items().filter(i => i.isPerDay).length);
  perNomineeCount = computed(() => this.items().filter(i => i.isPerNominee).length);
  activeCount = computed(() => this.items().filter(i => i.isActive).length);

  private matchesSearch(i: FinancialItemDto, q: string): boolean {
    if (!q) return true;
    return (
      (i.nameAr ?? '').toLowerCase().includes(q) ||
      (i.nameEn ?? '').toLowerCase().includes(q) ||
      (i.code ?? '').toLowerCase().includes(q) ||
      (i.voteCode ?? '').toLowerCase().includes(q)
    );
  }

  private matchesNonSearchFilters(i: FinancialItemDto): boolean {
    const act = this.filterActive();
    const day = this.filterPerDay();
    const nom = this.filterPerNominee();
    if (act === 'active' && !i.isActive) return false;
    if (act === 'inactive' && i.isActive) return false;
    if (day === 'yes' && !i.isPerDay) return false;
    if (day === 'no' && i.isPerDay) return false;
    if (nom === 'yes' && !i.isPerNominee) return false;
    if (nom === 'no' && i.isPerNominee) return false;
    return true;
  }

  ngOnInit(): void {
    this.loadData();
    this.loadRanks();
  }

  async loadData(): Promise<void> {
    this.loading.set(true);
    try {
      const r = await firstValueFrom(
        this.fiService.getList({ maxResultCount: 1000, skipCount: 0, sorting: 'voteCode' }),
      );
      this.items.set(r.items ?? []);
    } finally {
      this.loading.set(false);
    }
  }

  async loadRanks(): Promise<void> {
    const list = await firstValueFrom(this.hrService.getAllRanks());
    this.ranks.set(
      (list ?? []).slice().sort((a, b) => (a.sortOrder ?? 999) - (b.sortOrder ?? 999)),
    );
  }

  // ── Filter handlers ──
  onSearchInput(event: Event): void { this.searchText.set((event.target as HTMLInputElement).value); }
  setFilterActive(v: 'all' | 'active' | 'inactive'): void { this.filterActive.set(v); }
  setFilterPerDay(v: 'all' | 'yes' | 'no'): void { this.filterPerDay.set(v); }
  setFilterPerNominee(v: 'all' | 'yes' | 'no'): void { this.filterPerNominee.set(v); }

  // ── Item dialog ──
  onAdd(): void {
    this.isEditMode.set(false);
    this.editingId.set(null);
    this.resetForm();
    this.isDialogOpen.set(true);
  }

  onEdit(item: FinancialItemDto, event: Event): void {
    event.stopPropagation();
    this.isEditMode.set(true);
    this.editingId.set(item.id);
    this.fParentId.set(item.parentId ?? '');
    this.fNameAr.set(item.nameAr ?? '');
    this.fNameEn.set(item.nameEn ?? '');
    this.fVoteCode.set(item.voteCode ?? '');
    this.fIsActive.set(item.isActive ?? true);
    this.fDefaultAmount.set(item.defaultAmountOMR ?? 0);
    this.fIsPerDay.set(item.isPerDay ?? false);
    this.fIsPerNominee.set(item.isPerNominee ?? false);
    this.fExtraDaysBefore.set(item.extraDaysBefore ?? 0);
    this.fExtraDaysAfter.set(item.extraDaysAfter ?? 0);
    this.isDialogOpen.set(true);
  }

  private resetForm(): void {
    this.fParentId.set('');
    this.fNameAr.set('');
    this.fNameEn.set('');
    this.fVoteCode.set('');
    this.fIsActive.set(true);
    this.fDefaultAmount.set(0);
    this.fIsPerDay.set(false);
    this.fIsPerNominee.set(false);
    this.fExtraDaysBefore.set(0);
    this.fExtraDaysAfter.set(0);
  }

  async onSave(): Promise<void> {
    if (!this.fNameAr().trim() || !this.fVoteCode().trim()) return;
    const data: CreateUpdateFinancialItemDto = {
      parentId: this.fParentId() || undefined,
      nameAr: this.fNameAr().trim(),
      nameEn: this.fNameEn().trim() || undefined,
      voteCode: this.fVoteCode().trim(),
      isActive: this.fIsActive(),
      defaultAmountOMR: this.fDefaultAmount(),
      isPerDay: this.fIsPerDay(),
      isPerNominee: this.fIsPerNominee(),
      extraDaysBefore: this.fIsPerDay() ? this.fExtraDaysBefore() : 0,
      extraDaysAfter: this.fIsPerDay() ? this.fExtraDaysAfter() : 0,
    };
    if (this.isEditMode() && this.editingId()) {
      await firstValueFrom(this.fiService.update(this.editingId()!, data));
    } else {
      await firstValueFrom(this.fiService.create(data));
    }
    this.isDialogOpen.set(false);
    await this.loadData();
  }

  async onDelete(id: string, event: Event): Promise<void> {
    event.stopPropagation();
    if (!confirm('هل أنت متأكد من حذف هذا البند المالي؟')) return;
    await firstValueFrom(this.fiService.delete(id));
    if (this.expandedItemId() === id) this.expandedItemId.set(null);
    await this.loadData();
  }

  // ── Parent tree expansion ──
  onParentRowClick(parent: ParentItemView, event: Event): void {
    // Empty parents have no children to toggle — fall back to rank-overrides expansion
    if (parent.children.length === 0) {
      void this.toggleExpand(parent.id!, event);
      return;
    }
    this.toggleParent(parent.id!, event);
  }

  toggleParent(parentId: string, event: Event): void {
    event.stopPropagation();
    this.expandedParentIds.update(set => {
      const next = new Set(set);
      if (next.has(parentId)) next.delete(parentId);
      else next.add(parentId);
      return next;
    });
  }

  isParentExpanded(parentId: string): boolean {
    if (this.expandedParentIds().has(parentId)) return true;
    // Auto-expand when a child matches the search but the parent itself doesn't
    const q = this.searchText().trim().toLowerCase();
    if (!q) return false;
    const parent = this.parentsWithChildren().find(p => p.id === parentId);
    if (!parent) return false;
    if (this.matchesSearch(parent, q) && this.matchesNonSearchFilters(parent)) return false;
    return parent.children.some(c =>
      this.matchesNonSearchFilters(c) && this.matchesSearch(c, q),
    );
  }

  // ── Rank-overrides row expansion ──
  async toggleExpand(itemId: string, event: Event): Promise<void> {
    event.stopPropagation();
    if (this.expandedItemId() === itemId) { this.expandedItemId.set(null); return; }
    this.expandedItemId.set(itemId);
    this.loadingItemId.set(itemId);
    if (!this.rankAmountsMap().has(itemId)) await this.reloadRankAmounts(itemId);
    this.loadingItemId.set(null);
  }

  private async reloadRankAmounts(itemId: string): Promise<void> {
    const list = await firstValueFrom(this.rankAmountService.getByFinancialItem(itemId));
    this.rankAmountsMap.update(m => {
      const next = new Map(m);
      next.set(itemId, (list ?? []).slice().sort((a, b) => {
        const ra = this.ranks().find(r => r.id === a.rankId);
        const rb = this.ranks().find(r => r.id === b.rankId);
        return (ra?.sortOrder ?? 999) - (rb?.sortOrder ?? 999);
      }));
      return next;
    });
  }

  isExpanded(id: string): boolean { return this.expandedItemId() === id; }
  isRowLoading(id: string): boolean { return this.loadingItemId() === id; }
  getRankAmountsFor(id: string): FinancialItemRankAmountDto[] { return this.rankAmountsMap().get(id) ?? []; }

  // ── Rank amount dialog ──
  openAddRankDialog(itemId: string, event: Event): void {
    event.stopPropagation();
    this.rankDialogItemId.set(itemId);
    this.editingRankAmountId.set(null);
    this.rFRankId.set('');
    this.rFAmount.set(0);
    this.isRankDialogOpen.set(true);
  }

  openEditRankDialog(rank: FinancialItemRankAmountDto, event: Event): void {
    event.stopPropagation();
    this.rankDialogItemId.set(rank.financialItemId ?? null);
    this.editingRankAmountId.set(rank.id);
    this.rFRankId.set(rank.rankId ?? '');
    this.rFAmount.set(rank.amountOMR ?? 0);
    this.isRankDialogOpen.set(true);
  }

  async onSaveRankAmount(): Promise<void> {
    const itemId = this.rankDialogItemId();
    if (!itemId || !this.rFRankId()) return;
    const data: CreateUpdateFinancialItemRankAmountDto = {
      financialItemId: itemId,
      rankId: this.rFRankId(),
      amountOMR: this.rFAmount(),
    };
    if (this.editingRankAmountId()) {
      await firstValueFrom(this.rankAmountService.update(this.editingRankAmountId()!, data));
    } else {
      await firstValueFrom(this.rankAmountService.create(data));
    }
    this.isRankDialogOpen.set(false);
    await this.reloadRankAmounts(itemId);
  }

  async onDeleteRankAmount(rankAmountId: string, itemId: string, event: Event): Promise<void> {
    event.stopPropagation();
    if (!confirm('حذف هذا المبلغ للرتبة؟')) return;
    await firstValueFrom(this.rankAmountService.delete(rankAmountId));
    await this.reloadRankAmounts(itemId);
  }

  availableRanksForDialog(): RankLookupDto[] {
    const itemId = this.rankDialogItemId();
    if (!itemId) return this.ranks();
    const existing = this.getRankAmountsFor(itemId).map(r => r.rankId);
    const editing = this.editingRankAmountId();
    return this.ranks().filter(r => {
      if (existing.includes(r.id)) {
        const match = this.getRankAmountsFor(itemId).find(ra => ra.rankId === r.id);
        return editing && match?.id === editing;
      }
      return true;
    });
  }

  // ── Formula preview ──
  formulaExample(item: FinancialItemDto): string {
    const rate = item.defaultAmountOMR ?? 0;
    const sampleDays = 5;
    const sampleCount = 3;
    const eb = item.extraDaysBefore ?? 0;
    const ea = item.extraDaysAfter ?? 0;
    const days = item.isPerDay ? sampleDays + eb + ea : 1;
    const count = item.isPerNominee ? sampleCount : 1;
    const subtotal = rate * days * count;
    const parts: string[] = [];
    parts.push(`${rate.toFixed(3)} ر.ع`);
    if (item.isPerDay) {
      const daysPart = eb || ea ? `(${sampleDays} + ${eb} + ${ea})` : `${sampleDays}`;
      parts.push(`× ${daysPart} يوم`);
    }
    if (item.isPerNominee) parts.push(`× ${sampleCount} مرشح`);
    return `${parts.join(' ')} = ${subtotal.toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 })} ر.ع`;
  }

  // ── Helpers ──
  formatMoney(n?: number): string {
    if (n == null) return '—';
    return n.toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 });
  }

  getRankName(rankId?: string): string {
    const r = this.ranks().find(x => x.id === rankId);
    return r?.nameAr ?? '—';
  }

  getParentName(parentId?: string | null): string {
    if (!parentId) return '—';
    const p = this.items().find(x => x.id === parentId);
    return p?.nameAr ?? '—';
  }

  trackById(_: number, i: FinancialItemDto): string { return i.id ?? ''; }
  trackRankById(_: number, r: FinancialItemRankAmountDto): string { return r.id ?? ''; }
}
