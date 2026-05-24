// Phase 4C-α Patch 1 (v4.10.1) — pure variance computation for the Info Bar /
// quotes table / Add-Quote dialog / Pick-Winner dialog. Centralises the four-tier
// thresholding so every surface renders the same colour for the same delta.
//
//   ok       quoted < approved − 2%   (significant savings)
//   neutral  within ±2% of approved   (noise band)
//   warn     +2%  to +10%             (moderate overrun)
//   danger   > +10%                   (significant overrun)
//
// approvedCost ≤ 0 collapses to 'neutral' with zero deltas; the caller decides
// whether to show "—" instead of a chip.

export type VarianceTier = 'ok' | 'neutral' | 'warn' | 'danger';

export interface VarianceResult {
  amountOMR: number;          // |quoted − approved|
  signedAmountOMR: number;    // quoted − approved (negative = savings)
  percentDifference: number;  // ((quoted − approved) / approved) * 100
  tier: VarianceTier;
  cssClass: string;           // `variance-${tier}`
}

export function computeVariance(quotedPriceOMR: number, approvedCostOMR: number): VarianceResult {
  if (!approvedCostOMR || approvedCostOMR <= 0) {
    return {
      amountOMR: 0,
      signedAmountOMR: 0,
      percentDifference: 0,
      tier: 'neutral',
      cssClass: 'variance-neutral',
    };
  }

  const signed = quotedPriceOMR - approvedCostOMR;
  const pct = (signed / approvedCostOMR) * 100;

  let tier: VarianceTier;
  if (pct < -2) tier = 'ok';
  else if (pct < 2) tier = 'neutral';
  else if (pct < 10) tier = 'warn';
  else tier = 'danger';

  return {
    amountOMR: Math.abs(signed),
    signedAmountOMR: signed,
    percentDifference: pct,
    tier,
    cssClass: `variance-${tier}`,
  };
}

/** "↑" / "↓" / "=" — used by chips and inline indicators. */
export function varianceSymbol(pct: number): '↑' | '↓' | '=' {
  if (pct < -0.01) return '↓';
  if (pct > 0.01) return '↑';
  return '=';
}
