import { mapEnumToOptions } from '@abp/ng.core';

export enum CompletionStatus {
  NotStarted = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3,
}

export const completionStatusOptions = mapEnumToOptions(CompletionStatus);
