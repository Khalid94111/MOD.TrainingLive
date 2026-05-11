import { mapEnumToOptions } from '@abp/ng.core';

export enum ReallocationStatus {
  Pending = 0,
  Approved = 1,
}

export const reallocationStatusOptions = mapEnumToOptions(ReallocationStatus);
