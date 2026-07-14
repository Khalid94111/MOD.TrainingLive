import { inject, Injectable } from '@angular/core';
import { LocalizationService } from '@abp/ng.core';
import {
  CATEGORY_OPTIONS,
  NATURE_OPTIONS,
  PROPOSAL_STATUS_OPTIONS,
  RESULT_TYPE_OPTIONS,
} from '../models/training-enums';

/**
 * Helper service to resolve enum values to localized display text.
 * Uses ABP LocalizationService under the hood.
 *
 * Usage:
 *   private readonly l = inject(TrainingLocalizationHelper);
 *   this.l.resultType(ResultType.PassFail) → "ناجح / راسب" or "Pass / Fail"
 */
@Injectable({ providedIn: 'root' })
export class TrainingLocalizationHelper {
  private readonly localization = inject(LocalizationService);

  /** Shorthand to resolve any Training:: key */
  t(key: string): string {
    return this.localization.instant(key);
  }

  resultType(value: number): string {
    const option = RESULT_TYPE_OPTIONS.find(o => o.value === value);
    return option ? this.localization.instant(option.key) : '';
  }

  proposalStatus(value: number): { label: string; cssClass: string } {
    const option = PROPOSAL_STATUS_OPTIONS.find(o => o.value === value);
    if (!option) return { label: '', cssClass: '' };
    return { label: this.localization.instant(option.key), cssClass: option.cssClass };
  }

  category(value: string): string {
    const option = CATEGORY_OPTIONS.find(o => o.value === value);
    return option ? this.localization.instant(option.key) : value;
  }

  nature(value: string): string {
    const option = NATURE_OPTIONS.find(o => o.value === value);
    return option ? this.localization.instant(option.key) : value;
  }

  /** Build localized dataSource for DevExtreme dropdowns */
  resultTypeDataSource() {
    return RESULT_TYPE_OPTIONS.map(o => ({
      value: o.value,
      text: this.localization.instant(o.key),
    }));
  }

  categoryDataSource() {
    return CATEGORY_OPTIONS.map(o => ({
      value: o.value,
      text: this.localization.instant(o.key),
    }));
  }

  natureDataSource() {
    return NATURE_OPTIONS.map(o => ({
      value: o.value,
      text: this.localization.instant(o.key),
    }));
  }

  proposalStatusDataSource() {
    return PROPOSAL_STATUS_OPTIONS.map(o => ({
      value: o.value,
      text: this.localization.instant(o.key),
    }));
  }
}
