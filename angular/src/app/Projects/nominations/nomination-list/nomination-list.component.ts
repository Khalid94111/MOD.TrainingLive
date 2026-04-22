import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';
import { NominationService } from 'src/app/proxy/training/nominations/nomination.service';
import { NominationDto } from 'src/app/proxy/training/nominations/dtos';
import { NominationStatus } from 'src/app/proxy/training/enums/nomination-status.enum';
import { TrainingPlanItemService, TrainingPlanService } from 'src/app/proxy/training/plans';
import { TrainingPlanDto, TrainingPlanItemDto } from 'src/app/proxy/training/plans/dtos';
import { PlanStatus } from '../../shared';

@Component({
  standalone: true,
  selector: 'app-nomination-list',
  templateUrl: './nomination-list.component.html',
  styleUrls: ['./nomination-list.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule],
})
export class NominationListComponent implements OnInit {
  private nominationService = inject(NominationService);
  private itemService = inject(TrainingPlanItemService);
  private planService = inject(TrainingPlanService);
  private router = inject(Router);

  nominations = signal<NominationDto[]>([]);
  loading = signal(false);
  navigating = signal<string | null>(null);

  // Cache planItem and plan info so we don't re-fetch on each click
  private itemCache = new Map<string, TrainingPlanItemDto>();
  private planCache = new Map<string, TrainingPlanDto>();

  // Filters
  filterPlanItemId = signal('');
  filterStatus = signal<string>('');
  filterReturned = signal<'all' | 'returned' | 'normal'>('all');
  filterSearch = signal('');

  NominationStatus = NominationStatus;

  // Stats
  totalCount = computed(() => this.nominations().length);
  returnedCount = computed(() => this.nominations().filter(n => n.isReturned).length);
  activeCount = computed(() => this.nominations().filter(n => n.status !== NominationStatus.Rejected).length);
  rejectedCount = computed(() => this.nominations().filter(n => n.status === NominationStatus.Rejected).length);

  filteredNominations = computed(() => {
    const q = this.filterSearch().trim().toLowerCase();
    const ret = this.filterReturned();
    const st = this.filterStatus();
    const item = this.filterPlanItemId();
    return this.nominations().filter(n => {
      if (item && n.planItemId !== item) return false;
      if (st !== '' && n.status !== +st) return false;
      if (ret === 'returned' && !n.isReturned) return false;
      if (ret === 'normal' && n.isReturned) return false;
      if (q) {
        const hit =
          (n.employeeName ?? '').toLowerCase().includes(q) ||
          (n.courseName ?? '').toLowerCase().includes(q) ||
          (n.nominatedByName ?? '').toLowerCase().includes(q);
        if (!hit) return false;
      }
      return true;
    });
  });

  uniquePlanItems = computed(() => {
    const seen = new Map<string, string>();
    for (const n of this.nominations()) {
      if (n.planItemId && n.courseName && !seen.has(n.planItemId)) {
        seen.set(n.planItemId, n.courseName);
      }
    }
    return Array.from(seen.entries()).map(([id, name]) => ({ id, name }));
  });

  ngOnInit(): void { this.loadNominations(); }

  async loadNominations(): Promise<void> {
    this.loading.set(true);
    try {
      const r = await firstValueFrom(this.nominationService.getList({ maxResultCount: 500, sorting: 'nominatedAt desc' }));
      this.nominations.set(r.items ?? []);
    } finally {
      this.loading.set(false);
    }
  }

  async onRowClick(n: NominationDto): Promise<void> {
    if (!n.planItemId || !n.id || this.navigating()) return;
    this.navigating.set(n.id);
    try {
      let item = this.itemCache.get(n.planItemId);
      if (!item) {
        item = await firstValueFrom(this.itemService.get(n.planItemId));
        if (item) this.itemCache.set(n.planItemId, item);
      }
      if (!item?.planId) return;

      let plan = this.planCache.get(item.planId);
      if (!plan) {
        plan = await firstValueFrom(this.planService.get(item.planId));
        if (plan) this.planCache.set(item.planId, plan);
      }

      const isApprovalStage =
        plan?.status === PlanStatus.TDApproved
        || plan?.status === PlanStatus.THApproved;

      const target = isApprovalStage ? 'approve' : 'review';
      this.router.navigate(['/training/plans', item.planId, target]);
    } finally {
      this.navigating.set(null);
    }
  }

  onSearchInput(event: Event): void {
    this.filterSearch.set((event.target as HTMLInputElement).value);
  }

  setReturnedFilter(v: 'all' | 'returned' | 'normal'): void { this.filterReturned.set(v); }

  resetFilters(): void {
    this.filterPlanItemId.set('');
    this.filterStatus.set('');
    this.filterReturned.set('all');
    this.filterSearch.set('');
  }

  // Helpers
  getStatusBadge(status?: NominationStatus): string {
    return ({
      [NominationStatus.Nominated]: 'badge-pending',
      [NominationStatus.UTMApproved]: 'badge-open',
      [NominationStatus.UGMApproved]: 'badge-review',
      [NominationStatus.TDApproved]: 'badge-approved',
      [NominationStatus.Rejected]: 'badge-rejected',
    } as Record<number, string>)[status as number] ?? 'badge-draft';
  }

  getStatusText(status?: NominationStatus): string {
    return ({
      [NominationStatus.Nominated]: 'مرشح',
      [NominationStatus.UTMApproved]: 'معتمد UTM',
      [NominationStatus.UGMApproved]: 'معتمد UGM',
      [NominationStatus.TDApproved]: 'معتمد TD',
      [NominationStatus.Rejected]: 'مرفوض',
    } as Record<number, string>)[status as number] ?? '—';
  }

  formatDate(d?: string | null): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('ar-OM');
  }

  trackById(_: number, n: NominationDto): string { return n.id ?? ''; }
}
