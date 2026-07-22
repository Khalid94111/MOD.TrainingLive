import { Component, inject, OnInit, OnDestroy, TemplateRef, ViewChild, ChangeDetectionStrategy, signal, computed } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbModal, NgbModalModule } from '@ng-bootstrap/ng-bootstrap';
import { LocalizationModule, PermissionDirective, LocalizationService } from '@abp/ng.core';
import { ConfirmationService, ToasterService } from '@abp/ng.theme.shared';
import {
  CreateUpdateTravelTypeDefinitionDto,
  TravelTypeDefinitionDto,
  TravelTypeDefinitionService,
} from '../../services/travel-type-definition.service';

const TYPE_COLORS: Record<number, string> = {
  1: 'linear-gradient(135deg, #2563eb, #60a5fa)',
  2: 'linear-gradient(135deg, #059669, #34d399)',
  3: 'linear-gradient(135deg, #dc2626, #f87171)',
  4: 'linear-gradient(135deg, #7c3aed, #a78bfa)',
  5: 'linear-gradient(135deg, #d97706, #fbbf24)',
  6: 'linear-gradient(135deg, #0891b2, #22d3ee)',
  7: 'linear-gradient(135deg, #ec4899, #f472b6)',
  8: 'linear-gradient(135deg, #4f46e5, #818cf8)',
  9: 'linear-gradient(135deg, #0d9488, #2dd4bf)',
  10: 'linear-gradient(135deg, #f97316, #fb923c)',
};

function getTypeColor(code: number): string {
  return TYPE_COLORS[code] || TYPE_COLORS[(code % 10) || 1];
}

@Component({
  selector: 'app-travel-types',
  standalone: true,
  imports: [CommonModule, FormsModule, NgbModalModule, LocalizationModule, PermissionDirective],
  templateUrl: './travel-types.component.html',
  styleUrls: ['./travel-types.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TravelTypesComponent implements OnInit, OnDestroy {
  private readonly service = inject(TravelTypeDefinitionService);
  private readonly modalService = inject(NgbModal);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly toaster = inject(ToasterService);
  private readonly localizationService = inject(LocalizationService);
  private readonly destroy$ = new Subject<void>();

  readonly travelTypes = signal<TravelTypeDefinitionDto[]>([]);
  readonly filteredTypes = signal<TravelTypeDefinitionDto[]>([]);
  readonly isLoading = signal<boolean>(true);
  readonly editingType = signal<TravelTypeDefinitionDto | null>(null);

  searchText = '';

  form: CreateUpdateTravelTypeDefinitionDto = {
    code: 1,
    name: '',
    isActive: true,
  };

  @ViewChild('typeModal', { static: false }) typeModalRef!: TemplateRef<any>;

  readonly activeTypesCount = computed(() =>
    this.travelTypes().filter(t => t.isActive).length
  );

  getTypeColor = getTypeColor;

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  load(): void {
    this.isLoading.set(true);
    this.service.getList({ maxResultCount: 100 }).pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        const sorted = res.items.sort((a, b) => a.code - b.code);
        this.travelTypes.set(sorted);
        this.filteredTypes.set(sorted);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      },
    });
  }

  onSearch(): void {
    const search = this.searchText.trim().toLowerCase();
    if (!search) {
      this.filteredTypes.set(this.travelTypes());
      return;
    }
    this.filteredTypes.set(
      this.travelTypes().filter(
        t =>
          t.name.toLowerCase().includes(search) ||
          String(t.code).includes(search)
      )
    );
  }

  clearSearch(): void {
    this.searchText = '';
    this.filteredTypes.set(this.travelTypes());
  }

  openModal(type?: TravelTypeDefinitionDto): void {
    this.editingType.set(type || null);
    this.form = type
      ? { code: type.code, name: type.name, isActive: type.isActive }
      : { code: this.getNextCode(), name: '', isActive: true };

    this.modalService.open(this.typeModalRef, {
      backdrop: 'static',
      centered: true,
      windowClass: 'type-modal-window',
    });
  }

  isFormValid(): boolean {
    return this.form.code >= 1 && !!this.form.name?.trim();
  }

  save(): void {
    const action = this.editingType()
      ? this.service.update(this.editingType()!.id, this.form)
      : this.service.create(this.form);

    action.subscribe({
      next: () => {
        this.service.invalidateCache();
        this.toaster.success(this.localizationService.instant('Travel::SavedSuccessfully'));
        this.load();
      },
      error: () => {
        this.toaster.error(this.localizationService.instant('Travel::SaveFailed'));
      },
    });
  }

  deleteType(id: string): void {
    this.confirmationService
      .warn(
        this.localizationService.instant('Travel::DeleteTypeConfirmation'),
        this.localizationService.instant('Travel::AreYouSure')
      )
      .subscribe((status) => {
        if (status !== 'confirm') return;

        this.service.delete(id).subscribe({
          next: () => {
            this.service.invalidateCache();
            this.toaster.success(this.localizationService.instant('Travel::DeletedSuccessfully'));
            this.load();
          },
          error: () => {
            this.toaster.error(this.localizationService.instant('Travel::DeleteFailed'));
          },
        });
      });
  }

  getTypeLabel(code: number): string {
    return `Travel::TravelType.${code}`;
  }

  private getNextCode(): number {
    const max = this.travelTypes().reduce((value, type) => Math.max(value, type.code), 0);
    return max + 1;
  }
}
