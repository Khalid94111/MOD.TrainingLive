import { Component, OnInit, computed, inject, input, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { CasualCourseService } from 'src/app/proxy/training/casual-courses';
import type {
  CalculatePreviewDto,
  CasualCourseDetailDto,
} from 'src/app/proxy/training/casual-courses/dtos/models';
import { FinancialItemService } from 'src/app/proxy/training/finance/financial-item.service';
import type { FinancialItemDto } from 'src/app/proxy/training/finance/dtos/models';

import {
  CasualCourseStatus,
  CASUAL_COURSE_STATUS_OPTIONS,
  CourseType,
  FundingScenario,
  FUNDING_SCENARIO_OPTIONS,
  TrainingLocalizationHelper,
} from '../../shared';
import { NotesDrawerComponent } from '../../shared/components/notes-drawer/notes-drawer.component';
import { ReturnModalComponent } from '../../shared/components/return-modal/return-modal.component';
import { PlanNoteEntityType } from 'src/app/proxy/training/enums/plan-note-entity-type.enum';
import { FinancialItemType } from 'src/app/proxy/training/enums/financial-item-type.enum';
import { CasualCourseActionService } from '../casual-course-detail/casual-course-action.service';

interface TravelFundingRow {
  itemType: FinancialItemType;
  name: string;
  fundingSource: string;
  voteCode: string;
}

@Component({
  standalone: true,
  selector: 'app-casual-course-approval',
  templateUrl: './casual-course-approval.component.html',
  styleUrls: ['./casual-course-approval.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, RouterLink, NotesDrawerComponent, ReturnModalComponent],
})
export class CasualCourseApprovalComponent implements OnInit {
  private service = inject(CasualCourseService);
  private financialItemService = inject(FinancialItemService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private actions = inject(CasualCourseActionService);
  private destroyRef = inject(DestroyRef);
  l = inject(TrainingLocalizationHelper);

  CasualCourseStatus = CasualCourseStatus;
  CourseType = CourseType;
  PlanNoteEntityType = PlanNoteEntityType;

  /** Render slot — see CasualCourseRequestComponent for semantics. */
  mode = input<'details' | 'financials' | 'all'>('all');
  showDetails    = computed(() => this.mode() === 'details'    || this.mode() === 'all');
  showFinancials = computed(() => this.mode() === 'financials' || this.mode() === 'all');

  courseId = signal<string>('');
  embedded = signal<boolean>(false);
  casualCourse = signal<CasualCourseDetailDto | null>(null);
  financialItems = signal<FinancialItemDto[]>([]);
  // Patch 5 — UGM viewing a Submitted course (before scenario is picked) sees a server-
  // computed preview instead of real CasualCourseFinancialItem rows. TD/TH still see the
  // real rows because Staff has finalised review by then.
  preview = signal<CalculatePreviewDto | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  actionBusy = signal(false);
  expandedItemId = signal<string | null>(null);

  approvalNote = signal<string>('');
  notesOpen = signal(false);
  returnModalOpen = signal(false);
  rejectOpen = signal(false);
  rejectReason = signal('');
  rejectSubmitting = signal(false);

  isUGMTurn = computed(() => this.casualCourse()?.status === CasualCourseStatus.Submitted);
  isTDTurn = computed(() => this.casualCourse()?.status === CasualCourseStatus.StaffReviewed);
  isTHTurn = computed(() => this.casualCourse()?.status === CasualCourseStatus.TDApproved);
  canAct = computed(() => this.isUGMTurn() || this.isTDTurn() || this.isTHTurn());

  actorLabel = computed(() => {
    if (this.isUGMTurn()) return 'مدير عام الوحدة (UGM)';
    if (this.isTDTurn()) return 'مدير التدريب (TD)';
    if (this.isTHTurn()) return 'رئيس التدريب (TH)';
    return 'عرض فقط';
  });

  totalCost = computed(() => this.casualCourse()?.estimatedTotalCost ?? 0);
  courseCost = computed(() => this.casualCourse()?.courseCost ?? 0);
  isInternational = computed(
    () => this.casualCourse()?.courseType === CourseType.ExternalInternational,
  );
  travelFundingRows = computed<TravelFundingRow[]>(() => {
    if (!this.isInternational()) return [];

    const course = this.casualCourse();
    const useCourseSource = course?.fundingScenario === FundingScenario.FundingSourceCoversAll;
    const specs: Array<{ itemType: FinancialItemType; name: string }> = [
      { itemType: FinancialItemType.Ticket, name: 'تذاكر السفر' },
      { itemType: FinancialItemType.Visa, name: 'التأشيرة' },
      { itemType: FinancialItemType.Insurance, name: 'التأمين الصحي' },
      { itemType: FinancialItemType.Allowance, name: 'العلاوة اليومية' },
      { itemType: FinancialItemType.Clothing, name: 'بدل الملابس' },
    ];

    return specs.map(spec => {
      const configured = [...this.financialItems()]
        .reverse()
        .find(item => item.itemType === spec.itemType && !!item.voteCode?.trim());
      return {
        ...spec,
        fundingSource: useCourseSource
          ? course?.fundingSourceName || 'مصدر تمويل الدورة'
          : configured?.nameAr || 'غير مضبوط في إعدادات التدريب',
        voteCode: useCourseSource
          ? course?.fundingSourceVoteCode || ''
          : configured?.voteCode || '',
      };
    });
  });
  costGateBlocked = computed(() => {
    const cost = this.casualCourse()?.estimatedTotalCost;
    return cost === null || cost === undefined || cost <= 0;
  });

  scenarioLabel = computed(() => {
    const s = this.casualCourse()?.fundingScenario;
    if (s === null || s === undefined) return '—';
    const opt = FUNDING_SCENARIO_OPTIONS.find(o => o.value === s);
    return opt ? this.l.t(opt.key) : '—';
  });

  statusLabel = computed(() => {
    const s = this.casualCourse()?.status;
    if (s === undefined) return '';
    const opt = CASUAL_COURSE_STATUS_OPTIONS.find(o => o.value === s);
    return opt ? this.l.t(opt.key) : '';
  });

  statusCss = computed(() => {
    const s = this.casualCourse()?.status;
    if (s === undefined) return '';
    return CASUAL_COURSE_STATUS_OPTIONS.find(o => o.value === s)?.cssClass ?? '';
  });

  courseTypeLabel = computed(() => {
    switch (this.casualCourse()?.courseType) {
      case CourseType.ExternalLocal: return 'خارجية محلية';
      case CourseType.ExternalInternational: return 'خارجية دولية';
      default: return '—';
    }
  });

  priorityLabel = computed(() => {
    switch (this.casualCourse()?.priority) {
      case 1: return 'عالية جدًا';
      case 2: return 'عالية';
      case 3: return 'متوسطة';
      case 4: return 'منخفضة';
      case 5: return 'منخفضة جدًا';
      default: return '—';
    }
  });

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.courseId.set(id);
    this.embedded.set(!!this.route.snapshot.data['embedded']);
    await this.loadAll();

    // Header action bar dispatch — UGM (Submitted) acts on Section 1
    // (mode='details'); TD (StaffReviewed) and TH (TDApproved) act on
    // Section 2 (mode='financials'). The guard picks the primary mode
    // for the current status so the live instance responds and a sibling
    // instance (rendered when the user manually expands the off-default
    // section) doesn't double-fire.
    this.actions.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(action => {
      if (!this.embedded() || !this.canAct()) return;
      const s = this.casualCourse()?.status;
      const primaryMode: 'details' | 'financials' =
        s === CasualCourseStatus.Submitted ? 'details' : 'financials';
      if (this.mode() !== primaryMode) return;
      if (action === 'approve')      void this.onApprove();
      else if (action === 'return')  this.openReturn();
      else if (action === 'reject')  this.openReject();
    });
  }

  private async loadAll(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const detail = await firstValueFrom(this.service.getDetail(this.courseId()));
      this.casualCourse.set(detail);

      if (detail.courseType === CourseType.ExternalInternational) {
        const items = await firstValueFrom(
          this.financialItemService.getList({ maxResultCount: 500, isActive: true }),
        );
        this.financialItems.set(items.items ?? []);
      } else {
        this.financialItems.set([]);
      }

      const isUgmStage = detail.status === CasualCourseStatus.Submitted;
      if (isUgmStage) {
        // Patch 5 — Submitted = pre-scenario; render the calculator preview instead
        // of querying CasualCourseFinancialItems (which don't exist yet).
        const nomineeIds = (detail.nominations ?? [])
          .map(n => n.employeeId)
          .filter((v): v is string => !!v);
        if (detail.courseType !== undefined && (detail.durationDays ?? 0) > 0) {
          this.preview.set(await firstValueFrom(this.service.calculatePreview({
            courseType: detail.courseType,
            durationDays: detail.durationDays!,
            nomineeEmployeeIds: nomineeIds,
            courseCost: detail.courseCost ?? null,
          })));
        }
      } else {
        this.preview.set(null);
      }
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
    } finally {
      this.loading.set(false);
    }
  }

  previewGrandTotal = computed(() => this.preview()?.totalOMR ?? 0);

  formatDate(value?: string | null): string {
    if (!value) return '—';
    const [year, month, day] = value.substring(0, 10).split('-');
    return year && month && day ? `${day}/${month}/${year}` : value;
  }

  isExpanded(id: string | undefined): boolean {
    return !!id && this.expandedItemId() === id;
  }

  toggleExpand(id: string | undefined): void {
    if (!id) return;
    this.expandedItemId.update(cur => (cur === id ? null : id));
  }

  async onApprove(): Promise<void> {
    if (!this.canAct() || this.actionBusy()) return;
    // UGM approves on Submitted; cost isn't assigned yet, so skip the cost gate for UGM.
    if (!this.isUGMTurn() && this.costGateBlocked()) return;
    this.actionBusy.set(true);
    this.error.set(null);
    try {
      const note = {
        entityType: PlanNoteEntityType.CasualCourse,
        entityId: this.courseId(),
        note: this.approvalNote().trim() || 'معتمد',
        isReturnReason: false,
      };
      if (this.isUGMTurn()) {
        await firstValueFrom(this.service.ugmApprove(this.courseId(), note));
      } else if (this.isTDTurn()) {
        await firstValueFrom(this.service.tdApprove(this.courseId(), note));
      } else {
        await firstValueFrom(this.service.headApprove(this.courseId(), note));
      }
      this.router.navigate(['/training/casual-courses']);
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
    } finally {
      this.actionBusy.set(false);
    }
  }

  openReturn(): void {
    this.returnModalOpen.set(true);
  }

  onReturnCancelled(): void {
    this.returnModalOpen.set(false);
  }

  async onReturnConfirmed(): Promise<void> {
    this.returnModalOpen.set(false);
    this.router.navigate(['/training/casual-courses']);
  }

  openReject(): void {
    this.rejectReason.set('');
    this.rejectOpen.set(true);
  }

  closeReject(): void {
    this.rejectOpen.set(false);
  }

  async onConfirmReject(): Promise<void> {
    const reason = this.rejectReason().trim();
    if (reason.length < 10 || this.rejectSubmitting()) return;
    this.rejectSubmitting.set(true);
    try {
      await firstValueFrom(this.service.reject(this.courseId(), { reason }));
      this.rejectOpen.set(false);
      this.router.navigate(['/training/casual-courses']);
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
    } finally {
      this.rejectSubmitting.set(false);
    }
  }

  openNotesDrawer(): void {
    this.notesOpen.set(true);
  }

  closeNotesDrawer(): void {
    this.notesOpen.set(false);
  }

  private mapError(e: unknown): string {
    const anyErr = e as { error?: { error?: { code?: string; message?: string } }; message?: string };
    const code = anyErr?.error?.error?.code;
    if (code) {
      const localized = this.l.t(`::${code}`);
      if (localized && localized !== `::${code}`) return localized;
    }
    return anyErr?.error?.error?.message ?? anyErr?.message ?? 'حدث خطأ غير متوقع';
  }
}
