import type { EntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { SessionStatus } from '../../enums/session-status.enum';
import type { CompletionStatus } from '../../enums/completion-status.enum';
import type { CourseType } from '../../enums/course-type.enum';
import type { PreferredQuarter } from '../../enums/preferred-quarter.enum';
import type { ConditionType } from '../../enums/condition-type.enum';
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
  officersCount: number;
  enlistedCount: number;
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
  officersCount?: number;
  enlistedCount?: number;
  capacity?: number;
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
}

export interface TrainingPlanItemGetListInput extends PagedAndSortedResultRequestDto {
  planId?: string;
  courseType?: CourseType | null;
  preferredQuarter?: PreferredQuarter | null;
  unitId?: string | null;
}
