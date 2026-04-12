import { mapEnumToOptions } from '@abp/ng.core';

export enum CenterRoleType {
  TCO = 0,
  TCM = 1,
}

export const centerRoleTypeOptions = mapEnumToOptions(CenterRoleType);
