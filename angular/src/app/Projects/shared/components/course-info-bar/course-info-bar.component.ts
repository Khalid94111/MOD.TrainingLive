import { Component, computed, input } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';

// Phase 4C-α Patch 1 (v4.10.1) — persistent course context bar that sits below
// the status pipeline on Session Detail and Casual Course Detail pages.
// Polymorphic via `variant`: 'session' hides the funding-scenario chip and shows
// Unit Total; 'casual' shows the scenario and omits Unit Total.

export interface CourseInfoBarData {
  // Common fields
  courseName: string;
  unitName?: string | null;
  durationDays: number;
  nomineesCount: number;
  officersCount: number;
  enlistedCount: number;
  approvedCostOMR: number;

  // Session-only
  unitTotalOMR?: number | null;

  // Casual-only
  fundingScenarioLabel?: string | null;
}

@Component({
  standalone: true,
  selector: 'app-course-info-bar',
  templateUrl: './course-info-bar.component.html',
  styleUrls: ['./course-info-bar.component.scss'],
  imports: [CommonModule, LocalizationPipe, DecimalPipe],
})
export class CourseInfoBarComponent {
  data = input.required<CourseInfoBarData>();
  variant = input<'casual' | 'session'>('session');

  showScenario = computed(() =>
    this.variant() === 'casual' && !!this.data().fundingScenarioLabel);

  showUnitTotal = computed(() => {
    if (this.variant() !== 'session') return false;
    const v = this.data().unitTotalOMR;
    return v !== null && v !== undefined;
  });

  showUnit = computed(() => !!this.data().unitName);
}
