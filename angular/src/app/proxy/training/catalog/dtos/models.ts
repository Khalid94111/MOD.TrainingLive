import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { ResultType } from '../../enums/result-type.enum';

export interface CourseCatalogDto extends FullAuditedEntityDto<string> {
  courseNameAr?: string;
  courseNameEn?: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  category?: string;
  nature?: string;
  fieldId?: string;
  fieldNameAr?: string | null;
  fieldNameEn?: string | null;
  resultType?: ResultType;
  requiresEvaluation?: boolean;
  requiresProviderEvaluation?: boolean;
  hasCertificate?: boolean;
  evaluationBlocksCertificate?: boolean;
  isActive?: boolean;
}

export interface CourseCatalogGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
  fieldId?: string | null;
  category?: string | null;
  isActive?: boolean | null;
}

export interface CourseCatalogSubscribedTenantDto {
  tenantId?: string;
  tenantName?: string;
}

export interface CreateUpdateCourseCatalogDto {
  courseNameAr?: string;
  courseNameEn?: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  category?: string;
  nature?: string;
  fieldId?: string;
  resultType?: ResultType;
  requiresEvaluation?: boolean;
  requiresProviderEvaluation?: boolean;
  hasCertificate?: boolean;
  evaluationBlocksCertificate?: boolean;
  isActive?: boolean;
}
