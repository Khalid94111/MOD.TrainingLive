import type { CreationAuditedEntityDto, EntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { SessionStatus } from '../../enums/session-status.enum';
import type { CompletionStatus } from '../../enums/completion-status.enum';
import type { PlanNoteEntityType } from '../../enums/plan-note-entity-type.enum';
import type { CourseType } from '../../enums/course-type.enum';
import type { PreferredQuarter } from '../../enums/preferred-quarter.enum';
import type { ConditionType } from '../../enums/condition-type.enum';
import type { PlanNoteAuthorRole } from '../../enums/plan-note-author-role.enum';
import type { PlanStatus } from '../../enums/plan-status.enum';

export interface CourseSessionDto extends FullAuditedEntityDto<string> {
  courseId?: string;
  courseName?: string;
  sessionCode?: string;
  startDate?: string;
  endDate?: string;
  location?: string | null;
  country?: string | null;
  maxSeats?: number;
  availableSeats?: number;
  cost?: number | null;
  status?: SessionStatus;
  completionStatus?: CompletionStatus;
  completedAt?: string | null;
}

export interface CourseSessionGetListInput extends PagedAndSortedResultRequestDto {
  courseId?: string | null;
  status?: SessionStatus | null;
}

export interface CreatePlanNoteDto {
  entityType?: PlanNoteEntityType;
  entityId?: string;
  note?: string;
  isReturnReason?: boolean;
}

export interface CreateUpdateCourseSessionDto {
  courseId: string;
  sessionCode: string;
  startDate: string;
  endDate: string;
  location?: string | null;
  country?: string | null;
  maxSeats: number;
  cost?: number | null;
}

export interface CreateUpdatePlanItemFinancialItemDto {
  planItemId: string;
  financialItemId: string;
  estimatedAmountOMR: number;
  estimatedAmountUSD?: number | null;
  notes?: string | null;
}

export interface CreateUpdateTrainingPlanDto {
  year: number;
  openDate?: string | null;
  closeDate?: string | null;
}

export interface CreateUpdateTrainingPlanItemDto {
  planId: string;
  tenantCourseId: string;
  courseType: CourseType;
  preferredQuarter: PreferredQuarter;
  priority: number;
  justification: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  objectivesAr?: string | null;
  objectivesEn?: string | null;
  durationYears?: number;
  durationMonths?: number;
  durationDays?: number;
  estimatedDateFrom?: string | null;
  estimatedDateTo?: string | null;
  fundingSource?: string | null;
  unitId?: string | null;
  nomineeEmployeeIds: string[];
}

export interface PlanItemConditionDto extends EntityDto<string> {
  planItemId?: string;
  conditionType?: ConditionType;
  conditionValue?: string;
}

export interface PlanItemFinancialItemDto extends FullAuditedEntityDto<string> {
  planItemId?: string;
  financialItemId?: string;
  financialItemName?: string;
  estimatedAmountOMR?: number;
  estimatedAmountUSD?: number | null;
  actualAmountOMR?: number | null;
  actualAmountUSD?: number | null;
  notes?: string | null;
}

export interface PlanItemFinancialItemRankDto extends FullAuditedEntityDto<string> {
  planItemFinancialItemId?: string;
  rankId?: string;
  rankNameAr?: string;
  rankNameEn?: string;
  nomineeCount?: number;
  ratePerUnitOMR?: number;
  subtotalOMR?: number;
}

export interface PlanNoteDto extends CreationAuditedEntityDto<string> {
  entityType?: PlanNoteEntityType;
  entityId?: string;
  note?: string;
  authorRole?: PlanNoteAuthorRole;
  isReturnReason?: boolean;
  createdByName?: string;
}

export interface PlanNoteGetListInput extends PagedAndSortedResultRequestDto {
  entityType?: PlanNoteEntityType;
  entityId?: string;
}

export interface ReturnReasonDto {
  reason: string;
}

export interface TrainingPlanDto extends FullAuditedEntityDto<string> {
  year?: number;
  status?: PlanStatus;
  openDate?: string | null;
  closeDate?: string | null;
  itemCount?: number;
  totalEstimatedCost?: number;
}

export interface TrainingPlanGetListInput extends PagedAndSortedResultRequestDto {
  year?: number | null;
}

export interface TrainingPlanItemDto extends FullAuditedEntityDto<string> {
  planId?: string;
  tenantCourseId?: string;
  tenantCourseNameAr?: string;
  tenantCourseNameEn?: string;
  courseType?: CourseType;
  preferredQuarter?: PreferredQuarter;
  priority?: number;
  justification?: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  objectivesAr?: string | null;
  objectivesEn?: string | null;
  durationYears?: number;
  durationMonths?: number;
  durationDays?: number;
  estimatedDateFrom?: string | null;
  estimatedDateTo?: string | null;
  estimatedCost?: number;
  fundingSource?: string | null;
  submittedById?: string;
  submittedByName?: string;
  submittedByRank?: string | null;
  unitId?: string | null;
  unitName?: string | null;
  financialItemCount?: number;
  nomineesCount?: number;
  isReturned?: boolean;
}

export interface TrainingPlanItemGetListInput extends PagedAndSortedResultRequestDto {
  planId?: string;
  courseType?: CourseType | null;
  preferredQuarter?: PreferredQuarter | null;
  unitId?: string | null;
}

export interface UpdateAmountDto {
  estimatedAmountOMR?: number;
}

export interface UpdateNotesDto {
  notes?: string | null;
}

export interface UpdateRateDto {
  newRate?: number;
}
