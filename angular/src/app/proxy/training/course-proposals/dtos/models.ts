import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { ProposalStatus } from '../../enums/proposal-status.enum';

export interface CourseProposalDto extends FullAuditedEntityDto<string> {
  courseNameAr?: string;
  courseNameEn?: string;
  category?: string;
  nature?: string;
  fieldId?: string;
  fieldNameAr?: string | null;
  status?: ProposalStatus;
  proposedById?: string;
  proposedByName?: string | null;
  reviewedById?: string | null;
  rejectionReason?: string | null;
  reviewedAt?: string | null;
  creationTimeFormatted?: string | null;
  reviewedAtFormatted?: string | null;
}

export interface CourseProposalGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
  status?: ProposalStatus | null;
}

export interface CreateCourseProposalDto {
  courseNameAr?: string;
  courseNameEn?: string;
  category?: string;
  nature?: string;
  fieldId?: string;
}

export interface ReviewCourseProposalDto {
  decision?: ProposalStatus;
  rejectionReason?: string | null;
}
