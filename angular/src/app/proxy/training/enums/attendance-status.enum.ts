import { mapEnumToOptions } from '@abp/ng.core';

export enum AttendanceStatus {
  Attended = 0,
  NoShow = 1,
  Withdrew = 2,
}

export const attendanceStatusOptions = mapEnumToOptions(AttendanceStatus);
