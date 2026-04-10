import { mapEnumToOptions } from '@abp/ng.core';

export enum BudgetType {
  Internal = 0,
  ExternalInternational = 1,
  Planning = 2,
  HigherEducation = 3,
}

export const budgetTypeOptions = mapEnumToOptions(BudgetType);
