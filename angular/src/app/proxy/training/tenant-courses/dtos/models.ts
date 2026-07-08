import type { EntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { ConditionType } from '../../enums/condition-type.enum';
import type { ResultType } from '../../enums/result-type.enum';

export interface AddFromCatalogDto {
  catalogCourseIds?: string[];
}

export interface TenantCourseConditionDto extends EntityDto<string> {
  tenantCourseId?: string;
  conditionType?: ConditionType;
  conditionValue?: string;
  isActive?: boolean;
}

export interface TenantCourseDto extends FullAuditedEntityDto<string> {
  catalogCourseId?: string;
  catalogCourseNameAr?: string;
  catalogCourseNameEn?: string;
  catalogCourseFieldNameAr?: string | null;
  catalogCourseCategory?: string | null;
  defaultCapacity?: number | null;
  defaultDurationWeeks?: number | null;
  resultType?: ResultType;
  requiresEvaluation?: boolean;
  requiresProviderEvaluation?: boolean;
  hasCertificate?: boolean;
  evaluationBlocksCertificate?: boolean;
  isActive?: boolean;
  conditionsCount?: number;
  addedByName?: string | null;
  addedAt?: string;
  addedAtFormatted?: string | null;
}

export interface TenantCourseGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
  fieldId?: string | null;
  resultType?: ResultType | null;
  isActive?: boolean | null;
}

export interface UpdateTenantCourseDto {
  defaultCapacity?: number | null;
  defaultDurationWeeks?: number | null;
  resultType?: ResultType;
  requiresEvaluation?: boolean;
  requiresProviderEvaluation?: boolean;
  hasCertificate?: boolean;
  evaluationBlocksCertificate?: boolean;
  isActive?: boolean;
}
