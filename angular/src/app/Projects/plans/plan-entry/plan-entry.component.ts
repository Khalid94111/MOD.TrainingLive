import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { TrainingPlanService, TrainingPlanItemService, PlanNoteService } from 'src/app/proxy/training/plans';
import {
  TrainingPlanDto,
  TrainingPlanItemDto,
  PlanItemConditionDto,
  CreateUpdateTrainingPlanItemDto,
  PlanNoteDto,
} from 'src/app/proxy/training/plans/dtos';
import { TenantCourseService } from 'src/app/proxy/training/tenant-courses';
import { HrLookupService } from 'src/app/proxy/training/hr-integration/hr-lookup.service';
import { NominationService } from 'src/app/proxy/training/nominations/nomination.service';
import { NominationDto } from 'src/app/proxy/training/nominations/dtos';
import { PlanNoteEntityType } from 'src/app/proxy/training/enums/plan-note-entity-type.enum';
import {
  NominationPickerComponent,
  NotesDrawerComponent,
  PlanStatus,
  TrainingLocalizationHelper,
} from '../../shared';

interface UnitGroup {
  unitId: string;
  unitName: string;
  items: TrainingPlanItemDto[];
  itemCount: number;
  totalCost: number;
  missingNominees: number;
  returnedCount: number;
}

