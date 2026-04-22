import { Component, OnInit, computed, effect, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { HrLookupService } from 'src/app/proxy/training/hr-integration/hr-lookup.service';
import { EmployeeLookupDto } from 'src/app/proxy/training/hr-integration/models';

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
  existingEmployeeIds = input<string[]>([]);
  initialSelection = input<string[]>([]);

  selectionChange = output<string[]>();

  employees = signal<EmployeeLookupDto[]>([]);
  loading = signal(false);
  loadError = signal<string | null>(null);
  searchText = signal('');
  selectedIds = signal<Set<string>>(new Set());

  constructor() {
    effect(() => {
      const u = this.unitId();
      if (u) this.loadEmployees(u);
    });

    effect(() => {
      const init = this.initialSelection();
      if (init?.length) this.selectedIds.set(new Set(init));
    });
  }

  ngOnInit(): void {}

  async loadEmployees(unitId: string): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const list = await firstValueFrom(this.hrService.getEmployeesByUnit(unitId));
      this.employees.set(
        (list ?? []).slice().sort((a, b) => {
          const ar = a.rankSortOrder ?? 999;
          const br = b.rankSortOrder ?? 999;
          if (ar !== br) return ar - br;
          return (a.fullNameAr ?? '').localeCompare(b.fullNameAr ?? '', 'ar');
        }),
      );
    } catch (e: any) {
      this.loadError.set(e?.error?.error?.message ?? 'تعذر تحميل قائمة المرشحين');
    } finally {
      this.loading.set(false);
    }
  }

  filteredEmployees = computed(() => {
    const q = this.searchText().trim().toLowerCase();
    const existing = new Set(this.existingEmployeeIds());
    const list = this.employees().filter(e => !existing.has(e.id ?? ''));
    if (!q) return list;
    return list.filter(e => {
      const name = (e.fullNameAr ?? '').toLowerCase();
      const nameEn = (e.fullNameEn ?? '').toLowerCase();
      const svc = (e.serviceNumber ?? '').toLowerCase();
      return name.includes(q) || nameEn.includes(q) || svc.includes(q);
    });
  });

  selectedEmployees = computed(() => {
    const sel = this.selectedIds();
    return this.employees().filter(e => sel.has(e.id ?? ''));
  });

  selectedCount = computed(() => this.selectedIds().size);

  isMaxReached = computed(() => {
    const m = this.maxCount();
    return m > 0 && this.selectedIds().size >= m;
  });

  isValid = computed(() => this.selectedIds().size >= this.minCount());

  onSearchInput(event: Event): void {
    this.searchText.set((event.target as HTMLInputElement).value);
  }

  isSelected(id?: string): boolean { return id ? this.selectedIds().has(id) : false; }

  isDisabled(id?: string): boolean {
    if (!id) return true;
    return !this.isSelected(id) && this.isMaxReached();
  }

  toggleEmployee(employee: EmployeeLookupDto): void {
    const id = employee.id;
    if (!id) return;
    const next = new Set(this.selectedIds());
    if (next.has(id)) next.delete(id);
    else {
      if (this.isMaxReached()) return;
      next.add(id);
    }
    this.selectedIds.set(next);
    this.emitSelection();
  }

  removeChip(id: string, event: Event): void {
    event.stopPropagation();
    const next = new Set(this.selectedIds());
    next.delete(id);
    this.selectedIds.set(next);
    this.emitSelection();
  }

  clearAll(): void {
    this.selectedIds.set(new Set());
    this.emitSelection();
  }

  selectAllVisible(): void {
    const next = new Set(this.selectedIds());
    const max = this.maxCount();
    for (const e of this.filteredEmployees()) {
      if (!e.id) continue;
      if (max > 0 && next.size >= max) break;
      next.add(e.id);
    }
    this.selectedIds.set(next);
    this.emitSelection();
  }

  private emitSelection(): void {
    this.selectionChange.emit(Array.from(this.selectedIds()));
  }

  trackById(_: number, e: EmployeeLookupDto): string { return e.id ?? ''; }
}
