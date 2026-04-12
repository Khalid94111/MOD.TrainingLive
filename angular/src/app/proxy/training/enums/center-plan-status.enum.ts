import { mapEnumToOptions } from '@abp/ng.core';

export enum CenterPlanStatus {
  Draft = 0,
  Submitted = 1,
  Approved = 2,
  Rejected = 3,
}

export const centerPlanStatusOptions = mapEnumToOptions(CenterPlanStatus);
