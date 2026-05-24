import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { LocalizationPipe } from '@abp/ng.core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';

import { AnnualPlanSessionService } from 'src/app/proxy/training/annual-plan-sessions/annual-plan-session.service';
import { TrainingPlanItemService } from 'src/app/proxy/training/plans/training-plan-item.service';
import { NominationService } from 'src/app/proxy/training/nominations/nomination.service';
import type {
  CreateInternalSessionDto,
  CreateExternalSessionDto,
} from 'src/app/proxy/training/annual-plan-sessions/dtos/models';
import type { TrainingPlanItemDto } from 'src/app/proxy/training/plans/dtos/models';
import type { NominationDto } from 'src/app/proxy/training/nominations/dtos/models';
import { CourseType } from 'src/app/proxy/training/enums/course-type.enum';
import { NominationStatus } from 'src/app/proxy/training/enums/nomination-status.enum';

import { TrainingLocalizationHelper } from '../../shared';
import {
  SubstituteNomineeDialogComponent,
  type SubstitutionConfirmed,
} from '../substitute-nominee-dialog/substitute-nominee-dialog.component';

type CreateMode = 'internal' | 'external';

interface NomineeRow {
  /** Original employee from the plan-item nomination (never mutates). */
  readonly originalEmployeeId: string;
  readonly originalEmployeeName: string;
  readonly status?: NominationStatus;
  /** Substitution applied locally (not yet saved). */
  substitution: SubstitutionConfirmed | null;
}

