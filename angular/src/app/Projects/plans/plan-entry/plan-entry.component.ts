import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { TrainingPlanService, TrainingPlanItemService } from 'src/app/proxy/training/plans';
import { TrainingPlanDto, TrainingPlanItemDto, PlanItemConditionDto, CreateUpdateTrainingPlanItemDto } from 'src/app/proxy/training/plans/dtos';
import { PlanStatus, TrainingLocalizationHelper } from '../../shared';
import { TenantCourseService } from 'src/app/proxy/training/tenant-courses';


@Component({
  standalone: true,
  selector: 'app-plan-entry',
  templateUrl: './plan-entry.component.html',
  styleUrls: ['./plan-entry.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule],
})
export class PlanEntryComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private planService = inject(TrainingPlanService);
  private itemService = inject(TrainingPlanItemService);
  private tcService = inject(TenantCourseService);
  l = inject(TrainingLocalizationHelper);

  planId = '';
  plan = signal<TrainingPlanDto | null>(null);
  items = signal<TrainingPlanItemDto[]>([]);
  tenantCourses = signal<any[]>([]);

  // Inline expand
  expandedItemId = signal<string | null>(null);
  conditionsMap = signal(new Map<string, PlanItemConditionDto[]>());
  loadingItemId = signal<string | null>(null);

  // Dialog
  isDialogOpen = signal(false);
  isEditMode = signal(false);
  editItemId = signal<string | null>(null);

  // Form fields
  fTenantCourseId = signal('');
  fCourseType = signal(0);
  fPreferredQuarter = signal(1);
  fPriority = signal(1);
  fOfficersCount = signal(0);
  fEnlistedCount = signal(0);
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

  // Conditions preview (for new item dialog — from tenant course)
  dialogConditions = signal<any[]>([]);

  PlanStatus = PlanStatus;

  get isWindowOpen(): boolean { return this.plan()?.status === PlanStatus.Open; }
  get dialogTitle(): string { return this.isEditMode() ? '✏️ تعديل بند' : '➕ إضافة بند جديد'; }

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('planId') ?? '';
    this.loadPlan();
    this.loadItems();
    this.loadTenantCourses();
  }

  async loadPlan(): Promise<void> { this.plan.set(await firstValueFrom(this.planService.get(this.planId))); }

  async loadItems(): Promise<void> {
    const r = await firstValueFrom(this.itemService.getList({ planId: this.planId, maxResultCount: 500 }));
    this.items.set(r.items ?? []);
  }

  async loadTenantCourses(): Promise<void> {
    const r = await firstValueFrom(this.tcService.getList({ maxResultCount: 200, isActive: true }));
    this.tenantCourses.set(r.items ?? []);
  }

  // ── Inline expand (course detail only, no financials) ──
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
    this.isDialogOpen.set(true);
  }

  openEditDialog(item: TrainingPlanItemDto, event: Event): void {
    event.stopPropagation();
    this.isEditMode.set(true);
    this.editItemId.set(item.id);
    this.fTenantCourseId.set(item.tenantCourseId);
    this.fCourseType.set(item.courseType);
    this.fPreferredQuarter.set(item.preferredQuarter);
    this.fPriority.set(item.priority);
    this.fOfficersCount.set(item.officersCount);
    this.fEnlistedCount.set(item.enlistedCount);
    this.fJustification.set(item.justification);
    this.fDescriptionAr.set(item.descriptionAr ?? '');
    this.fDescriptionEn.set(item.descriptionEn ?? '');
    this.fObjectivesAr.set(item.objectivesAr ?? '');
    this.fObjectivesEn.set(item.objectivesEn ?? '');
    this.fDurationYears.set(item.durationYears);
    this.fDurationMonths.set(item.durationMonths);
    this.fDurationDays.set(item.durationDays);
    this.fEstimatedDateFrom.set(item.estimatedDateFrom ?? '');
    this.fEstimatedDateTo.set(item.estimatedDateTo ?? '');
    this.fFundingSource.set(item.fundingSource ?? '');
    this.isDialogOpen.set(true);
  }

  resetForm(): void {
    this.fTenantCourseId.set(''); this.fCourseType.set(0); this.fPreferredQuarter.set(1); this.fPriority.set(1);
    this.fOfficersCount.set(0); this.fEnlistedCount.set(0); this.fJustification.set('');
    this.fDescriptionAr.set(''); this.fDescriptionEn.set(''); this.fObjectivesAr.set(''); this.fObjectivesEn.set('');
    this.fDurationYears.set(0); this.fDurationMonths.set(0); this.fDurationDays.set(0);
    this.fEstimatedDateFrom.set(''); this.fEstimatedDateTo.set(''); this.fFundingSource.set('');
    this.dialogConditions.set([]);
  }

  async onCourseSelected(): Promise<void> {
    if (!this.fTenantCourseId()) { this.dialogConditions.set([]); return; }
    const tc = this.tenantCourses().find((c: any) => c.id === this.fTenantCourseId());
    this.dialogConditions.set(tc?.conditions ?? []);
  }

  async onSave(): Promise<void> {
    const data: CreateUpdateTrainingPlanItemDto = {
      planId: this.planId, tenantCourseId: this.fTenantCourseId(), courseType: this.fCourseType(),
      preferredQuarter: this.fPreferredQuarter(), priority: this.fPriority(),
      officersCount: this.fOfficersCount(), enlistedCount: this.fEnlistedCount(), justification: this.fJustification(),
      descriptionAr: this.fDescriptionAr() || undefined, descriptionEn: this.fDescriptionEn() || undefined,
      objectivesAr: this.fObjectivesAr() || undefined, objectivesEn: this.fObjectivesEn() || undefined,
      durationYears: this.fDurationYears(), durationMonths: this.fDurationMonths(), durationDays: this.fDurationDays(),
      estimatedDateFrom: this.fEstimatedDateFrom() || undefined, estimatedDateTo: this.fEstimatedDateTo() || undefined,
      fundingSource: this.fFundingSource() || undefined,
    };
    if (this.isEditMode() && this.editItemId()) await firstValueFrom(this.itemService.update(this.editItemId()!, data));
    else await firstValueFrom(this.itemService.create(data));
    this.isDialogOpen.set(false);
    await this.loadItems();
  }

  async onDelete(id: string, event: Event): Promise<void> {
    event.stopPropagation();
    if (!confirm('هل أنت متأكد من حذف هذا البند؟')) return;
    await firstValueFrom(this.itemService.delete(id));
    if (this.expandedItemId() === id) this.expandedItemId.set(null);
    await this.loadItems();
  }

  // ── Helpers ──
  getCourseTypeBadge(t: number): string { return ({ 0: 'badge-internal', 1: 'badge-ext-local', 2: 'badge-ext-intl' } as Record<number, string>)[t] ?? ''; }
  getCourseTypeText(t: number): string { return ({ 0: 'داخلية', 1: 'خارجية محلية', 2: 'خارجية دولية' } as Record<number, string>)[t] ?? ''; }
  getQuarterText(q: number): string { return ({ 1: 'الربع الأول', 2: 'الربع الثاني', 3: 'الربع الثالث', 4: 'الربع الرابع' } as Record<number, string>)[q] ?? ''; }
  getConditionTypeName(t: number): string { return ({ 0: 'الرتبة', 1: 'العمر', 2: 'سنوات الخدمة', 3: 'المؤهل', 4: 'لياقة طبية', 5: 'تصريح أمني', 6: 'لغة', 7: 'دورة سابقة', 8: 'مخصص' } as Record<number, string>)[t] ?? ''; }
  formatDate(d?: string): string { if (!d) return '—'; return new Date(d).toLocaleDateString('ar-OM'); }
  getDurationText(i: TrainingPlanItemDto): string { const p: string[] = []; if (i.durationYears > 0) p.push(`${i.durationYears} سنة`); if (i.durationMonths > 0) p.push(`${i.durationMonths} شهر`); if (i.durationDays > 0) p.push(`${i.durationDays} يوم`); return p.length ? p.join(' و ') : '—'; }
}
