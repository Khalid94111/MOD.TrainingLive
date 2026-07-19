import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { PermissionService } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';
import {
  CenterPlanItemService,
  CenterPlanService,
  TrainingCenterService,
} from 'src/app/proxy/training/centers';
import {
  CenterPlanNominationDto,
  TrainingCenterDto,
  TrainingCenterPlanDto,
} from 'src/app/proxy/training/centers/dtos';
import { TrainingLocalizationHelper } from '../../shared';

@Component({
  selector: 'app-center-nominations',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './center-nominations.component.html',
  styleUrl: './center-nominations.component.scss',
})
export class CenterNominationsComponent implements OnInit {
  private readonly nominationService = inject(CenterPlanItemService);
  private readonly centerService = inject(TrainingCenterService);
  private readonly planService = inject(CenterPlanService);
  private readonly permissionService = inject(PermissionService);
  private readonly toaster = inject(ToasterService);
  private readonly l = inject(TrainingLocalizationHelper);

  canAdjustCapacity = computed(() => this.permissionService.getGrantedPolicy('Training.CenterPlanItems.AdjustCapacity'));

  nominations = signal<CenterPlanNominationDto[]>([]);
  centers = signal<TrainingCenterDto[]>([]);
  centerPlans = signal<TrainingCenterPlanDto[]>([]);

  isLoading = signal(false);
  totalCount = signal(0);
  currentPage = signal(1);
  pageSize = signal(10);

  filterCenterId = signal<string | null>(null);
  filterCenterPlanId = signal<string | null>(null);

  expandedIds = signal<Set<string>>(new Set());
  expandedBookingIds = signal<Set<string>>(new Set());

  // Capacity adjustment dialog
  isCapacityDialogOpen = signal(false);
  editingItem = signal<CenterPlanNominationDto | null>(null);
  newCapacity = signal<number>(0);
  savingCapacity = signal(false);

  hasActiveFilter = computed(() => !!this.filterCenterId() || !!this.filterCenterPlanId());

  async ngOnInit(): Promise<void> {
    await Promise.all([this.loadCenters(), this.loadCenterPlans()]);
    await this.loadNominations();
  }

  async loadCenters(): Promise<void> {
    try {
      const r = await firstValueFrom(this.centerService.getList({ maxResultCount: 200 }));
      this.centers.set(r.items ?? []);
    } catch {
      this.centers.set([]);
    }
  }

  async loadCenterPlans(): Promise<void> {
    try {
      const r = await firstValueFrom(this.planService.getList({ maxResultCount: 200 }));
      this.centerPlans.set(r.items ?? []);
    } catch {
      this.centerPlans.set([]);
    }
  }

  async loadNominations(): Promise<void> {
    this.isLoading.set(true);
    try {
      const r = await firstValueFrom(
        this.nominationService.getNominations({
          centerId: this.filterCenterId(),
          centerPlanId: this.filterCenterPlanId(),
          skipCount: (this.currentPage() - 1) * this.pageSize(),
          maxResultCount: this.pageSize(),
          sorting: 'creationTime desc',
        })
      );
      this.nominations.set(r.items ?? []);
      this.totalCount.set(r.totalCount ?? 0);
    } catch {
      this.nominations.set([]);
      this.totalCount.set(0);
    } finally {
      this.isLoading.set(false);
    }
  }

  async applyFilters(): Promise<void> {
    this.currentPage.set(1);
    await this.loadNominations();
  }

  async clearFilters(): Promise<void> {
    this.filterCenterId.set(null);
    this.filterCenterPlanId.set(null);
    this.currentPage.set(1);
    await this.loadNominations();
  }

  async onPageChange(page: number): Promise<void> {
    this.currentPage.set(page);
    await this.loadNominations();
  }

  get totalPages(): number {
    return Math.ceil(this.totalCount() / this.pageSize()) || 1;
  }

  openCapacityDialog(item: CenterPlanNominationDto): void {
    this.editingItem.set(item);
    this.newCapacity.set(item.capacity ?? 0);
    this.isCapacityDialogOpen.set(true);
  }

  closeCapacityDialog(): void {
    this.isCapacityDialogOpen.set(false);
    this.editingItem.set(null);
    this.newCapacity.set(0);
  }

  async saveCapacity(): Promise<void> {
    const item = this.editingItem();
    if (!item || this.savingCapacity()) return;

    const capacity = this.newCapacity();
    if (capacity < 1) {
      this.toaster.error('السعة يجب أن تكون 1 على الأقل');
      return;
    }
    if ((item.reservedSeats ?? 0) > capacity) {
      this.toaster.error(`لا يمكن تقليل السعة عن عدد المقاعد المحجوزة (${item.reservedSeats})`);
      return;
    }

    this.savingCapacity.set(true);
    try {
      await firstValueFrom(
        this.nominationService.adjustCapacity(item.id!, { capacity })
      );
      this.toaster.success('تم تحديث السعة');
      this.closeCapacityDialog();
      await this.loadNominations();
    } catch (e: any) {
      const msg = e?.error?.error?.message ?? e?.message ?? 'تعذّر تحديث السعة';
      this.toaster.error(msg);
    } finally {
      this.savingCapacity.set(false);
    }
  }

  toggleExpand(id?: string): void {
    if (!id) return;
    this.expandedIds.update(set => {
      const next = new Set(set);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  isExpanded(id?: string): boolean {
    return !!id && this.expandedIds().has(id);
  }

  toggleBooking(id?: string): void {
    if (!id) return;
    this.expandedBookingIds.update(set => {
      const next = new Set(set);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  isBookingExpanded(id?: string): boolean {
    return !!id && this.expandedBookingIds().has(id);
  }

  formatDate(d?: string | null): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('ar-OM');
  }
}
