import { mapEnumToOptions } from '@abp/ng.core';

export enum ResultType {
  AttendanceOnly = 0,
  PassFail = 1,
  CompletedNotCompleted = 2,
  GradeScore = 3,
}

export const resultTypeOptions = mapEnumToOptions(ResultType);
