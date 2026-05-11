import type { FundingScenario } from '../../enums/funding-scenario.enum';
import type { CourseType } from '../../enums/course-type.enum';
import type { PlanNoteDto } from '../../plans/dtos/models';
import type { EntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { CasualCourseStatus } from '../../enums/casual-course-status.enum';
import type { FinancialAmountSource } from '../../enums/financial-amount-source.enum';

export interface AssignScenarioDto {
  fundingScenario: FundingScenario;
  adjustments?: StaffAdjustmentDto[] | null;
  commit?: boolean;
}

export interface CalculatePreviewDto {
  items?: PreviewItemDto[];
  totalOMR?: number;
  courseType?: CourseType;
  computedAt?: string;
}

export interface CalculatePreviewInput {
  courseType?: CourseType;
  durationDays?: number;
  nomineeEmployeeIds?: string[];
  courseCost?: number | null;
}

export interface CasualCourseDetailDto extends CasualCourseDto {
  nominations?: CasualCourseNominationDto[];
  financialItems?: CasualCourseFinancialItemDto[];
  latestReturnNote?: PlanNoteDto | null;
  conditionSummary?: string | null;
}

export interface CasualCourseDto extends FullAuditedEntityDto<string> {
  tenantId?: string | null;
  tenantCourseId?: string;
  unitId?: string;
  requestedById?: string;
  courseType?: CourseType;
  priority?: number;
  justification?: string;
  descriptionAr?: string | null;
  objectivesAr?: string | null;
  durationYears?: number;
  durationMonths?: number;
  durationDays?: number;
  estimatedDateFrom?: string;
  estimatedDateTo?: string;
  fundingSourceName?: string | null;
  fundingSourceVoteCode?: string | null;
  courseCost?: number | null;
  estimatedTotalCost?: number | null;
  fundingScenario?: FundingScenario | null;
  selectedPriceQuoteId?: string | null;
  actualStartDate?: string | null;
  actualEndDate?: string | null;
  status?: CasualCourseStatus;
  returnedFromStatus?: CasualCourseStatus | null;
  isReturned?: boolean;
  lastReturnNoteId?: string | null;
  rejectedReason?: string | null;
  courseNameAr?: string;
  unitName?: string;
  requesterName?: string;
  nomineesCount?: number;
  financialItemsCount?: number;
  latestReturnReason?: string | null;
}

export interface CasualCourseFinancialItemDto extends FullAuditedEntityDto<string> {
  tenantId?: string | null;
  casualCourseId?: string;
  financialItemId?: string;
  financialItemName?: string;
  isPerDay?: boolean;
  isPerNominee?: boolean;
  extraDaysBefore?: number;
  extraDaysAfter?: number;
  effectiveDays?: number;
  estimatedAmountOMR?: number;
  actualAmountOMR?: number | null;
  source?: FinancialAmountSource;
  notes?: string | null;
  ranks?: CasualCourseFinancialItemRankDto[];
}

export interface CasualCourseFinancialItemRankDto extends EntityDto<string> {
  casualCourseFinancialItemId?: string;
  rankId?: string;
  rankNameAr?: string;
  nomineeCount?: number;
  ratePerUnitOMR?: number;
  subtotalOMR?: number;
  rateSource?: string;
}

export interface CasualCourseGetListInput extends PagedAndSortedResultRequestDto {
  year?: number | null;
  status?: CasualCourseStatus[] | null;
  unitId?: string | null;
  onlyMyRequests?: boolean;
  isReturnedOnly?: boolean;
  search?: string | null;
}

export interface CasualCourseNominationDto extends FullAuditedEntityDto<string> {
  tenantId?: string | null;
  casualCourseId?: string;
  employeeId?: string;
  employeeName?: string;
  rankName?: string;
  unitName?: string;
  isReturned?: boolean;
  lastReturnNote?: string | null;
  conditionPassed?: boolean;
  conditionDetails?: string | null;
}

export interface CreateCasualCourseFinancialItemDto {
  financialItemId: string;
  notes?: string | null;
}

export interface CreateUpdateCasualCourseDto {
  tenantCourseId: string;
  unitId: string;
  courseType: CourseType;
  priority?: number;
  justification: string;
  descriptionAr?: string | null;
  objectivesAr?: string | null;
  durationYears?: number;
  durationMonths?: number;
  durationDays?: number;
  estimatedDateFrom: string;
  estimatedDateTo: string;
  fundingSourceName?: string | null;
  fundingSourceVoteCode?: string | null;
  courseCost?: number | null;
  nomineeEmployeeIds?: string[];
}

export interface PreviewItemDto {
  financialItemId?: string;
  financialItemNameAr?: string;
  isPerDay?: boolean;
  isPerNominee?: boolean;
  effectiveDays?: number;
  subtotalOMR?: number;
  rankBreakdown?: PreviewRankRowDto[] | null;
}

export interface PreviewRankRowDto {
  rankId?: string;
  rankNameAr?: string;
  nomineeCount?: number;
  ratePerUnitOMR?: number;
  subtotalOMR?: number;
}

export interface RejectDto {
  reason: string;
}

export interface SelectPriceQuoteDto {
  priceQuoteId: string;
  actualStartDate: string;
  actualEndDate: string;
}

export interface StaffAdjustmentDto {
  casualCourseFinancialItemRankId?: string;
  newRatePerUnitOMR?: number;
  adjustmentNote?: string | null;
}

export interface UpdateRankRateDto {
  ratePerUnitOMR?: number;
}
