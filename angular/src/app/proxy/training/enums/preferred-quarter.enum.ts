import { mapEnumToOptions } from '@abp/ng.core';

export enum PreferredQuarter {
  Q1 = 1,
  Q2 = 2,
  Q3 = 3,
  Q4 = 4,
}

export const preferredQuarterOptions = mapEnumToOptions(PreferredQuarter);
