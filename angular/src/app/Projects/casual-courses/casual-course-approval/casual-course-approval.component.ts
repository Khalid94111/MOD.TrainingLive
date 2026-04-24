import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { CasualCourseService, CasualCourseFinancialService } from 'src/app/proxy/training/casual-courses';
import type {
  CasualCourseDetailDto,
  CasualCourseFinancialDto,
  EstimatePreviewDto,
} from 'src/app/proxy/training/casual-courses/dtos/models';

import {
  CasualCourseStatus,
  CASUAL_COURSE_STATUS_OPTIONS,
  FundingScenario,
  FUNDING_SCENARIO_OPTIONS,
  TrainingLocalizationHelper,
} from '../../shared';
import { NotesDrawerComponent } from '../../shared/components/notes-drawer/notes-drawer.component';
import { ReturnModalComponent } from '../../shared/components/return-modal/return-modal.component';
import { PlanNoteEntityType } from 'src/app/proxy/training/enums/plan-note-entity-type.enum';

@Component({
  standalone: true,
  selector: 'app-casual-course-approval',
  templateUrl: './casual-course-approval.component.html',
  styleUrls: ['./casual-course-approval.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, RouterLink, NotesDrawerComponent, ReturnModalComponent],
})
export class CasualCourseApprovalComponent implements OnInit {
  private service = inject(CasualCourseService);
  private financialService = inject(CasualCourseFinancialService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  l = inject(TrainingLocalizationHelper);

  CasualCourseStatus = CasualCourseStatus;
  PlanNoteEntityType = PlanNoteEntityType;

  courseId = signal<string>('');
  casualCourse = signal<CasualCourseDetailDto | null>(null);
  financials = signal<CasualCourseFinancialDto[]>([]);
  preview = signal<EstimatePreviewDto | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  actionBusy = signal(false);

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

  grandTotal = computed(() =>
    this.financials().reduce((sum, f) => sum + (f.estimatedAmountOMR ?? 0), 0),
  );

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.courseId.set(id);
    await this.loadAll();
  }

  private async loadAll(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const [detail, financials] = await Promise.all([
        firstValueFrom(this.service.getDetail(this.courseId())),
        firstValueFrom(this.financialService.getListByCasualCourse(this.courseId())),
      ]);
      this.casualCourse.set(detail);
      this.financials.set(financials);
      await this.loadPreview(detail);
    } catch (e: unknown) {
      this.error.set(this.mapError(e));
    } finally {
      this.loading.set(false);
    }
  }

  private async loadPreview(detail: CasualCourseDetailDto): Promise<void> {
    const nomineeIds = (detail.nominations ?? [])
      .map(n => n.employeeId)
      .filter((v): v is string => !!v);
    if (
      !detail.tenantCourseId ||
      detail.courseType === undefined ||
      !detail.durationDays ||
      nomineeIds.length === 0
    ) {
      this.preview.set(null);
      return;
    }
    try {
      const result = await firstValueFrom(
        this.service.getEstimatePreview({
          tenantCourseId: detail.tenantCourseId,
          courseType: detail.courseType,
          durationDays: detail.durationDays,
          nomineeEmployeeIds: nomineeIds,
        }),
      );
      this.preview.set(result);
    } catch {
      // Preview is informational; swallow failures so the page still renders.
      this.preview.set(null);
    }
  }

  rateSourceLabel(source: string | undefined): string {
    if (source === 'RankOverride') return 'معدل الرتبة';
    if (source === 'DefaultAmount') return 'افتراضي';
    return source ?? '—';
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
