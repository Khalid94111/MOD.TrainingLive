import { mapEnumToOptions } from '@abp/ng.core';

export enum PersonnelType {
  Officer = 0,
  Enlisted = 1,
}

export const personnelTypeOptions = mapEnumToOptions(PersonnelType);
