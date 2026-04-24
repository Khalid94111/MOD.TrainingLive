import type { FundingScenario } from '../../enums/funding-scenario.enum';
import type { PlanNoteDto } from '../../plans/dtos/models';
import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { CourseType } from '../../enums/course-type.enum';
import type { CasualCourseStatus } from '../../enums/casual-course-status.enum';
import type { FinancialAmountSource } from '../../enums/financial-amount-source.enum';

export interface AssignScenarioDto {
  fundingScenario: FundingScenario;
  adjustments?: StaffAdjustmentDto[] | null;
  commit?: boolean;
}

export interface StaffAdjustmentDto {
  casualCourseFinancialItemRankId: string;
  newRatePerUnitOMR: number;
  adjustmentNote?: string | null;
}

export interface FinancialOverrideDto {
  financialItemId: string;
  rankId: string;
  ratePerUnitOMR: number;
  notes?: string | null;
}

export interface UpdateRankRateDto {
  ratePerUnitOMR?: number;
}

export interface CasualCourseDetailDto extends CasualCourseDto {
  nominations?: CasualCourseNominationDto[];
  financials?: CasualCourseFinancialDto[];
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
  fundingSource?: string | null;
  estimatedTotalCost?: number | null;
  fundingScenario?: FundingScenario | null;
  selectedPriceQuoteId?: string | null;
  status?: CasualCourseStatus;
  returnedFromStatus?: CasualCourseStatus | null;
  isReturned?: boolean;
  lastReturnNoteId?: string | null;
  rejectedReason?: string | null;
  courseNameAr?: string;
  unitName?: string;
  requesterName?: string;
  nomineesCount?: number;
  financialsCount?: number;
  latestReturnReason?: string | null;
}

export interface CasualCourseFinancialDto extends FullAuditedEntityDto<string> {
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

export interface CasualCourseFinancialItemRankDto {
  id?: string;
  casualCourseFinancialId?: string;
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

export interface CreateCasualCourseFinancialDto {
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
  fundingSource?: string | null;
  nomineeEmployeeIds?: string[];
  financialOverrides?: FinancialOverrideDto[] | null;
}

export interface RejectDto {
  reason: string;
}
