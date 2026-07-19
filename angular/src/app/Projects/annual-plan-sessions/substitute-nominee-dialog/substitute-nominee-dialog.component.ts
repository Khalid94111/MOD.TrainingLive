import {
  Component, DestroyRef, EventEmitter, OnChanges, Output,
  SimpleChanges, inject, input, signal,
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
// The user types the substitute's service number; it is matched against the eligible
// pool from GetAvailableSubstitutesAsync (same-rank-as-original, active, not already
// nominated — enforced server-side). A successful match shows a confirmation card;
// the reason is optional. Emits to the parent on confirm.
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
  readonly fServiceNumber = signal<string>('');
  readonly fReason = signal<string>('');

  /** Candidate matched by the typed service number (from the eligible pool). */
  readonly resolved = signal<AvailableSubstituteDto | null>(null);

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
    this.fServiceNumber.set('');
    this.resolved.set(null);
    this.fReason.set('');
    this.error.set(null);
  }

  onServiceNumberInput(event: Event): void {
    this.fServiceNumber.set((event.target as HTMLInputElement).value);
    // Editing the number invalidates the previous match.
    this.resolved.set(null);
    this.error.set(null);
  }

  onServiceNumberKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.onResolve();
    }
  }

  // Matches the typed service number against the eligible (same-rank, not-yet-nominated) pool.
  onResolve(): void {
    const raw = this.fServiceNumber().trim();
    if (!raw) return;
    const match = this.candidates().find(c => (c.serviceNumber ?? '').trim() === raw) ?? null;
    this.resolved.set(match);
    this.error.set(match ? null : this.l.t('::Training.AnnualPlan.Substitute.NotEligible'));
  }

  onReasonChange(event: Event): void {
    const v = (event.target as HTMLTextAreaElement).value;
    this.fReason.set(v);
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  onConfirm(): void {
    const candidate = this.resolved();
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
