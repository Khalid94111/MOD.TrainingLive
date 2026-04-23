import { mapEnumToOptions } from '@abp/ng.core';

export enum CasualCourseStatus {
  Draft = 0,
  Submitted = 1,
  UGMApproved = 2,
  UnderReview = 3,
  StaffReviewed = 4,
  TDApproved = 5,
  THApproved = 6,
  ReturnedToCreator = 7,
  Rejected = 8,
}

export const casualCourseStatusOptions = mapEnumToOptions(CasualCourseStatus);
