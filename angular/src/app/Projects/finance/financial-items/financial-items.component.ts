import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { firstValueFrom } from 'rxjs';
import { FinancialItemService } from 'src/app/proxy/training/finance/financial-item.service';
import { FinancialItemRankAmountService } from 'src/app/proxy/training/finance/financial-item-rank-amount.service';
import {
  FinancialItemDto,
  CreateUpdateFinancialItemDto,
  FinancialItemRankAmountDto,
  CreateUpdateFinancialItemRankAmountDto,
} from 'src/app/proxy/training/finance/dtos';
import { FinancialItemType } from 'src/app/proxy/training/enums/financial-item-type.enum';
import { HrLookupService } from 'src/app/proxy/training/hr-integration/hr-lookup.service';
import { RankLookupDto } from 'src/app/proxy/training/hr-integration/models';
import { TrainingLocalizationHelper } from '../../shared';



const ITEM_TYPE_ORDER: FinancialItemType[] = [
  FinancialItemType.CourseCost,
  FinancialItemType.Ticket,
  FinancialItemType.Allowance,
  FinancialItemType.Clothing,
  FinancialItemType.Insurance,
  FinancialItemType.Visa,
  FinancialItemType.Other,
];

@Component({
  selector: 'app-financial-items',
  standalone: true,
  imports: [CommonModule, LocalizationPipe],
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

  // Rank-overrides inline panel
  expandedItemId = signal<string | null>(null);
  rankAmountsMap = signal(new Map<string, FinancialItemRankAmountDto[]>());
  loadingItemId = signal<string | null>(null);

  // Item dialog
  isDialogOpen = signal(false);
  isEditMode = signal(false);
  editingId = signal<string | null>(null);

  fNameAr = signal('');
  fNameEn = signal('');
  fVoteCode = signal('');
  fIsActive = signal(true);
  fDefaultAmount = signal(0);
  fIsPerDay = signal(false);
  fIsPerNominee = signal(false);
  fExtraDaysBefore = signal(0);
  fExtraDaysAfter = signal(0);
  fItemType = signal<FinancialItemType | null>(null);

  FinancialItemType = FinancialItemType;
  itemTypeOptions = ITEM_TYPE_ORDER;

  // Rank amount dialog
  isRankDialogOpen = signal(false);
  rankDialogItemId = signal<string | null>(null);
  editingRankAmountId = signal<string | null>(null);
  rFRankId = signal('');
  rFAmount = signal(0);

  filteredItems = computed<FinancialItemDto[]>(() => {
    const q = this.searchText().trim().toLowerCase();
    return this.items().filter(i => this.matchesSearch(i, q) && this.matchesNonSearchFilters(i));
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
    this.fNameAr.set(item.nameAr ?? '');
    this.fNameEn.set(item.nameEn ?? '');
    this.fVoteCode.set(item.voteCode ?? '');
    this.fIsActive.set(item.isActive ?? true);
    this.fDefaultAmount.set(item.defaultAmountOMR ?? 0);
    this.fIsPerDay.set(item.isPerDay ?? false);
    this.fIsPerNominee.set(item.isPerNominee ?? false);
    this.fExtraDaysBefore.set(item.extraDaysBefore ?? 0);
    this.fExtraDaysAfter.set(item.extraDaysAfter ?? 0);
    this.fItemType.set(item.itemType ?? null);
    this.isDialogOpen.set(true);
  }

  private resetForm(): void {
    this.fNameAr.set('');
    this.fNameEn.set('');
    this.fVoteCode.set('');
    this.fIsActive.set(true);
    this.fDefaultAmount.set(0);
    this.fIsPerDay.set(false);
    this.fIsPerNominee.set(false);
    this.fExtraDaysBefore.set(0);
    this.fExtraDaysAfter.set(0);
    this.fItemType.set(null);
  }

  async onSave(): Promise<void> {
    if (!this.fNameAr().trim() || !this.fVoteCode().trim()) return;
    const data: CreateUpdateFinancialItemDto = {
      nameAr: this.fNameAr().trim(),
      nameEn: this.fNameEn().trim() || undefined,
      voteCode: this.fVoteCode().trim(),
      isActive: this.fIsActive(),
      defaultAmountOMR: this.fDefaultAmount(),
      isPerDay: this.fIsPerDay(),
      isPerNominee: this.fIsPerNominee(),
      extraDaysBefore: this.fIsPerDay() ? this.fExtraDaysBefore() : 0,
      extraDaysAfter: this.fIsPerDay() ? this.fExtraDaysAfter() : 0,
      itemType: this.fItemType(),
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
    if (!confirm(this.l.t('::Training.FinancialItems.Confirm.DeleteItem'))) return;
    await firstValueFrom(this.fiService.delete(id));
    if (this.expandedItemId() === id) this.expandedItemId.set(null);
    await this.loadData();
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
    if (!confirm(this.l.t('::Training.FinancialItems.RankAmounts.DeleteConfirm'))) return;
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

  itemTypeLabel(type: FinancialItemType | null | undefined): string {
    if (type == null) return this.l.t('::Training.FinancialItems.Table.No');
    const key = this.itemTypeKey(type);
    return key ? this.l.t(key) : this.l.t('::Training.FinancialItems.Table.No');
  }

  private itemTypeKey(type: FinancialItemType): string | null {
    switch (type) {
      case FinancialItemType.Other: return '::Training.FinancialItems.ItemType.Other';
      case FinancialItemType.CourseCost: return '::Training.FinancialItems.ItemType.CourseCost';
      case FinancialItemType.Ticket: return '::Training.FinancialItems.ItemType.Ticket';
      case FinancialItemType.Insurance: return '::Training.FinancialItems.ItemType.Insurance';
      case FinancialItemType.Visa: return '::Training.FinancialItems.ItemType.Visa';
      case FinancialItemType.Allowance: return '::Training.FinancialItems.ItemType.Allowance';
      case FinancialItemType.Clothing: return '::Training.FinancialItems.ItemType.Clothing';
      default: return null;
    }
  }

  onItemTypeChange(raw: string): void {
    if (raw === '') { this.fItemType.set(null); return; }
    const parsed = +raw;
    this.fItemType.set(Number.isFinite(parsed) ? (parsed as FinancialItemType) : null);
  }

  getTypeClass(type: FinancialItemType): string {
    switch (type) {
      case FinancialItemType.CourseCost: return 'cost';
      case FinancialItemType.Ticket: return 'ticket';
      case FinancialItemType.Insurance: return 'insurance';
      case FinancialItemType.Visa: return 'visa';
      case FinancialItemType.Allowance: return 'allowance';
      case FinancialItemType.Clothing: return 'clothing';
      case FinancialItemType.Other: return 'other';
      default: return 'other';
    }
  }

  trackById(_: number, i: FinancialItemDto): string { return i.id ?? ''; }
  trackRankById(_: number, r: FinancialItemRankAmountDto): string { return r.id ?? ''; }
}
