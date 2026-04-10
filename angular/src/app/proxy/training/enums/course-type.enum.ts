import { mapEnumToOptions } from '@abp/ng.core';

export enum CourseType {
  Internal = 0,
  ExternalLocal = 1,
  ExternalInternational = 2,
}

export const courseTypeOptions = mapEnumToOptions(CourseType);
