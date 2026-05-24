import { mapEnumToOptions } from '@abp/ng.core';

export enum SessionStatus {
  Planned = 0,
  Scheduled = 1,
  InProgress = 2,
  Completed = 3,
  Cancelled = 4,
  FinanciallyClosed = 5,
}

export const sessionStatusOptions = mapEnumToOptions(SessionStatus);
