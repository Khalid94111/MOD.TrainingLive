import { mapEnumToOptions } from '@abp/ng.core';

export enum PlanNoteAuthorRole {
  UTM = 0,
  Staff = 1,
  TD = 2,
  TH = 3,
  UGM = 4,
}

export const planNoteAuthorRoleOptions = mapEnumToOptions(PlanNoteAuthorRole);
