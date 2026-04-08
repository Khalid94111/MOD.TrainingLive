import type { FullAuditedEntityDto } from '@abp/ng.core';

export interface CourseFieldDto extends FullAuditedEntityDto<string> {
  fieldNameAr?: string;
  fieldNameEn?: string;
  isActive?: boolean;
}

export interface CreateUpdateCourseFieldDto {
  fieldNameAr?: string;
  fieldNameEn?: string;
  isActive?: boolean;
}
