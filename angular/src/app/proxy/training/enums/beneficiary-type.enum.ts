import { mapEnumToOptions } from '@abp/ng.core';

export enum BeneficiaryType {
  Internal = 0,
  Shared = 1,
}

export const beneficiaryTypeOptions = mapEnumToOptions(BeneficiaryType);
