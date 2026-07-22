import type { EntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { FundingScenario } from '../../enums/funding-scenario.enum';
import type { ReallocationStatus } from '../../enums/reallocation-status.enum';
import type { PaymentStatus } from '../../enums/payment-status.enum';
import type { PersonnelType } from '../../enums/personnel-type.enum';

export interface BudgetReallocationDto extends EntityDto<string> {
  casualCourseId?: string;
  casualCourseNameAr?: string | null;
  fundingScenario?: FundingScenario | null;
  coursePaymentId?: string;
  fundingSourceVoteCode?: string;
  fundingSourceName?: string | null;
  toFinancialItemId?: string;
  toFinancialItemNameAr?: string | null;
  amountOMR?: number;
  status?: ReallocationStatus;
  approvedAt?: string | null;
  approvedById?: string | null;
  approvedByName?: string | null;
  approvalNote?: string | null;
  creationTime?: string;
}

export interface BudgetReallocationGetListInput extends PagedAndSortedResultRequestDto {
  casualCourseId?: string | null;
  coursePaymentId?: string | null;
  toFinancialItemId?: string | null;
  fundingSourceVoteCode?: string | null;
  status?: ReallocationStatus | null;
  createdFrom?: string | null;
  createdTo?: string | null;
}

export interface CoursePaymentConfirmResultDto {
  payment?: CoursePaymentDto;
  generatedReallocationsCount?: number;
}

export interface CoursePaymentDto extends EntityDto<string> {
  casualCourseId?: string | null;
  sessionId?: string | null;
  trainingProviderId?: string;
  trainingProviderName?: string | null;
  courseNameAr?: string | null;
  fundingScenario?: FundingScenario | null;
  invoiceAmountOMR?: number;
  invoiceDate?: string;
  invoiceBlobName?: string | null;
  invoiceOriginalFileName?: string | null;
  hasInvoice?: boolean;
  status?: PaymentStatus;
  confirmedAt?: string | null;
  confirmedById?: string | null;
  confirmedByName?: string | null;
  notes?: string | null;
  creationTime?: string;
}

export interface CoursePaymentGetListInput extends PagedAndSortedResultRequestDto {
  casualCourseId?: string | null;
  sessionId?: string | null;
  trainingProviderId?: string | null;
  status?: PaymentStatus | null;
  invoiceDateFrom?: string | null;
  invoiceDateTo?: string | null;
}

export interface CreateUpdateCoursePaymentDto {
  casualCourseId?: string | null;
  sessionId?: string | null;
  trainingProviderId?: string;
  invoiceAmountOMR?: number;
  invoiceDate?: string;
  notes?: string | null;
}

export interface MarkReallocationApprovedDto {
  approvalNote?: string | null;
}

export interface TravelAllowancePaymentDto extends EntityDto<string> {
  casualCourseId?: string | null;
  sessionId?: string | null;
  nominationId?: string;
  employeeNameAr?: string | null;
  rankNameAr?: string | null;
  personnelType?: PersonnelType;
  courseNameAr?: string | null;
  effectiveTravelDays?: number;
  ticketAmountOMR?: number;
  travelAllowanceOMR?: number;
  clothingAllowanceOMR?: number;
  insuranceOMR?: number;
  visaFeesOMR?: number;
  totalOMR?: number;
  status?: PaymentStatus;
  confirmedAt?: string | null;
  confirmedById?: string | null;
  confirmedByName?: string | null;
  externalRequestId?: string | null;
  externalStatus?: string | null;
  externalResponseAt?: string | null;
  notes?: string | null;
  creationTime?: string;
}

export interface TravelAllowancePaymentGetListInput extends PagedAndSortedResultRequestDto {
  casualCourseId?: string | null;
  sessionId?: string | null;
  nominationId?: string | null;
  status?: PaymentStatus | null;
  personnelType?: PersonnelType | null;
  search?: string | null;
}
