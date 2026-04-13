import { mapEnumToOptions } from '@abp/ng.core';

export enum SessionStatus {
  Scheduled = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3,
}

export const sessionStatusOptions = mapEnumToOptions(SessionStatus);
