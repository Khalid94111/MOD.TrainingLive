import { mapEnumToOptions } from '@abp/ng.core';

export enum PlanStatus {
  Draft = 0,
  Open = 1,
  Submitted = 2,
  UnderReview = 3,
  TDApproved = 4,
  THApproved = 5,
  Closed = 6,
}

export const planStatusOptions = mapEnumToOptions(PlanStatus);
