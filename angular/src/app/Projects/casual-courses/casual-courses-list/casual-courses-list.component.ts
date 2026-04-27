import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { PermissionService } from '@abp/ng.core';
import { CasualCourseService } from 'src/app/proxy/training/casual-courses';
import type { CasualCourseDto } from 'src/app/proxy/training/casual-courses/dtos/models';
import {
  CasualCourseStatus,
  CASUAL_COURSE_STATUS_OPTIONS,
  TrainingLocalizationHelper,
} from '../../shared';
import { actionForRow } from '../models/casual-course-view-model';

@Component({
  standalone: true,
  selector: 'app-casual-courses-list',
  templateUrl: './casual-courses-list.component.html',
  styleUrls: ['./casual-courses-list.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule, RouterLink],
})
export class CasualCoursesListComponent implements OnInit {
  private service = inject(CasualCourseService);
  private permissionService = inject(PermissionService);
  private router = inject(Router);
  l = inject(TrainingLocalizationHelper);

  readonly STATUS_OPTIONS = CASUAL_COURSE_STATUS_OPTIONS;

  rows = signal<CasualCourseDto[]>([]);
  loading = signal(true);
  canCreate = signal(false);

  fStatus = signal<string>('');
  fSearch = signal<string>('');
  fOnlyMine = signal<boolean>(false);

  filteredRows = computed(() => {
    const q = this.fSearch().trim().toLowerCase();
    const status = this.fStatus();
    return this.rows().filter(r => {
      if (status !== '' && r.status !== +status) return false;
      if (q) {
        const name = (r.courseNameAr ?? '').toLowerCase();
        const fundingName = (r.fundingSourceName ?? '').toLowerCase();
        const fundingCode = (r.fundingSourceVoteCode ?? '').toLowerCase();
        if (!name.includes(q) && !fundingName.includes(q) && !fundingCode.includes(q)) return false;
      }
      return true;
    });
  });

  async ngOnInit(): Promise<void> {
    this.canCreate.set(this.permissionService.getGrantedPolicy('Training.CasualCourses.Create'));
    await this.loadRows();
  }

  async loadRows(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await firstValueFrom(
        this.service.getList({
          maxResultCount: 1000,
          onlyMyRequests: this.fOnlyMine(),
        }),
      );
      this.rows.set(result.items ?? []);
    } finally {
      this.loading.set(false);
    }
  }

  onStatusChange(value: string): void {
    this.fStatus.set(value);
  }

  onSearchChange(value: string): void {
    this.fSearch.set(value);
  }

  async onOnlyMineChange(value: boolean): Promise<void> {
    this.fOnlyMine.set(value);
    await this.loadRows();
  }

  statusCss(s: CasualCourseStatus | undefined): string {
    if (s === undefined) return '';
    return this.STATUS_OPTIONS.find(o => o.value === s)?.cssClass ?? '';
  }

  statusLabel(s: CasualCourseStatus | undefined): string {
    if (s === undefined) return '';
    const opt = this.STATUS_OPTIONS.find(o => o.value === s);
    return opt ? this.l.t(opt.key) : '';
  }

  actionLabel(row: CasualCourseDto): string {
    return actionForRow(row).labelAr;
  }

  onRowClick(row: CasualCourseDto): void {
    if (!row.id) return;
    const action = actionForRow(row);
    if (action.route) {
      this.router.navigate([action.route(row.id)]);
    }
  }

  formatCost(cost: number | null | undefined): string {
    if (cost === null || cost === undefined) return '—';
    return cost.toLocaleString('en-US', { minimumFractionDigits: 3, maximumFractionDigits: 3 });
  }
}
