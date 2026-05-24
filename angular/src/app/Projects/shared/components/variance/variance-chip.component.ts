import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';

import { computeVariance, varianceSymbol, VarianceResult } from './variance.helper';

// Phase 4C-α Patch 1 (v4.10.1) — presentational variance pill.
// Inputs are plain numbers (signals) so the chip updates live as the user types
// in the Add-Quote dialog. The 4-tier colour is driven by ./variance-chip.component.scss.
@Component({
  standalone: true,
  selector: 'app-variance-chip',
  template: `
    <span [class]="'variance-chip ' + result().cssClass">
      <span class="vc-symbol">{{ symbol() }}</span>
      <span class="vc-pct">{{ pctDisplay() }}</span>
      <span class="vc-amount">({{ amountDisplay() }} {{ omr }})</span>
    </span>
  `,
  styleUrls: ['./variance-chip.component.scss'],
  imports: [CommonModule],
})
export class VarianceChipComponent {
  quotedPrice = input.required<number>();
  approvedCost = input.required<number>();

  // Phase 3 inline-Arabic convention is preserved here: this is a leaf component
  // shared across both bilingual surfaces, and "ر.ع" is the Arabic OMR symbol
  // that matches the surrounding text in every existing call site.
  readonly omr = 'ر.ع';

  result = computed<VarianceResult>(() =>
    computeVariance(this.quotedPrice() ?? 0, this.approvedCost() ?? 0));

  symbol = computed(() => varianceSymbol(this.result().percentDifference));

  pctDisplay = computed(() => {
    const pct = this.result().percentDifference;
    const sign = pct > 0 ? '+' : '';
    return `${sign}${pct.toFixed(1)}%`;
  });

  amountDisplay = computed(() =>
    this.result().amountOMR.toLocaleString('en-US', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 3,
    }));
}
