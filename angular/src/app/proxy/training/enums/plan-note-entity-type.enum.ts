import { mapEnumToOptions } from '@abp/ng.core';

export enum PlanNoteEntityType {
  Plan = 0,
  PlanItem = 1,
  Nomination = 2,
}

export const planNoteEntityTypeOptions = mapEnumToOptions(PlanNoteEntityType);
