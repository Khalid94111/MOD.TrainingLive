import { mapEnumToOptions } from '@abp/ng.core';

export enum PlanNoteEntityType {
  Plan = 0,
  PlanItem = 1,
  Nomination = 2,
  CasualCourse = 3,
  CasualCourseNomination = 4,
}

export const planNoteEntityTypeOptions = mapEnumToOptions(PlanNoteEntityType);
