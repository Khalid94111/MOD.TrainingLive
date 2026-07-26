import { mapEnumToOptions } from '@abp/ng.core';

export enum TrainingExpenseRecoveryStatus {
  PendingReview = 0,
  Reviewed = 1,
  PartiallySettled = 2,
  Settled = 3,
}

export const trainingExpenseRecoveryStatusOptions = mapEnumToOptions(TrainingExpenseRecoveryStatus);
