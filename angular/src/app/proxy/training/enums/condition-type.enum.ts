import { mapEnumToOptions } from '@abp/ng.core';

export enum ConditionType {
  Rank = 0,
  Age = 1,
  ServiceYears = 2,
  Education = 3,
  MedicalFitness = 4,
  SecurityClearance = 5,
  LanguageLevel = 6,
  PreviousCourse = 7,
  Custom = 8,
}

export const conditionTypeOptions = mapEnumToOptions(ConditionType);
