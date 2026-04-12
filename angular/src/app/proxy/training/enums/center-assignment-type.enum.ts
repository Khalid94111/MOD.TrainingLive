import { mapEnumToOptions } from '@abp/ng.core';

export enum CenterAssignmentType {
  Employee = 0,
  Position = 1,
}

export const centerAssignmentTypeOptions = mapEnumToOptions(CenterAssignmentType);
