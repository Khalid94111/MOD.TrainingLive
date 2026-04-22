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
import { PlanNoteEntityType } from 'src/app/proxy/training/enums/plan-note-entity-type.enum';
import {
  NominationPickerComponent,
  NotesDrawerComponent,
  PlanStatus,
  TrainingLocalizationHelper,
} from '../../shared';

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

  // Conditions preview (for new item dialog — from tenant course)
  dialogConditions = signal<any[]>([]);

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
  hasUnresolvedReturns = computed(() => this.returnedItems().length > 0);
  canResubmit = computed(() =>
    this.isReturnedToCreator() && !this.hasUnresolvedReturns() && this.items().length > 0,
  );

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
    this.loadReturnReason();
  }

  async loadPlan(): Promise<void> {
    this.plan.set(await firstValueFrom(this.planService.get(this.planId)));
  }

  async loadItems(): Promise<void> {
    const r = await firstValueFrom(this.itemService.getList({ planId: this.planId, maxResultCount: 500 }));
    this.items.set(r.items ?? []);
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

  // ── Dialog ──
  openAddDialog(): void {
    this.isEditMode.set(false);
    this.editItemId.set(null);
    this.resetForm();
    this.pickerResetKey.update(v => v + 1);
    this.isDialogOpen.set(true);
  }

  openEditDialog(item: TrainingPlanItemDto, event: Event): void {
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
    this.fEstimatedDateFrom.set(item.estimatedDateFrom ?? '');
    this.fEstimatedDateTo.set(item.estimatedDateTo ?? '');
    this.fFundingSource.set(item.fundingSource ?? '');
    this.fNomineeIds.set([]); // TODO: load existing nominations if edit flow needs them
    this.pickerResetKey.update(v => v + 1);
    this.isDialogOpen.set(true);
  }

  resetForm(): void {
    this.fTenantCourseId.set(''); this.fCourseType.set(0); this.fPreferredQuarter.set(1); this.fPriority.set(1);
    this.fJustification.set('');
    this.fDescriptionAr.set(''); this.fDescriptionEn.set('');
    this.fObjectivesAr.set(''); this.fObjectivesEn.set('');
    this.fDurationYears.set(0); this.fDurationMonths.set(0); this.fDurationDays.set(0);
    this.fEstimatedDateFrom.set(''); this.fEstimatedDateTo.set(''); this.fFundingSource.set('');
    this.fNomineeIds.set([]);
    this.dialogConditions.set([]);
    this.saveError.set(null);
  }

  async onCourseSelected(): Promise<void> {
    if (!this.fTenantCourseId()) { this.dialogConditions.set([]); return; }
    const tc = this.tenantCourses().find((c: any) => c.id === this.fTenantCourseId());
    this.dialogConditions.set(tc?.conditions ?? []);
  }

  onNomineesChange(ids: string[]): void { this.fNomineeIds.set(ids); }

  get isFormValid(): boolean {
    return !!this.fTenantCourseId()
      && !!this.fJustification().trim()
      && this.fPriority() >= 1 && this.fPriority() <= 5
      && (this.isEditMode() || this.fNomineeIds().length >= 1);
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
    } catch (e: any) {
      this.saveError.set(e?.error?.error?.message ?? e?.message ?? 'فشل الحفظ');
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
  }

  // ── Resubmit ──
  async onResubmit(): Promise<void> {
    if (!this.canResubmit() || this.resubmitting()) return;
    this.resubmitting.set(true);
    try {
      await firstValueFrom(this.planService.resubmit(this.planId));
      await this.loadPlan();
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
  getDurationText(i: TrainingPlanItemDto): string {
    const p: string[] = [];
    if ((i.durationYears ?? 0) > 0) p.push(`${i.durationYears} سنة`);
    if ((i.durationMonths ?? 0) > 0) p.push(`${i.durationMonths} شهر`);
    if ((i.durationDays ?? 0) > 0) p.push(`${i.durationDays} يوم`);
    return p.length ? p.join(' و ') : '—';
  }
  trackById(_: number, i: TrainingPlanItemDto): string { return i.id ?? ''; }
}
