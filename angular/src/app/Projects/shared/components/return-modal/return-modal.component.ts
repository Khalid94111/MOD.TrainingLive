import { Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { TrainingPlanService, TrainingPlanItemService } from 'src/app/proxy/training/plans';
import { NominationService } from 'src/app/proxy/training/nominations';
import { CasualCourseService, CasualCourseNominationService } from 'src/app/proxy/training/casual-courses';
import { PlanNoteEntityType } from 'src/app/proxy/training/enums/plan-note-entity-type.enum';

@Component({
  standalone: true,
  selector: 'gtms-return-modal',
  templateUrl: './return-modal.component.html',
  styleUrls: ['./return-modal.component.scss', '../../gtms-design.scss'],
  imports: [CommonModule],
})
export class ReturnModalComponent {
  private planService = inject(TrainingPlanService);
  private itemService = inject(TrainingPlanItemService);
  private nominationService = inject(NominationService);
  private casualCourseService = inject(CasualCourseService);
  private casualCourseNominationService = inject(CasualCourseNominationService);

  isOpen = input.required<boolean>();
  entityType = input.required<PlanNoteEntityType>();
  entityId = input.required<string>();
  title = input<string>('');
  subtitle = input<string>('');

  cancelled = output<void>();
  confirmed = output<string>();

  reason = signal('');
  submitting = signal(false);
  error = signal<string | null>(null);

  readonly MIN_LEN = 10;
  readonly MAX_LEN = 2000;

  get characterCount(): number { return this.reason().length; }
  get isValid(): boolean {
    const l = this.reason().trim().length;
    return l >= this.MIN_LEN && l <= this.MAX_LEN;
  }

  onReasonInput(event: Event): void {
    this.reason.set((event.target as HTMLTextAreaElement).value);
    if (this.error()) this.error.set(null);
  }

  async onConfirm(): Promise<void> {
    if (!this.isValid || this.submitting()) return;
    this.submitting.set(true);
    this.error.set(null);
    try {
      const body = { reason: this.reason().trim() };
      switch (this.entityType()) {
        case PlanNoteEntityType.Plan:
          await firstValueFrom(this.planService.returnToCreator(this.entityId(), body));
          break;
        case PlanNoteEntityType.PlanItem:
          await firstValueFrom(this.itemService.return(this.entityId(), body));
          break;
        case PlanNoteEntityType.Nomination:
          await firstValueFrom(this.nominationService.return(this.entityId(), body));
          break;
        case PlanNoteEntityType.CasualCourse:
          await firstValueFrom(this.casualCourseService.return(this.entityId(), body));
          break;
        case PlanNoteEntityType.CasualCourseNomination:
          await firstValueFrom(this.casualCourseNominationService.return(this.entityId(), body));
          break;
      }
      this.confirmed.emit(body.reason);
      this.reset();
    } catch (e: any) {
      this.error.set(e?.error?.error?.message ?? e?.message ?? 'فشلت عملية الإعادة');
    } finally {
      this.submitting.set(false);
    }
  }

  onCancel(): void {
    this.cancelled.emit();
    this.reset();
  }

  onBackdropClick(event: Event): void {
    if (event.target === event.currentTarget) this.onCancel();
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') this.onCancel();
    if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
      event.preventDefault();
      this.onConfirm();
    }
  }

  private reset(): void {
    this.reason.set('');
    this.error.set(null);
    this.submitting.set(false);
  }

  get entityLabel(): string {
    return ({
      [PlanNoteEntityType.Plan]: 'إعادة الخطة إلى مُنشئها',
      [PlanNoteEntityType.PlanItem]: 'إعادة البند إلى مُنشئه',
      [PlanNoteEntityType.Nomination]: 'إعادة الترشيح',
      [PlanNoteEntityType.CasualCourse]: 'إعادة طلب الدورة العرضية إلى مُنشئه',
      [PlanNoteEntityType.CasualCourseNomination]: 'إعادة ترشيح الدورة العرضية',
    } as Record<number, string>)[this.entityType()];
  }

  get recipientLabel(): string {
    const t = this.entityType();
    if (t === PlanNoteEntityType.Nomination || t === PlanNoteEntityType.CasualCourseNomination) {
      return 'UTM / مسؤول التدريب بالوحدة';
    }
    return 'UTM / مُنشئ الطلب';
  }
}
