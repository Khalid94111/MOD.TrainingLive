import type { FundingScenario } from '../../enums/funding-scenario.enum';
import type { PlanNoteDto } from '../../plans/dtos/models';
import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { CourseType } from '../../enums/course-type.enum';
import type { CasualCourseStatus } from '../../enums/casual-course-status.enum';
import type { FinancialAmountSource } from '../../enums/financial-amount-source.enum';

export interface AssignScenarioDto {
  fundingScenario: FundingScenario;
  estimatedTotalCost?: number;
  financialItems?: AssignmentLineDto[];
  commit?: boolean;
}

export interface AssignmentLineDto {
  id?: string | null;
  financialItemId: string;
  amount?: number;
  notes?: string | null;
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
  estimatedAmountOMR?: number;
  actualAmountOMR?: number | null;
  source?: FinancialAmountSource;
  notes?: string | null;
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
  estimatedAmountOMR?: number;
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
}

export interface EstimatePreviewDto {
  items?: EstimatePreviewItemDto[];
  total?: number;
  courseType?: CourseType;
  computedAt?: string;
}

export interface EstimatePreviewInput {
  tenantCourseId: string;
  courseType: CourseType;
  durationDays?: number;
  nomineeCount?: number;
}

export interface EstimatePreviewItemDto {
  financialItemId?: string;
  financialItemName?: string;
  isPerDay?: boolean;
  isPerNominee?: boolean;
  rate?: number;
  effectiveDays?: number;
  effectiveCount?: number;
  computedAmount?: number;
}

export interface RejectDto {
  reason: string;
}

export interface UpdateAmountDto {
  amount?: number;
  notes?: string | null;
}
