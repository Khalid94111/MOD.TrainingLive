import type { EntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { FundingScenario } from '../../enums/funding-scenario.enum';
import type { TrainingExpenseRecoveryStatus } from '../../enums/training-expense-recovery-status.enum';
import type { PaymentStatus } from '../../enums/payment-status.enum';
import type { PersonnelType } from '../../enums/personnel-type.enum';

export interface TrainingExpenseRecoveryDto extends EntityDto<string> {
  casualCourseId?: string;
  casualCourseNameAr?: string;
  travelRequestId?: string;
  expenseDate?: string;
  currency?: string;
  totalAmountOMR?: number;
  settledAmountOMR?: number;
  remainingAmountOMR?: number;
  status?: TrainingExpenseRecoveryStatus;
  reviewedAt?: string | null;
  reviewedById?: string | null;
  reviewedByName?: string | null;
  reviewNote?: string | null;
  creationTime?: string;
  items?: TrainingExpenseRecoveryItemDto[];
}

export interface TrainingExpenseRecoveryItemDto extends EntityDto<string> {
  financialItemId?: string | null;
  financialItemNameAr?: string | null;
  expenseTypeCode?: string;
  fundingSourceVoteCode?: string;
  amountOMR?: number;
  isSettled?: boolean;
  settledAmountOMR?: number;
  remainingAmountOMR?: number;
  settledAt?: string | null;
  settledById?: string | null;
  settledByName?: string | null;
  settlementReference?: string | null;
  settlementNote?: string | null;
}

export interface TrainingExpenseRecoveryGetListInput extends PagedAndSortedResultRequestDto {
  casualCourseId?: string | null;
  status?: TrainingExpenseRecoveryStatus | null;
  search?: string | null;
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

export interface MarkTrainingExpenseRecoveryReviewedDto {
  reviewNote?: string | null;
}

export interface MarkTrainingExpenseRecoverySettledDto {
  settlementReference: string;
  settlementNote?: string | null;
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
