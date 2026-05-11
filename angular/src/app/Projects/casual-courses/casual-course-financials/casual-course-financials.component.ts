import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { CasualCourseService } from 'src/app/proxy/training/casual-courses';
import type {
  CasualCourseDetailDto,
  CasualCourseFinancialItemDto,
} from 'src/app/proxy/training/casual-courses/dtos/models';

import { TrainingLocalizationHelper } from '../../shared';

@Component({
  standalone: true,
  selector: 'app-casual-course-financials',
  templateUrl: './casual-course-financials.component.html',
  styleUrls: ['./casual-course-financials.component.scss', '../../shared/gtms-design.scss'],
  imports: [CommonModule],
})
export class CasualCourseFinancialsComponent implements OnInit {
  private courseService = inject(CasualCourseService);
  private route = inject(ActivatedRoute);
  l = inject(TrainingLocalizationHelper);

  id = signal<string>('');
  course = signal<CasualCourseDetailDto | null>(null);
  loading = signal<boolean>(false);
  expandedItemId = signal<string | null>(null);

  financialItems = computed<CasualCourseFinancialItemDto[]>(
    () => this.course()?.financialItems ?? [],
  );
  hasItems = computed(() => this.financialItems().length > 0);
  totalEstimatedOMR = computed(() => this.course()?.estimatedTotalCost ?? 0);
  totalActualOMR = computed(() =>
    this.financialItems().reduce((sum, i) => sum + (i.actualAmountOMR ?? 0), 0),
  );

  async ngOnInit(): Promise<void> {
    const id = this.route.parent?.snapshot.paramMap.get('id')
      ?? this.route.snapshot.paramMap.get('id')
      ?? '';
    this.id.set(id);
    if (!id) return;
    this.loading.set(true);
    try {
      const detail = await firstValueFrom(this.courseService.getDetail(id));
      this.course.set(detail);
    } finally {
      this.loading.set(false);
    }
  }

  isExpanded(id: string | undefined): boolean {
    return !!id && this.expandedItemId() === id;
  }

  toggleExpand(id: string | undefined): void {
    if (!id) return;
    this.expandedItemId.update(cur => (cur === id ? null : id));
  }

  formatCurrency(value: number | null | undefined): string {
    return (value ?? 0).toLocaleString('en-US', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 3,
    });
  }
}
