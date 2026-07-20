import type { CreationAuditedEntityDto, EntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { CourseType } from '../../enums/course-type.enum';
import type { SessionStatus } from '../../enums/session-status.enum';
import type { SessionExecutionStage } from '../../enums/session-execution-stage.enum';
import type { PlanNoteEntityType } from '../../enums/plan-note-entity-type.enum';
import type { PreferredQuarter } from '../../enums/preferred-quarter.enum';
import type { PlanNoteAuthorRole } from '../../enums/plan-note-author-role.enum';
import type { PlanStatus } from '../../enums/plan-status.enum';

export interface CancelSessionDto {
  reason: string;
}

export interface CourseSessionDetailDto extends FullAuditedEntityDto<string> {
  trainingPlanItemId?: string | null;
  trainingCenterPlanItemId?: string | null;
  tenantCourseId?: string;
  tenantCourseNameAr?: string | null;
  tenantCourseNameEn?: string | null;
  courseType?: CourseType;
  preferredQuarter?: number;
  planYear?: number;
  actualStartDate?: string | null;
  actualEndDate?: string | null;
  estimatedDateFrom?: string | null;
  estimatedDateTo?: string | null;
  status?: SessionStatus;
  executionStage?: SessionExecutionStage;
  selectedPriceQuoteId?: string | null;
  selectedPriceQuoteAmountOMR?: number | null;
  selectedPriceQuoteProviderId?: string | null;
  selectedPriceQuoteProviderNameAr?: string | null;
  cancellationReason?: string | null;
  cancelledAt?: string | null;
  cancelledById?: string | null;
  nominations?: SessionNominationDto[];
  durationDays?: number;
  nomineesCount?: number;
  officersCount?: number;
  enlistedCount?: number;
  approvedCostOMR?: number;
  unitTotalOMR?: number;
  unitName?: string | null;
}

export interface CourseSessionDto extends FullAuditedEntityDto<string> {
  trainingPlanItemId?: string | null;
  trainingCenterPlanItemId?: string | null;
  tenantCourseId?: string;
  tenantCourseNameAr?: string | null;
  tenantCourseNameEn?: string | null;
  courseType?: CourseType;
  preferredQuarter?: number;
  planYear?: number;
  actualStartDate?: string | null;
  actualEndDate?: string | null;
  selectedPriceQuoteId?: string | null;
  status?: SessionStatus;
  executionStage?: SessionExecutionStage;
  nomineesCount?: number;
  cancellationReason?: string | null;
  cancelledAt?: string | null;
  cancelledById?: string | null;
}

export interface CourseSessionGetListInput extends PagedAndSortedResultRequestDto {
  trainingPlanItemId?: string | null;
  trainingCenterPlanItemId?: string | null;
  tenantCourseId?: string | null;
  courseType?: CourseType | null;
  status?: SessionStatus | null;
  executionStage?: SessionExecutionStage | null;
  planYear?: number | null;
  preferredQuarter?: number | null;
}

export interface CreatePlanNoteDto {
  entityType?: PlanNoteEntityType;
  entityId?: string;
  note?: string;
  isReturnReason?: boolean;
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
  tenantCourseId?: string;
  trainingCenterPlanItemId?: string | null;
  catalogCourseId?: string | null;
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

export interface ReopenSubmissionWindowDto {
  openDate?: string;
  closeDate?: string;
}

export interface ReturnReasonDto {
  reason: string;
}

export interface SelectSessionPriceQuoteDto {
  priceQuoteId: string;
  actualStartDate: string;
  actualEndDate: string;
}

export interface SessionNominationDto extends EntityDto<string> {
  sessionId?: string;
  employeeId?: string;
  originalEmployeeId?: string;
  wasSubstituted?: boolean;
  substitutionReason?: string | null;
  rankId?: string;
  rankNameAr?: string | null;
  employeeNameAr?: string | null;
  employeeNameEn?: string | null;
  originalEmployeeNameAr?: string | null;
  creationTime?: string;
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
  trainingCenterPlanItemId?: string | null;
  sourceCenterName?: string | null;
  sourceTenantName?: string | null;
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
