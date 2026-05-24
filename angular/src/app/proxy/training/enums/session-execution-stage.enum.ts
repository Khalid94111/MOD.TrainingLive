import { mapEnumToOptions } from '@abp/ng.core';

export enum SessionExecutionStage {
  AwaitingQuoteSelection = 1,
  AwaitingTravelInstruction = 2,
  AwaitingTravelAllowances = 3,
  AwaitingCoursePayment = 4,
  AwaitingCompletion = 5,
  FinanciallyComplete = 6,
  NoExecutionPending = 7,
}

export const sessionExecutionStageOptions = mapEnumToOptions(SessionExecutionStage);
