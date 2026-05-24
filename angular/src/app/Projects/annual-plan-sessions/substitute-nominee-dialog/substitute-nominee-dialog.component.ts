import {
  Component, DestroyRef, EventEmitter, OnChanges, Output,
  SimpleChanges, computed, inject, input, signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';

import { AnnualPlanSessionService } from 'src/app/proxy/training/annual-plan-sessions/annual-plan-session.service';
import type { AvailableSubstituteDto } from 'src/app/proxy/training/annual-plan-sessions/dtos/models';

import { TrainingLocalizationHelper } from '../../shared';

export interface SubstitutionConfirmed {
  originalEmployeeId: string;
  replacementEmployeeId: string;
  replacementName: string;
  rankNameAr?: string | null;
  reason: string | null;
}

// Phase 4C-α (v4.10.0) — substitution dialog used by PAGE B-1 + B-2.
//
// Lists same-rank candidates from GetAvailableSubstitutesAsync (excludes already-nominated
// employees server-side), forces the user to pick one + optionally enter a reason, then
// emits to the parent. Locked nominee (original) is displayed read-only as context.
@Component({
  selector: 'app-substitute-nominee-dialog',
  standalone: true,
  templateUrl: './substitute-nominee-dialog.component.html',
  styleUrls: ['./substitute-nominee-dialog.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, LocalizationPipe],
})
export class SubstituteNomineeDialogComponent implements OnChanges {
  private readonly api = inject(AnnualPlanSessionService);
  private readonly destroyRef = inject(DestroyRef);
  readonly l = inject(TrainingLocalizationHelper);

  // ── Inputs ──
  readonly open = input<boolean>(false);
  readonly planItemId = input.required<string>();
  readonly originalEmployeeId = input.required<string>();
  readonly originalEmployeeName = input<string>('');

  // ── Outputs ──
  @Output() confirmed = new EventEmitter<SubstitutionConfirmed>();
  @Output() cancelled = new EventEmitter<void>();

  // ── State ──
  readonly candidates = signal<AvailableSubstituteDto[]>([]);
  readonly loading = signal<boolean>(false);
  readonly error = signal<string | null>(null);
  readonly fReplacementId = signal<string>('');
  readonly fReason = signal<string>('');

  readonly selectedCandidate = computed(() =>
    this.candidates().find(c => c.employeeId === this.fReplacementId()) ?? null);

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open'] && this.open()) {
      this.reset();
      this.loadCandidates();
    }
  }

  private async loadCandidates(): Promise<void> {
    if (!this.planItemId() || !this.originalEmployeeId()) return;
    this.loading.set(true);
    this.error.set(null);
    try {
      const list = await firstValueFrom(
        this.api.getAvailableSubstitutes(this.planItemId(), this.originalEmployeeId())
          .pipe(takeUntilDestroyed(this.destroyRef)),
      );
      this.candidates.set(list ?? []);
    } catch (err) {
      this.error.set(this.extractError(err));
      this.candidates.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  private reset(): void {
    this.candidates.set([]);
    this.fReplacementId.set('');
    this.fReason.set('');
    this.error.set(null);
  }

  onReplacementChange(event: Event): void {
    const v = (event.target as HTMLSelectElement).value;
    this.fReplacementId.set(v);
  }

  onReasonChange(event: Event): void {
    const v = (event.target as HTMLTextAreaElement).value;
    this.fReason.set(v);
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  onConfirm(): void {
    const candidate = this.selectedCandidate();
    if (!candidate || !candidate.employeeId) {
      this.error.set(this.l.t('::Training.AnnualPlan.Substitute.PickFirst'));
      return;
    }
    this.confirmed.emit({
      originalEmployeeId: this.originalEmployeeId(),
      replacementEmployeeId: candidate.employeeId,
      replacementName: candidate.fullNameAr ?? candidate.fullNameEn ?? '',
      rankNameAr: candidate.rankNameAr,
      reason: this.fReason().trim() || null,
    });
  }

  private extractError(err: unknown): string {
    if (err && typeof err === 'object') {
      const anyErr = err as { error?: { error?: { message?: string } }; message?: string };
      return anyErr.error?.error?.message ?? anyErr.message ?? this.l.t('::Training.AnnualPlan.Substitute.GenericError');
    }
    return this.l.t('::Training.AnnualPlan.Substitute.GenericError');
  }
}
