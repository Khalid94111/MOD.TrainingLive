import { mapEnumToOptions } from '@abp/ng.core';

export enum FinancialItemType {
  Other = 0,
  CourseCost = 1,
  Ticket = 2,
  Insurance = 3,
  Visa = 4,
  Allowance = 5,
  Clothing = 6,
}

export const financialItemTypeOptions = mapEnumToOptions(FinancialItemType);
