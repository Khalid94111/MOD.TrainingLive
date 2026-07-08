import { Component, computed, effect, inject, input, OnInit, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { HrLookupService } from 'src/app/proxy/training/hr-integration/hr-lookup.service';
import { EmployeeLookupDto } from 'src/app/proxy/training/hr-integration/models';

/**
 * Nomination picker that collects nominees by service number lookup
 * instead of browsing a long employee list.
 *
 * Usage (create mode):
 *   <gtms-nomination-picker
 *     [unitId]="unitId"
 *     [minCount]="1"
 *     (selectionChange)="onNomineesChange($event)">
 *   </gtms-nomination-picker>
 *
 * Usage (edit mode — pass pre-resolved employees):
 *   <gtms-nomination-picker
 *     [unitId]="unitId"
 *     [minCount]="1"
 *     [initialEmployees]="resolvedEmployees"
 *     (selectionChange)="onNomineesChange($event)">
 *   </gtms-nomination-picker>
 */
@Component({
  standalone: true,
  selector: 'gtms-nomination-picker',
  templateUrl: './nomination-picker.component.html',
  styleUrls: ['./nomination-picker.component.scss', '../../gtms-design.scss'],
  imports: [CommonModule],
})
export class NominationPickerComponent implements OnInit {
  private hrService = inject(HrLookupService);

  unitId = input.required<string>();
  minCount = input<number>(1);
  maxCount = input<number>(0);
  /** Array of employee IDs for create mode (unused after initial setup) */
  initialSelection = input<string[]>([]);
  /** Pre-resolved employee objects for edit mode (displays chips immediately) */
  initialEmployees = input<Partial<EmployeeLookupDto>[]>([]);

  selectionChange = output<string[]>();

  /** Currently selected employees (resolved from backend) */
  selectedEmployees = signal<EmployeeLookupDto[]>([]);

  /** Input state */
  serviceNumberInput = signal('');
  isResolving = signal(false);
  resolveError = signal<string | null>(null);

  selectedCount = computed(() => this.selectedEmployees().length);

  isMaxReached = computed(() => {
    const m = this.maxCount();
    return m > 0 && this.selectedEmployees().length >= m;
  });

  isValid = computed(() => this.selectedEmployees().length >= this.minCount());

  constructor() {
    effect(() => {
      const pre = this.initialEmployees();
      if (pre && pre.length > 0) {
        // Hydrate from partial dtos passed by parent (edit mode)
        this.selectedEmployees.set(
          pre.map(p => ({
            id: p.id ?? '',
            userId: p.userId ?? '',
            serviceNumber: p.serviceNumber ?? '',
            fullNameAr: p.fullNameAr ?? '',
            fullNameEn: p.fullNameEn ?? '',
            rankNameAr: p.rankNameAr ?? '',
            rankNameEn: p.rankNameEn ?? '',
            rankSortOrder: p.rankSortOrder ?? 0,
            personnelType: p.personnelType ?? '',
            mainUnitId: p.mainUnitId ?? '',
          } as EmployeeLookupDto)),
        );
      }
    });
  }

  ngOnInit(): void {
    this.serviceNumberInput.set('');
    this.resolveError.set(null);
  }

  async onAddByServiceNumber(): Promise<void> {
    const raw = this.serviceNumberInput().trim();
    if (!raw) return;

    if (this.isMaxReached()) {
      this.resolveError.set('تم الوصول للحد الأقصى من المرشحين');
      return;
    }

    const unitId = this.unitId();
    if (!unitId) {
      this.resolveError.set('لم يتم تحديد الوحدة');
      return;
    }

    // Prevent adding duplicate service numbers.
    const alreadyAdded = this.selectedEmployees().some(
      e => (e.serviceNumber ?? '').trim() === raw,
    );
    if (alreadyAdded) {
      this.resolveError.set('هذا الرقم مُضاف مسبقاً');
      return;
    }

    this.isResolving.set(true);
    this.resolveError.set(null);

    try {
      const employee = await firstValueFrom(
        this.hrService.getByServiceNumber(raw, unitId),
      );

      if (!employee || !employee.id) {
        this.resolveError.set('لم يتم العثور على موظف بهذا الرقم في الوحدة المختارة');
        return;
      }

      this.selectedEmployees.update(list => [...list, employee]);
      this.serviceNumberInput.set('');
      this.emitSelection();
    } catch (e: any) {
      const msg = e?.error?.error?.message ?? e?.message ?? 'تعذر البحث عن الموظف';
      this.resolveError.set(msg);
    } finally {
      this.isResolving.set(false);
    }
  }

  onInputKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.onAddByServiceNumber();
    }
  }

  removeEmployee(id: string, event: Event): void {
    event.stopPropagation();
    this.selectedEmployees.update(list => list.filter(e => e.id !== id));
    this.emitSelection();
  }

  clearAll(): void {
    this.selectedEmployees.set([]);
    this.resolveError.set(null);
    this.emitSelection();
  }

  private emitSelection(): void {
    this.selectionChange.emit(this.selectedEmployees().map(e => e.id!).filter(Boolean));
  }
}
