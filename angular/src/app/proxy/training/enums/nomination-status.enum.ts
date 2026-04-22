import { mapEnumToOptions } from '@abp/ng.core';

export enum NominationStatus {
  Nominated = 0,
  UTMApproved = 1,
  UGMApproved = 2,
  TDApproved = 3,
  Rejected = 4,
  Returned = 5,
}

export const nominationStatusOptions = mapEnumToOptions(NominationStatus);
