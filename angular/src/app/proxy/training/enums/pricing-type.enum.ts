import { mapEnumToOptions } from '@abp/ng.core';

export enum PricingType {
  PerPerson = 0,
  Total = 1,
}

export const pricingTypeOptions = mapEnumToOptions(PricingType);
