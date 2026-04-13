import { mapEnumToOptions } from '@abp/ng.core';

export enum ApprovalStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Returned = 3,
}

export const approvalStatusOptions = mapEnumToOptions(ApprovalStatus);