// Phase 4C-α (v4.10.0) — PAGE B-1 (Internal) and PAGE B-2 (External) share this shell.
// Route data carries `mode: 'internal' | 'external'`; the date-pickers section is replaced
// with the "Dates come later" banner for the external variant. Both arms share the
// nominees+substitution UX and the substitution dialog.
@Component({
  selector: 'app-create-session',
  standalone: true,
  templateUrl: './create-session.component.html',
  styleUrls: ['./create-session.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, LocalizationPipe, SubstituteNomineeDialogComponent],
})
export class CreateSessionComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly api = inject(AnnualPlanSessionService);
  private readonly planItemService = inject(TrainingPlanItemService);
  private readonly nominationService = inject(NominationService);
  readonly l = inject(TrainingLocalizationHelper);

  readonly CourseType = CourseType;

  // ── Route inputs ──
  readonly mode = signal<CreateMode>('internal');
  readonly planItemId = signal<string>('');

  // ── Data ──
  readonly planItem = signal<TrainingPlanItemDto | null>(null);
  readonly nominees = signal<NomineeRow[]>([]);
  readonly loading = signal<boolean>(true);
  readonly submitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  // ── Form fields ──
  readonly fActualStartDate = signal<string>('');
  readonly fActualEndDate = signal<string>('');
  readonly fNotes = signal<string>('');

  // ── Substitution dialog state ──
  readonly dialogOpen = signal<boolean>(false);
  readonly dialogTarget = signal<NomineeRow | null>(null);

  // ── Derived ──
  readonly isInternal = computed(() => this.mode() === 'internal');
  readonly substitutionCount = computed(() => this.nominees().filter(n => n.substitution).length);
  readonly datesValid = computed(() => {
    if (!this.isInternal()) return true;
    const s = this.fActualStartDate();
    const e = this.fActualEndDate();
    if (!s || !e) return false;
    return s <= e;
  });
  readonly canSubmit = computed(() =>
    !this.loading() && !this.submitting() && this.datesValid() && !!this.planItem());

  ngOnInit(): void {
    const mode = (this.route.snapshot.data['mode'] as CreateMode) ?? 'internal';
    const id = this.route.snapshot.paramMap.get('planItemId') ?? '';
    this.mode.set(mode);
    this.planItemId.set(id);

    if (!id) {
      this.errorMessage.set(this.l.t('::Training.AnnualPlan.CreateSession.MissingPlanItem'));
      this.loading.set(false);
      return;
    }
    this.loadAll();
  }

  private async loadAll(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set(null);
    try {
      const [item, nomList] = await Promise.all([
        firstValueFrom(this.planItemService.get(this.planItemId()).pipe(takeUntilDestroyed(this.destroyRef))),
        firstValueFrom(this.nominationService.getList({
          planItemId: this.planItemId(),
          maxResultCount: 500,
        }).pipe(takeUntilDestroyed(this.destroyRef))),
      ]);
      this.planItem.set(item);

      // Approved candidates only — exclude Rejected/Returned (matches server-side copy logic).
      const rows: NomineeRow[] = (nomList.items ?? [])
        .filter((n: NominationDto) =>
          n.status !== NominationStatus.Rejected && n.status !== NominationStatus.Returned)
        .map((n: NominationDto) => ({
          originalEmployeeId: n.employeeId ?? '',
          originalEmployeeName: n.employeeName ?? '',
          status: n.status,
          substitution: null,
        }));
      this.nominees.set(rows);

      // Seed dates for Internal from plan-item estimated window if available.
      if (this.isInternal() && item.estimatedDateFrom && item.estimatedDateTo) {
        this.fActualStartDate.set(item.estimatedDateFrom.substring(0, 10));
        this.fActualEndDate.set(item.estimatedDateTo.substring(0, 10));
      }
    } catch (err) {
      this.errorMessage.set(this.extractError(err));
    } finally {
      this.loading.set(false);
    }
  }

  // ── Field handlers ──
  onStartDateChange(event: Event): void {
    this.fActualStartDate.set((event.target as HTMLInputElement).value);
  }
  onEndDateChange(event: Event): void {
    this.fActualEndDate.set((event.target as HTMLInputElement).value);
  }
  onNotesChange(event: Event): void {
    this.fNotes.set((event.target as HTMLTextAreaElement).value);
  }

  // ── Substitution ──
  openSubstitution(row: NomineeRow): void {
    this.dialogTarget.set(row);
    this.dialogOpen.set(true);
  }
  cancelSubstitution(row: NomineeRow): void {
    this.nominees.update(list =>
      list.map(r => r === row ? { ...r, substitution: null } : r));
  }
  onDialogConfirmed(sub: SubstitutionConfirmed): void {
    const target = this.dialogTarget();
    if (target) {
      this.nominees.update(list =>
        list.map(r => r.originalEmployeeId === target.originalEmployeeId
          ? { ...r, substitution: sub }
          : r));
    }
    this.dialogOpen.set(false);
    this.dialogTarget.set(null);
  }
  onDialogCancelled(): void {
    this.dialogOpen.set(false);
    this.dialogTarget.set(null);
  }

  // ── Submit ──
  async onSubmit(): Promise<void> {
    if (!this.canSubmit()) return;

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const substitutions = this.nominees()
      .filter(n => n.substitution)
      .map(n => ({
        originalEmployeeId: n.substitution!.originalEmployeeId,
        replacementEmployeeId: n.substitution!.replacementEmployeeId,
        reason: n.substitution!.reason,
      }));

    try {
      if (this.isInternal()) {
        const dto: CreateInternalSessionDto = {
          trainingPlanItemId: this.planItemId(),
          actualStartDate: this.fActualStartDate(),
          actualEndDate: this.fActualEndDate(),
          substitutions,
          notes: this.fNotes() || null,
        };
        const session = await firstValueFrom(
          this.api.createInternalSession(dto).pipe(takeUntilDestroyed(this.destroyRef)));
        if (session?.id) {
          this.router.navigate(['/training/sessions', session.id]);
        }
      } else {
        const dto: CreateExternalSessionDto = {
          trainingPlanItemId: this.planItemId(),
          substitutions,
          notes: this.fNotes() || null,
        };
        const session = await firstValueFrom(
          this.api.createExternalSession(dto).pipe(takeUntilDestroyed(this.destroyRef)));
        if (session?.id) {
          this.router.navigate(['/training/sessions', session.id]);
        }
      }
    } catch (err) {
      this.errorMessage.set(this.extractError(err));
    } finally {
      this.submitting.set(false);
    }
  }

  goBack(): void {
    this.router.navigate(['/training/annual-plan/sessions-queue']);
  }

  // ── Display helpers ──
  priorityCss(priority: number | undefined): string {
    if ((priority ?? 0) >= 3) return 'priority-pill priority-high';
    if (priority === 2) return 'priority-pill priority-medium';
    return 'priority-pill priority-low';
  }
  priorityLabelKey(priority: number | undefined): string {
    if ((priority ?? 0) >= 3) return '::Training.AnnualPlan.SessionsQueue.Priority.High';
    if (priority === 2) return '::Training.AnnualPlan.SessionsQueue.Priority.Medium';
    return '::Training.AnnualPlan.SessionsQueue.Priority.Low';
  }
  courseTypeLabelKey(courseType: CourseType | undefined): string {
    switch (courseType) {
      case CourseType.Internal:              return '::Training.AnnualPlan.SessionsQueue.Type.Internal';
      case CourseType.ExternalLocal:         return '::Training.AnnualPlan.SessionsQueue.Type.ExternalLocal';
      case CourseType.ExternalInternational: return '::Training.AnnualPlan.SessionsQueue.Type.ExternalInternational';
      default: return '';
    }
  }

  private extractError(err: unknown): string {
    if (err && typeof err === 'object') {
      const anyErr = err as { error?: { error?: { message?: string } }; message?: string };
      return anyErr.error?.error?.message ?? anyErr.message ?? this.l.t('::Training.AnnualPlan.CreateSession.GenericError');
    }
    return this.l.t('::Training.AnnualPlan.CreateSession.GenericError');
  }
}
