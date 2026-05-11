import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { CasualCourseService } from 'src/app/proxy/training/casual-courses';
import type { CasualCourseDetailDto } from 'src/app/proxy/training/casual-courses/dtos/models';
import { TravelInstructionService } from 'src/app/proxy/training/execution/travel-instruction.service';
import type {
  CreateUpdateTravelInstructionDto,
  TravelInstructionDto,
} from 'src/app/proxy/training/execution/dtos/models';
import { TravelInstructionStatus } from 'src/app/proxy/training/enums/travel-instruction-status.enum';

import { TrainingLocalizationHelper } from '../../shared';

type ParentArm = 'casualCourse' | 'session';

@Component({
  standalone: true,
  selector: 'app-casual-course-travel-instructions',
  templateUrl: './casual-course-travel-instructions.component.html',
  styleUrls: ['./casual-course-travel-instructions.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule],
})
export class CasualCourseTravelInstructionsComponent implements OnInit {
  private courseService = inject(CasualCourseService);
  private travelService = inject(TravelInstructionService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  l = inject(TrainingLocalizationHelper);

  TravelInstructionStatus = TravelInstructionStatus;

  parentArm = signal<ParentArm>('casualCourse');
  parentId = signal<string>('');
  embedded = signal<boolean>(false);

  course = signal<CasualCourseDetailDto | null>(null);
  instruction = signal<TravelInstructionDto | null>(null);
  loading = signal(false);
  saving = signal(false);
  saveError = signal<string | null>(null);
  saveSuccess = signal<string | null>(null);

  // Form fields
  fDepartureDate = signal<string>('');
  fArrivalDate = signal<string>('');
  fReturnDate = signal<string>('');
  fArrivalBackDate = signal<string>('');

  fVisaRequired = signal<boolean>(false);
  fVisaNotes = signal<string>('');

  fInsuranceArranged = signal<boolean>(false);
  fInsuranceProvider = signal<string>('');

  fTicketsBooked = signal<boolean>(false);
  fTicketReference = signal<string>('');

  fOverrideTravelDays = signal<number | null>(null);

  isDraft = computed(() => {
    const s = this.instruction()?.status;
    return s === undefined || s === TravelInstructionStatus.Draft;
  });

  isIssued    = computed(() => this.instruction()?.status === TravelInstructionStatus.Issued);
  isCancelled = computed(() => this.instruction()?.status === TravelInstructionStatus.Cancelled);
  isReadOnly  = computed(() => !this.isDraft());

  // Live calculated travel days
  calculatedTravelDays = computed(() => {
    const d = this.fDepartureDate();
    const a = this.fArrivalBackDate();
    if (!d || !a) return 0;
    const dDate = new Date(d);
    const aDate = new Date(a);
    if (Number.isNaN(dDate.getTime()) || Number.isNaN(aDate.getTime())) return 0;
    if (aDate < dDate) return 0;
    const diffMs = aDate.getTime() - dDate.getTime();
    return Math.floor(diffMs / (1000 * 60 * 60 * 24)) + 1;
  });

  effectiveTravelDays = computed(() => {
    const o = this.fOverrideTravelDays();
    return o != null && o > 0 ? o : this.calculatedTravelDays();
  });

  hasOverride = computed(() => {
    const o = this.fOverrideTravelDays();
    return o != null && o > 0;
  });

  datesValid = computed(() => {
    const d = this.fDepartureDate();
    const a = this.fArrivalDate();
    const r = this.fReturnDate();
    const ab = this.fArrivalBackDate();
    if (!d || !a || !r || !ab) return false;
    return d <= a && a <= r && r <= ab;
  });

  visaSectionValid = computed(() => {
    if (!this.fVisaRequired()) return true;
    return this.fVisaNotes().trim().length > 0;
  });

  insuranceSectionValid = computed(() => {
    if (!this.fInsuranceArranged()) return true;
    return this.fInsuranceProvider().trim().length > 0;
  });

  ticketsSectionValid = computed(() =>
    this.fTicketsBooked() && this.fTicketReference().trim().length > 0
  );

  canIssue = computed(() =>
    this.datesValid() &&
    this.visaSectionValid() &&
    this.insuranceSectionValid() &&
    this.ticketsSectionValid()
  );

  ngOnInit(): void {
    const arm = (this.route.snapshot.data['parentArm'] as ParentArm) ?? 'casualCourse';
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.parentArm.set(arm);
    this.parentId.set(id);
    this.embedded.set(!!this.route.snapshot.data['embedded']);
    this.bootstrap();
  }

  private async bootstrap(): Promise<void> {
    this.loading.set(true);
    try {
      await Promise.all([this.loadCourse(), this.loadInstruction()]);
      const inst = this.instruction();
      if (inst) this.populateFormFromInstruction(inst);
      else this.seedDatesFromCourse();
    } finally {
      this.loading.set(false);
    }
  }

  private async loadCourse(): Promise<void> {
    if (this.parentArm() !== 'casualCourse') return;
    const detail = await firstValueFrom(this.courseService.getDetail(this.parentId()));
    this.course.set(detail);
  }

  private async loadInstruction(): Promise<void> {
    const casualCourseId = this.parentArm() === 'casualCourse' ? this.parentId() : '';
    const sessionId      = this.parentArm() === 'session'      ? this.parentId() : '';
    try {
      const result = await firstValueFrom(this.travelService.getByParent(casualCourseId, sessionId));
      this.instruction.set(result ?? null);
    } catch {
      this.instruction.set(null);
    }
  }

  private populateFormFromInstruction(inst: TravelInstructionDto): void {
    this.fDepartureDate.set((inst.departureDate ?? '').substring(0, 10));
    this.fArrivalDate.set((inst.arrivalDate ?? '').substring(0, 10));
    this.fReturnDate.set((inst.returnDate ?? '').substring(0, 10));
    this.fArrivalBackDate.set((inst.arrivalBackDate ?? '').substring(0, 10));
    this.fVisaRequired.set(!!inst.visaRequired);
    this.fVisaNotes.set(inst.visaNotes ?? '');
    this.fInsuranceArranged.set(!!inst.insuranceArranged);
    this.fInsuranceProvider.set(inst.insuranceProvider ?? '');
    this.fTicketsBooked.set(!!inst.ticketsBooked);
    this.fTicketReference.set(inst.ticketReference ?? '');
    this.fOverrideTravelDays.set(inst.overrideTravelDays ?? null);
  }

  private seedDatesFromCourse(): void {
    const c = this.course();
    if (!c) return;
    const start = (c.actualStartDate ?? c.estimatedDateFrom ?? '').substring(0, 10);
    const end   = (c.actualEndDate   ?? c.estimatedDateTo   ?? '').substring(0, 10);
    if (start) {
      const before = this.shiftDate(start, -1);
      this.fDepartureDate.set(before);
      this.fArrivalDate.set(before);
    }
    if (end) {
      this.fReturnDate.set(end);
      this.fArrivalBackDate.set(this.shiftDate(end, 1));
    }
  }

  private shiftDate(isoDate: string, days: number): string {
    const d = new Date(isoDate);
    if (Number.isNaN(d.getTime())) return isoDate;
    d.setDate(d.getDate() + days);
    return d.toISOString().substring(0, 10);
  }

  private buildDto(): CreateUpdateTravelInstructionDto {
    return {
      casualCourseId: this.parentArm() === 'casualCourse' ? this.parentId() : null,
      sessionId:      this.parentArm() === 'session'      ? this.parentId() : null,
      departureDate: this.fDepartureDate(),
      arrivalDate: this.fArrivalDate(),
      returnDate: this.fReturnDate(),
      arrivalBackDate: this.fArrivalBackDate(),
      visaRequired: this.fVisaRequired(),
      visaNotes: this.fVisaNotes() || null,
      insuranceArranged: this.fInsuranceArranged(),
      insuranceProvider: this.fInsuranceProvider() || null,
      ticketsBooked: this.fTicketsBooked(),
      ticketReference: this.fTicketReference() || null,
      overrideTravelDays: this.fOverrideTravelDays(),
    };
  }

  async onSaveDraft(): Promise<void> {
    if (!this.datesValid()) {
      this.saveError.set(this.l.t('::Training:TravelInstruction:InvalidDateOrder'));
      return;
    }
    this.saveError.set(null);
    this.saveSuccess.set(null);
    this.saving.set(true);
    try {
      const dto = this.buildDto();
      const result = await firstValueFrom(this.travelService.createOrUpdate(dto));
      this.instruction.set(result);
      this.populateFormFromInstruction(result);
      this.saveSuccess.set('تم حفظ المسودة بنجاح');
    } catch (err: unknown) {
      this.saveError.set(this.extractError(err));
    } finally {
      this.saving.set(false);
    }
  }

  async onIssue(): Promise<void> {
    if (!this.canIssue()) {
      this.saveError.set('استكمل جميع المتطلبات قبل الإصدار: تواريخ صحيحة، التذاكر، التأشيرة، التأمين');
      return;
    }
    this.saveError.set(null);
    this.saveSuccess.set(null);
    this.saving.set(true);
    try {
      // Persist any pending edits first (upsert)
      const dto = this.buildDto();
      const upserted = await firstValueFrom(this.travelService.createOrUpdate(dto));
      this.instruction.set(upserted);

      const issued = await firstValueFrom(this.travelService.issue(upserted.id));
      this.instruction.set(issued);
      this.populateFormFromInstruction(issued);
      this.saveSuccess.set('تم إصدار تعليمات السفر بنجاح');
    } catch (err: unknown) {
      this.saveError.set(this.extractError(err));
    } finally {
      this.saving.set(false);
    }
  }

  async onCancel(): Promise<void> {
    const inst = this.instruction();
    if (!inst) return;
    if (!confirm('هل أنت متأكد من إلغاء تعليمات السفر؟ هذه العملية لا يمكن التراجع عنها.')) return;

    this.saveError.set(null);
    this.saveSuccess.set(null);
    this.saving.set(true);
    try {
      const cancelled = await firstValueFrom(this.travelService.cancel(inst.id));
      this.instruction.set(cancelled);
      this.populateFormFromInstruction(cancelled);
      this.saveSuccess.set('تم إلغاء تعليمات السفر');
    } catch (err: unknown) {
      this.saveError.set(this.extractError(err));
    } finally {
      this.saving.set(false);
    }
  }

  goBack(): void {
    if (this.parentArm() === 'casualCourse') {
      this.router.navigate(['/training/casual-courses', this.parentId(), 'details']);
    } else {
      this.router.navigate(['/training']);
    }
  }

  statusLabel(): string {
    const s = this.instruction()?.status;
    switch (s) {
      case TravelInstructionStatus.Issued:    return 'مُصدَرة';
      case TravelInstructionStatus.Cancelled: return 'ملغاة';
      default: return 'مسودة — لم تُصدر بعد';
    }
  }

  statusCssClass(): string {
    const s = this.instruction()?.status;
    switch (s) {
      case TravelInstructionStatus.Issued:    return 'status-pill status-issued';
      case TravelInstructionStatus.Cancelled: return 'status-pill status-cancelled';
      default: return 'status-pill status-draft';
    }
  }

  private extractError(err: unknown): string {
    if (err && typeof err === 'object') {
      const anyErr = err as { error?: { error?: { message?: string } }; message?: string };
      return anyErr.error?.error?.message ?? anyErr.message ?? 'حدث خطأ أثناء تنفيذ العملية';
    }
    return 'حدث خطأ أثناء تنفيذ العملية';
  }
}
