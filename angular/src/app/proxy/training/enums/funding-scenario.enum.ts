import { mapEnumToOptions } from '@abp/ng.core';

export enum FundingScenario {
  FundingSourceCoversAll = 1,
  FundingSourceCoversCourse = 2,
  FinancialItemsCoverAll = 3,
}

export const fundingScenarioOptions = mapEnumToOptions(FundingScenario);