@Component({
  standalone: true,
  selector: 'app-plan-entry',
  templateUrl: './plan-entry.component.html',
  styleUrls: ['./plan-entry.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, NominationPickerComponent, NotesDrawerComponent],
})
export class PlanEntryComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private planService = inject(TrainingPlanService);
  private itemService = inject(TrainingPlanItemService);
  private tcService = inject(TenantCourseService);
  private hrService = inject(HrLookupService);
  private noteService = inject(PlanNoteService);
  private nominationService = inject(NominationService);
  l = inject(TrainingLocalizationHelper);

  planId = '';
  plan = signal<TrainingPlanDto | null>(null);
  items = signal<TrainingPlanItemDto[]>([]);
  tenantCourses = signal<any[]>([]);
  returnNote = signal<PlanNoteDto | null>(null);
  myUnitId = signal<string>('');

  // Inline expand
  expandedItemId = signal<string | null>(null);
  conditionsMap = signal(new Map<string, PlanItemConditionDto[]>());
  loadingItemId = signal<string | null>(null);

  // Dialog
  isDialogOpen = signal(false);
  isEditMode = signal(false);
  editItemId = signal<string | null>(null);

  // Form fields (Section A: course info)
  fTenantCourseId = signal('');
  fCourseType = signal(0);
  fPreferredQuarter = signal(1);
  fPriority = signal(1);
  fJustification = signal('');
  fDescriptionAr = signal('');
  fDescriptionEn = signal('');
  fObjectivesAr = signal('');
  fObjectivesEn = signal('');
  fDurationYears = signal(0);
  fDurationMonths = signal(0);
  fDurationDays = signal(0);
  fEstimatedDateFrom = signal('');
  fEstimatedDateTo = signal('');
  fFundingSource = signal('');

  // Section B: Nominees
  fNomineeIds = signal<string[]>([]);
  pickerResetKey = signal(0); // bump to remount picker between dialog opens
  fUnitId = signal<string>(''); // only used by Staff/TD/TH when accordions are shown

  // Existing nominations for the item being edited (read-only display).
  // Adding/removing nominees from an existing item needs a separate endpoint;
  // users are pointed to the review screen for nominee changes.
  editNominations = signal<NominationDto[]>([]);
  loadingEditNominations = signal(false);

  // Conditions preview (for new item dialog — from tenant course)
  dialogConditions = signal<any[]>([]);

  // Accordion state (Staff/TD/TH only; UTM/UGM see a flat list)
  expandedUnits = signal<Set<string>>(new Set());
  private expansionInitialized = false;

  // Filter bar above accordions / flat list
  searchText = signal('');
  filterCourseType = signal<string>('');
  filterUnit = signal<string>('');

  // Save error banner
  saveError = signal<string | null>(null);
  saving = signal(false);
  resubmitting = signal(false);

  // Notes drawer
  notesOpen = signal(false);
  notesEntityType = signal<PlanNoteEntityType>(PlanNoteEntityType.Plan);
  notesEntityId = signal<string>('');
  notesTitle = signal<string>('');

  PlanStatus = PlanStatus;
  PlanNoteEntityType = PlanNoteEntityType;

  // ── Computed ──
  isWindowOpen = computed(() => this.plan()?.status === PlanStatus.Open);
  isReturnedToCreator = computed(() => this.plan()?.status === PlanStatus.ReturnedToCreator);
  canEdit = computed(() => {
    const s = this.plan()?.status;
    return s === PlanStatus.Open || s === PlanStatus.ReturnedToCreator;
  });
  returnedItems = computed(() => this.items().filter(i => i.isReturned));
  returnedNominationsCount = signal(0);
  hasUnresolvedReturns = computed(() =>
    this.returnedItems().length > 0 || this.returnedNominationsCount() > 0,
  );
  canResubmit = computed(() =>
    this.isReturnedToCreator() && !this.hasUnresolvedReturns() && this.items().length > 0,
  );

  hasActiveFilter = computed(() =>
    !!this.searchText().trim() || !!this.filterCourseType() || !!this.filterUnit(),
  );

  filteredItems = computed(() => {
    const q = this.searchText().trim().toLowerCase();
    const ct = this.filterCourseType();
    const uid = this.filterUnit();
    return this.items().filter(i => {
      if (q) {
        const hit =
          (i.tenantCourseNameAr ?? '').toLowerCase().includes(q) ||
          (i.tenantCourseNameEn ?? '').toLowerCase().includes(q) ||
          (i.fundingSource ?? '').toLowerCase().includes(q);
        if (!hit) return false;
      }
      if (ct !== '' && i.courseType !== +ct) return false;
      if (uid && i.unitId !== uid) return false;
      return true;
    });
  });

  groupedByUnit = computed<UnitGroup[]>(() => {
    const byUnit = new Map<string, UnitGroup>();
    for (const item of this.filteredItems()) {
      const key = item.unitId ?? 'no-unit';
      const name = item.unitName ?? 'بدون وحدة';
      if (!byUnit.has(key)) {
        byUnit.set(key, {
          unitId: key, unitName: name, items: [],
          itemCount: 0, totalCost: 0, missingNominees: 0, returnedCount: 0,
        });
      }
      const g = byUnit.get(key)!;
      g.items.push(item);
      g.itemCount++;
      g.totalCost += item.estimatedCost ?? 0;
      if (!item.nomineesCount || item.nomineesCount === 0) g.missingNominees++;
      if (item.isReturned) g.returnedCount++;
    }
    return Array.from(byUnit.values()).sort((a, b) => a.unitName.localeCompare(b.unitName, 'ar'));
  });

  // Response-based inference: if items span multiple units the caller is non-scoped
  // (Staff/TD/TH) and should see accordions. Unit-scoped users (UTM/UGM) only ever
  // see their own unit's items, so a single group means flat-list mode.
  showAccordions = computed(() => {
    // Base the decision on the unfiltered items so the mode is stable while filtering.
    const unitSet = new Set<string>();
    for (const i of this.items()) unitSet.add(i.unitId ?? 'no-unit');
    return unitSet.size > 1;
  });

  uniqueUnits = computed<{ id: string; name: string }[]>(() => {
    const m = new Map<string, string>();
    for (const i of this.items()) {
      if (i.unitId) m.set(i.unitId, i.unitName ?? i.unitId);
    }
    return Array.from(m.entries())
      .map(([id, name]) => ({ id, name }))
      .sort((a, b) => a.name.localeCompare(b.name, 'ar'));
  });

  get dialogTitle(): string {
    return this.isEditMode() ? '✏️ تعديل بند' : '➕ إضافة بند جديد';
  }

  get statusBadgeClass(): string {
    const s = this.plan()?.status;
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

  get statusText(): string {
    const s = this.plan()?.status;
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

  async ngOnInit(): Promise<void> {
    this.planId = this.route.snapshot.paramMap.get('planId') ?? '';
    await Promise.all([
      this.loadPlan(),
      this.loadItems(),
      this.loadTenantCourses(),
      this.loadCurrentEmployee(),
    ]);
    await Promise.all([
      this.loadReturnReason(),
      this.loadReturnedNominationsCount(),
    ]);
  }

  async loadPlan(): Promise<void> {
    this.plan.set(await firstValueFrom(this.planService.get(this.planId)));
  }

  async loadItems(): Promise<void> {
    const r = await firstValueFrom(this.itemService.getList({ planId: this.planId, maxResultCount: 500 }));
    const items = r.items ?? [];
    this.items.set(items);

    // Default-expand rule (Staff/TD/TH only; runs once on first load):
    // ≤3 units → all expanded; more → all collapsed. Subsequent reloads
    // keep whatever state the user has set.
    if (!this.expansionInitialized) {
      const unitIds = new Set<string>();
      for (const i of items) unitIds.add(i.unitId ?? 'no-unit');
      this.expandedUnits.set(unitIds.size <= 3 ? unitIds : new Set());
      this.expansionInitialized = true;
    }
  }

  async loadTenantCourses(): Promise<void> {
    const r = await firstValueFrom(this.tcService.getList({ maxResultCount: 200, isActive: true }));
    this.tenantCourses.set(r.items ?? []);
  }

  async loadCurrentEmployee(): Promise<void> {
    try {
      const me = await firstValueFrom(this.hrService.getCurrentEmployee());
      this.myUnitId.set(me?.mainUnitId ?? '');
    } catch {
      this.myUnitId.set('');
    }
  }

  async loadReturnReason(): Promise<void> {
    if (!this.isReturnedToCreator()) { this.returnNote.set(null); return; }
    const list = await firstValueFrom(
      this.noteService.getList({
        entityType: PlanNoteEntityType.Plan,
        entityId: this.planId,
        maxResultCount: 50,
        sorting: 'creationTime desc',
      }),
    );
    const returnNotes = (list ?? []).filter(n => n.isReturnReason);
    this.returnNote.set(returnNotes[0] ?? null);
  }

  // Count nominations in this plan that were returned for correction, so the
  // Resubmit button stays disabled until all of them are resolved (the backend
  // ValidateNoUnresolvedReturnsAsync would otherwise throw).
  async loadReturnedNominationsCount(): Promise<void> {
    if (!this.isReturnedToCreator()) { this.returnedNominationsCount.set(0); return; }
    const planItemIds = new Set(this.items().map(i => i.id));
    if (planItemIds.size === 0) { this.returnedNominationsCount.set(0); return; }
    try {
      const r = await firstValueFrom(this.nominationService.getList({ maxResultCount: 1000 }));
      let count = 0;
      for (const n of (r.items ?? [])) {
        if (n.isReturned && n.planItemId && planItemIds.has(n.planItemId)) count++;
      }
      this.returnedNominationsCount.set(count);
    } catch {
      this.returnedNominationsCount.set(0);
    }
  }

  // ── Inline expand ──
  async toggleItemDetail(itemId: string): Promise<void> {
    if (this.expandedItemId() === itemId) { this.expandedItemId.set(null); return; }
    this.expandedItemId.set(itemId);
    this.loadingItemId.set(itemId);
    if (!this.conditionsMap().has(itemId)) {
      const conds = await firstValueFrom(this.itemService.getConditions(itemId));
      this.conditionsMap.update(m => { const n = new Map(m); n.set(itemId, conds); return n; });
    }
    this.loadingItemId.set(null);
  }

  isItemExpanded(id: string): boolean { return this.expandedItemId() === id; }
  isItemLoading(id: string): boolean { return this.loadingItemId() === id; }
  getConditionsFor(id: string): PlanItemConditionDto[] { return this.conditionsMap().get(id) ?? []; }

  // ── Accordion ──
  toggleUnit(unitId: string): void {
    this.expandedUnits.update(s => {
      const n = new Set(s);
      if (n.has(unitId)) n.delete(unitId); else n.add(unitId);
      return n;
    });
  }

  isUnitExpanded(unitId: string): boolean {
    if (this.expandedUnits().has(unitId)) return true;
    // Auto-expand when a filter is active so matching items in a previously-
    // collapsed accordion stay visible.
    return this.hasActiveFilter();
  }

  expandAll(): void { this.expandedUnits.set(new Set(this.groupedByUnit().map(g => g.unitId))); }
  collapseAll(): void { this.expandedUnits.set(new Set()); this.expandedItemId.set(null); }

  onSearchInput(event: Event): void { this.searchText.set((event.target as HTMLInputElement).value); }

  // ── Dialog ──
  openAddDialog(): void {
    this.isEditMode.set(false);
    this.editItemId.set(null);
    this.resetForm();
    // Default the picker to the first unit in the list (Staff/TD/TH only).
    if (this.showAccordions()) {
      const first = this.uniqueUnits()[0];
      if (first) this.fUnitId.set(first.id);
    }
    // Default EstimatedDateFrom to Q1 start of the plan year so the user sees a
    // sensible value up front; recomputes as soon as they pick a different quarter.
    const year = this.plan()?.year;
    if (year) {
      this.fEstimatedDateFrom.set(this.dateToInputValue(this.quarterStartDate(year, 1)));
      this.recomputeDateTo();
    }
    this.pickerResetKey.update(v => v + 1);
    this.isDialogOpen.set(true);
  }

  async openEditDialog(item: TrainingPlanItemDto, event: Event): Promise<void> {
    event.stopPropagation();
    this.isEditMode.set(true);
    this.editItemId.set(item.id);
    this.fTenantCourseId.set(item.tenantCourseId ?? '');
    this.fCourseType.set(item.courseType ?? 0);
    this.fPreferredQuarter.set(item.preferredQuarter ?? 1);
    this.fPriority.set(item.priority ?? 1);
    this.fJustification.set(item.justification ?? '');
    this.fDescriptionAr.set(item.descriptionAr ?? '');
    this.fDescriptionEn.set(item.descriptionEn ?? '');
    this.fObjectivesAr.set(item.objectivesAr ?? '');
    this.fObjectivesEn.set(item.objectivesEn ?? '');
    this.fDurationYears.set(item.durationYears ?? 0);
    this.fDurationMonths.set(item.durationMonths ?? 0);
    this.fDurationDays.set(item.durationDays ?? 0);
    // <input type="date"> needs yyyy-MM-dd; the DTO returns a full ISO timestamp.
    this.fEstimatedDateFrom.set(this.toDateInputValue(item.estimatedDateFrom));
    this.fEstimatedDateTo.set(this.toDateInputValue(item.estimatedDateTo));
    this.fFundingSource.set(item.fundingSource ?? '');
    // Pin the picker to the item's unit (works for both UTM and Staff viewers).
    this.fUnitId.set(item.unitId ?? '');
    this.fNomineeIds.set([]);
    this.editNominations.set([]);
    this.pickerResetKey.update(v => v + 1);
    this.isDialogOpen.set(true);

    // Load existing nominations and preselect them in the picker so the user
    // can toggle additions/removals. Backend UpdateAsync diffs the list.
    if (item.id) await this.loadEditNominations(item.id);
  }

  private async loadEditNominations(planItemId: string): Promise<void> {
    this.loadingEditNominations.set(true);
    try {
      const r = await firstValueFrom(
        this.nominationService.getList({ planItemId, maxResultCount: 500 }),
      );
      const list = r.items ?? [];
      this.editNominations.set(list);
      this.fNomineeIds.set(list.map(n => n.employeeId).filter((x): x is string => !!x));
    } catch {
      this.editNominations.set([]);
    } finally {
      this.loadingEditNominations.set(false);
    }
  }

  resetForm(): void {
    this.fTenantCourseId.set(''); this.fCourseType.set(0); this.fPreferredQuarter.set(1); this.fPriority.set(1);
    this.fJustification.set('');
    this.fDescriptionAr.set(''); this.fDescriptionEn.set('');
    this.fObjectivesAr.set(''); this.fObjectivesEn.set('');
    this.fDurationYears.set(0); this.fDurationMonths.set(0); this.fDurationDays.set(0);
    this.fEstimatedDateFrom.set(''); this.fEstimatedDateTo.set(''); this.fFundingSource.set('');
    this.fUnitId.set('');
    this.fNomineeIds.set([]);
    this.editNominations.set([]);
    this.dialogConditions.set([]);
    this.saveError.set(null);
  }

  async onCourseSelected(): Promise<void> {
    if (!this.fTenantCourseId()) { this.dialogConditions.set([]); return; }
    const tc = this.tenantCourses().find((c: any) => c.id === this.fTenantCourseId());
    this.dialogConditions.set(tc?.conditions ?? []);
  }

  // ── Date auto-fill ──
  // Selecting a preferred quarter snaps EstimatedDateFrom to the first day of
  // that quarter within the plan year; then EstimatedDateTo is recomputed as
  // EstimatedDateFrom + duration (years + months + days). Changing any duration
  // field or typing a different EstimatedDateFrom also re-derives DateTo.
  onQuarterChange(quarter: number): void {
    this.fPreferredQuarter.set(quarter);
    const year = this.plan()?.year;
    if (year) {
      this.fEstimatedDateFrom.set(this.dateToInputValue(this.quarterStartDate(year, quarter)));
    }
    this.recomputeDateTo();
  }

  onDurationChange(): void {
    this.recomputeDateTo();
  }

  onDateFromChange(value: string): void {
    this.fEstimatedDateFrom.set(value);
    this.recomputeDateTo();
  }

  private recomputeDateTo(): void {
    const from = this.fEstimatedDateFrom();
    if (!from) return;
    const parts = from.split('-').map(Number);
    if (parts.length !== 3 || parts.some(isNaN)) return;
    const [y, m, d] = parts;
    const start = new Date(y, m - 1, d);
    start.setFullYear(start.getFullYear() + (this.fDurationYears() || 0));
    start.setMonth(start.getMonth() + (this.fDurationMonths() || 0));
    start.setDate(start.getDate() + (this.fDurationDays() || 0));
    this.fEstimatedDateTo.set(this.dateToInputValue(start));
  }

  private quarterStartDate(year: number, quarter: number): Date {
    // Q1 → Jan (month 0), Q2 → Apr (3), Q3 → Jul (6), Q4 → Oct (9)
    const month = (Math.max(1, Math.min(4, quarter)) - 1) * 3;
    return new Date(year, month, 1);
  }

  private dateToInputValue(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  onNomineesChange(ids: string[]): void { this.fNomineeIds.set(ids); }

  get isFormValid(): boolean {
    const baseValid = !!this.fTenantCourseId()
      && !!this.fJustification().trim()
      && this.fPriority() >= 1 && this.fPriority() <= 5
      && this.fNomineeIds().length >= 1; // ≥1 nominee required for both create and edit
    if (!baseValid) return false;
    // Non-scoped users (multi-unit view) must pick a unit when creating.
    if (!this.isEditMode() && this.showAccordions() && !this.fUnitId()) return false;
    return true;
  }

  async onSave(): Promise<void> {
    if (!this.isFormValid || this.saving()) return;
    this.saving.set(true);
    this.saveError.set(null);
    const data: CreateUpdateTrainingPlanItemDto = {
      planId: this.planId,
      tenantCourseId: this.fTenantCourseId(),
      courseType: this.fCourseType(),
      preferredQuarter: this.fPreferredQuarter(),
      priority: this.fPriority(),
      justification: this.fJustification(),
      descriptionAr: this.fDescriptionAr() || undefined,
      descriptionEn: this.fDescriptionEn() || undefined,
      objectivesAr: this.fObjectivesAr() || undefined,
      objectivesEn: this.fObjectivesEn() || undefined,
      durationYears: this.fDurationYears(),
      durationMonths: this.fDurationMonths(),
      durationDays: this.fDurationDays(),
      estimatedDateFrom: this.fEstimatedDateFrom() || undefined,
      estimatedDateTo: this.fEstimatedDateTo() || undefined,
      fundingSource: this.fFundingSource() || undefined,
      unitId: this.showAccordions() && this.fUnitId() ? this.fUnitId() : undefined,
      nomineeEmployeeIds: this.fNomineeIds(),
    };
    try {
      if (this.isEditMode() && this.editItemId()) {
        await firstValueFrom(this.itemService.update(this.editItemId()!, data));
      } else {
        await firstValueFrom(this.itemService.create(data));
      }
      this.isDialogOpen.set(false);
      await this.loadItems();
      await this.loadReturnedNominationsCount();
    } catch (e: any) {
      // Surface per-nominee failure details when the backend returns them
      // via ConditionFailed (data.Failures = "Emp1: reason | Emp2: reason").
      const msg = e?.error?.error?.message ?? e?.message ?? 'فشل الحفظ';
      const failures = e?.error?.error?.data?.Failures;
      this.saveError.set(failures ? `${msg} — ${failures}` : msg);
    } finally {
      this.saving.set(false);
    }
  }

  async onDelete(id: string, event: Event): Promise<void> {
    event.stopPropagation();
    if (!confirm('هل أنت متأكد من حذف هذا البند؟')) return;
    await firstValueFrom(this.itemService.delete(id));
    if (this.expandedItemId() === id) this.expandedItemId.set(null);
    await this.loadItems();
    await this.loadReturnedNominationsCount();
  }

  // ── Resubmit ──
  async onResubmit(): Promise<void> {
    if (!this.canResubmit() || this.resubmitting()) return;
    this.resubmitting.set(true);
    this.saveError.set(null);
    try {
      await firstValueFrom(this.planService.resubmit(this.planId));
      await this.loadPlan();
      await this.loadReturnedNominationsCount();
    } catch (e: any) {
      this.saveError.set(e?.error?.error?.message ?? e?.message ?? this.l.t('::Training.Errors.Generic'));
    } finally {
      this.resubmitting.set(false);
    }
  }

  // ── Notes drawer ──
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
  getCourseTypeBadge(t: number): string { return ({ 0: 'badge-internal', 1: 'badge-ext-local', 2: 'badge-ext-intl' } as Record<number, string>)[t] ?? ''; }
  getCourseTypeText(t: number): string { return ({ 0: 'داخلية', 1: 'خارجية محلية', 2: 'خارجية دولية' } as Record<number, string>)[t] ?? ''; }
  getQuarterText(q: number): string { return ({ 1: 'الربع الأول', 2: 'الربع الثاني', 3: 'الربع الثالث', 4: 'الربع الرابع' } as Record<number, string>)[q] ?? ''; }
  getConditionTypeName(t: number): string { return ({ 0: 'الرتبة', 1: 'العمر', 2: 'سنوات الخدمة', 3: 'المؤهل', 4: 'لياقة طبية', 5: 'تصريح أمني', 6: 'لغة', 7: 'دورة سابقة', 8: 'مخصص' } as Record<number, string>)[t] ?? ''; }
  formatDate(d?: string | null): string { if (!d) return '—'; return new Date(d).toLocaleDateString('ar-OM'); }

  // Convert an ISO date/datetime string from the API into the yyyy-MM-dd slice
  // that <input type="date"> expects. Returns '' for null/undefined/empty input.
  private toDateInputValue(d?: string | null): string {
    if (!d) return '';
    return d.length >= 10 ? d.substring(0, 10) : '';
  }
  getDurationText(i: TrainingPlanItemDto): string {
    const p: string[] = [];
    if ((i.durationYears ?? 0) > 0) p.push(`${i.durationYears} سنة`);
    if ((i.durationMonths ?? 0) > 0) p.push(`${i.durationMonths} شهر`);
    if ((i.durationDays ?? 0) > 0) p.push(`${i.durationDays} يوم`);
    return p.length ? p.join(' و ') : '—';
  }
  trackById(_: number, i: TrainingPlanItemDto): string { return i.id ?? ''; }
  trackByUnit(_: number, g: UnitGroup): string { return g.unitId; }

  formatCost(n?: number): string {
    if (!n || n <= 0) return '';
    return n.toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 });
  }
}
