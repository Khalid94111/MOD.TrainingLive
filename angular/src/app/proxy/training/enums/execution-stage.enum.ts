import { mapEnumToOptions } from '@abp/ng.core';

export enum ExecutionStage {
  AwaitingQuoteSelection = 1,
  AwaitingTravelCompletion = 2,
  AwaitingTravelAllowances = 3,
  AwaitingCoursePayment = 4,
  AwaitingReallocationApproval = 5,
  FinanciallyComplete = 6,
}

export const executionStageOptions = mapEnumToOptions(ExecutionStage);
