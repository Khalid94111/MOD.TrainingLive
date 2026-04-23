import { mapEnumToOptions } from '@abp/ng.core';

export enum FinancialAmountSource {
  FundingSource = 0,
  FinancialItem = 1,
}

export const financialAmountSourceOptions = mapEnumToOptions(FinancialAmountSource);
