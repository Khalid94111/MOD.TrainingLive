import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toObservable } from '@angular/core/rxjs-interop';
import { firstValueFrom, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';

import { CasualCourseService } from 'src/app/proxy/training/casual-courses';
import type {
  CasualCourseDetailDto,
  CreateUpdateCasualCourseDto,
  EstimatePreviewDto,
} from 'src/app/proxy/training/casual-courses/dtos/models';
import { TenantCourseService } from 'src/app/proxy/training/tenant-courses/tenant-course.service';
import type { TenantCourseDto } from 'src/app/proxy/training/tenant-courses/dtos/models';
import { HrLookupService } from 'src/app/proxy/training/hr-integration/hr-lookup.service';

import { CasualCourseStatus, CourseType, TrainingLocalizationHelper } from '../../shared';
import { NominationPickerComponent } from '../../shared/components/nomination-picker/nomination-picker.component';
import { NotesDrawerComponent } from '../../shared/components/notes-drawer/notes-drawer.component';
import { PlanNoteEntityType } from 'src/app/proxy/training/enums/plan-note-entity-type.enum';

@Component({
  standalone: true,
  selector: 'app-casual-course-request',
  templateUrl: './casual-course-request.component.html',
  styleUrls: ['./casual-course-request.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, RouterLink, NominationPickerComponent, NotesDrawerComponent],
})
export class CasualCourseRequestComponent implements OnInit {
  private service = inject(CasualCourseService);
  private tenantCourseService = inject(TenantCourseService);
  private hrService = inject(HrLookupService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  l = inject(TrainingLocalizationHelper);

  CourseType = CourseType;
  CasualCourseStatus = CasualCourseStatus;
  PlanNoteEntityType = PlanNoteEntityType;

  id = signal<string | null>(null);
  casualCourse = signal<CasualCourseDetailDto | null>(null);
  loading = signal(false);
  submitError = signal<string | null>(null);
  notesOpen = signal(false);

  tenantCourses = signal<TenantCourseDto[]>([]);
  currentUnitId = signal<string>('');

  fTenantCourseId = signal<string>('');
  fCourseType = signal<CourseType>(CourseType.Internal);
  fPriority = signal<number>(3);
  fJustification = signal<string>('');
  fDescriptionAr = signal<string>('');
  fObjectivesAr = signal<string>('');
  fDurationYears = signal<number>(0);
  fDurationMonths = signal<number>(0);
  fDurationDays = signal<number>(0);
  fDateFrom = signal<string>('');
  fDateTo = signal<string>('');
  fFundingSource = signal<string>('');
  fNomineeIds = signal<string[]>([]);

  preview = signal<EstimatePreviewDto | null>(null);
  previewLoading = signal(false);

  isEdit = computed(() => !!this.id());
  isReturned = computed(() => this.casualCourse()?.status === CasualCourseStatus.ReturnedToCreator);
  showFundingSource = computed(() => this.fCourseType() !== CourseType.Internal);
  nomineeCount = computed(() => this.fNomineeIds().length);

  dateRangeWarning = computed(() => {
    const from = this.fDateFrom();
    const to = this.fDateTo();
    const declaredDays = this.fDurationDays();
    if (!from || !to || declaredDays <= 0) return null;
    const ms = new Date(to).getTime() - new Date(from).getTime();
    if (Number.isNaN(ms) || ms < 0) return 'نطاق التاريخ غير صالح';
    const actualDays = Math.round(ms / (1000 * 60 * 60 * 24)) + 1;
    if (actualDays !== declaredDays) {
      return `نطاق التاريخ (${actualDays} يوم) لا يطابق المدة المدخلة (${declaredDays} يوم)`;
    }
    return null;
  });

  canSubmit = computed(() => {
    return (
      !!this.fTenantCourseId() &&
      this.fJustification().trim().length > 0 &&
      !!this.fDateFrom() &&
      !!this.fDateTo() &&
      this.fDurationDays() > 0 &&
      this.fNomineeIds().length > 0 &&
      (this.fCourseType() === CourseType.Internal || this.fFundingSource().trim().length > 0)
    );
  });

  private previewInputs = computed(() => ({
    tenantCourseId: this.fTenantCourseId(),
    courseType: this.fCourseType(),
    durationDays: this.fDurationDays(),
    nomineeCount: this.fNomineeIds().length,
  }));

  constructor() {
    toObservable(this.previewInputs)
      .pipe(
        debounceTime(400),
        distinctUntilChanged(
          (a, b) =>
            a.tenantCourseId === b.tenantCourseId &&
            a.courseType === b.courseType &&
            a.durationDays === b.durationDays &&
            a.nomineeCount === b.nomineeCount,
        ),
        switchMap(inputs => {
          if (!inputs.tenantCourseId || (inputs.durationDays ?? 0) < 1 || (inputs.nomineeCount ?? 0) < 1) {
            this.preview.set(null);
            this.previewLoading.set(false);
            return of(null);
          }
          this.previewLoading.set(true);
          return this.service.getEstimatePreview(inputs);
        }),
      )
      .subscribe(result => {
        if (result) this.preview.set(result);
        this.previewLoading.set(false);
      });

    effect(() => {
      if (!this.fTenantCourseId()) {
        this.preview.set(null);
      }
    });
  }

  async ngOnInit(): Promise<void> {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.id.set(idParam);
    this.loading.set(true);
    try {
      await Promise.all([this.loadCurrentEmployee(), this.loadTenantCourses()]);
      if (idParam) {
        await this.loadExisting(idParam);
      }
    } finally {
      this.loading.set(false);
    }
  }

  private async loadCurrentEmployee(): Promise<void> {
    try {
      const me = await firstValueFrom(this.hrService.getCurrentEmployee());
      this.currentUnitId.set(me?.mainUnitId ?? '');
    } catch {
      this.currentUnitId.set('');
    }
  }

  private async loadTenantCourses(): Promise<void> {
    const r = await firstValueFrom(
      this.tenantCourseService.getList({ maxResultCount: 500, isActive: true }),
    );
    this.tenantCourses.set(r.items ?? []);
  }

  private async loadExisting(id: string): Promise<void> {
    const detail = await firstValueFrom(this.service.getDetail(id));
    this.casualCourse.set(detail);
    this.fTenantCourseId.set(detail.tenantCourseId ?? '');
    this.fCourseType.set(detail.courseType ?? CourseType.Internal);
    this.fPriority.set(detail.priority ?? 3);
    this.fJustification.set(detail.justification ?? '');
    this.fDescriptionAr.set(detail.descriptionAr ?? '');
    this.fObjectivesAr.set(detail.objectivesAr ?? '');
    this.fDurationYears.set(detail.durationYears ?? 0);
    this.fDurationMonths.set(detail.durationMonths ?? 0);
    this.fDurationDays.set(detail.durationDays ?? 0);
    this.fDateFrom.set((detail.estimatedDateFrom ?? '').substring(0, 10));
    this.fDateTo.set((detail.estimatedDateTo ?? '').substring(0, 10));
    this.fFundingSource.set(detail.fundingSource ?? '');
    const ids = (detail.nominations ?? [])
      .map(n => n.employeeId)
      .filter((v): v is string => !!v);
    this.fNomineeIds.set(ids);
    if (detail.unitId) this.currentUnitId.set(detail.unitId);
  }

  onTenantCourseChange(value: string): void {
    this.fTenantCourseId.set(value);
    const tc = this.tenantCourses().find(t => t.id === value);
    if (tc && !this.fDescriptionAr()) {
      this.fDescriptionAr.set(tc.catalogCourseNameAr ?? '');
    }
  }

  onCourseTypeChange(value: string): void {
    this.fCourseType.set(+value as CourseType);
  }

  onNomineesChange(ids: string[]): void {
    this.fNomineeIds.set(ids);
  }

  openNotesDrawer(): void {
    this.notesOpen.set(true);
  }

  closeNotesDrawer(): void {
    this.notesOpen.set(false);
  }

  async onSave(isDraft: boolean): Promise<void> {
    if (!isDraft && !this.canSubmit()) return;
    this.submitError.set(null);
    const dto: CreateUpdateCasualCourseDto = {
      tenantCourseId: this.fTenantCourseId(),
      unitId: this.currentUnitId(),
      courseType: this.fCourseType(),
      priority: this.fPriority(),
      justification: this.fJustification().trim(),
      descriptionAr: this.fDescriptionAr().trim() || undefined,
      objectivesAr: this.fObjectivesAr().trim() || undefined,
      durationYears: this.fDurationYears(),
      durationMonths: this.fDurationMonths(),
      durationDays: this.fDurationDays(),
      estimatedDateFrom: this.fDateFrom(),
      estimatedDateTo: this.fDateTo(),
      fundingSource: this.fFundingSource().trim() || undefined,
      nomineeEmployeeIds: this.fNomineeIds(),
    };

    try {
      const wasReturned = this.isReturned();
      const currentId = this.id();
      const saved = currentId
        ? await firstValueFrom(this.service.update(currentId, dto))
        : await firstValueFrom(this.service.create(dto));

      if (!isDraft && saved.id) {
        if (wasReturned) {
          await firstValueFrom(this.service.resubmit(saved.id));
        } else {
          await firstValueFrom(this.service.submit(saved.id));
        }
      }
      this.router.navigate(['/training/casual-courses']);
    } catch (e: unknown) {
      this.submitError.set(this.mapError(e));
    }
  }

  onCancel(): void {
    this.router.navigate(['/training/casual-courses']);
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
