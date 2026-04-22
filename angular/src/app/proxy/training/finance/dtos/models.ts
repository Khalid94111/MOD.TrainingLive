import type { EntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { CourseType } from '../../enums/course-type.enum';
import type { PricingType } from '../../enums/pricing-type.enum';
import type { ApprovalStatus } from '../../enums/approval-status.enum';

export interface CourseTypeFinancialItemDefaultDto extends EntityDto<string> {
  courseType?: CourseType;
  financialItemId?: string;
  financialItemNameAr?: string;
  financialItemNameEn?: string;
  sortOrder?: number;
  parentNameAr?: string;
  parentNameEn?: string;
  financialItemCode?: string;
}

export interface CreateCourseTypeFinancialItemDefaultDto {
  courseType?: CourseType;
  financialItemId?: string;
}

export interface CreateExchangeRateDto {
  rate?: number;
  notes?: string | null;
}

export interface CreateUpdateFinancialItemDto {
  parentId?: string | null;
  nameAr?: string;
  nameEn?: string;
  voteCode?: string;
  isActive?: boolean;
  defaultAmountOMR?: number;
  isPerDay?: boolean;
  isPerNominee?: boolean;
  extraDaysBefore?: number;
  extraDaysAfter?: number;
}

export interface CreateUpdateFinancialItemRankAmountDto {
  financialItemId?: string;
  rankId?: string;
  amountOMR?: number;
}

export interface CreateUpdatePriceQuoteDto {
  sessionId: string;
  providerId: string;
  pricingType: PricingType;
  quotedPrice: number;
  participantsCount: number;
  notes?: string | null;
}

export interface CreateUpdateTrainingProviderDto {
  providerNameAr: string;
  providerNameEn: string;
  contactPerson?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  website?: string | null;
  isApproved?: boolean;
  isActive?: boolean;
}

export interface ExchangeRateDto extends EntityDto<string> {
  fromCurrency?: string;
  toCurrency?: string;
  rate?: number;
  isActive?: boolean;
  setAt?: string;
}

export interface ExchangeRateGetListInput extends PagedAndSortedResultRequestDto {
}

export interface FinancialItemDto extends EntityDto<string> {
  parentId?: string | null;
  nameAr?: string;
  nameEn?: string;
  code?: string;
  voteCode?: string;
  isGeneral?: boolean;
  isActive?: boolean;
  defaultAmountOMR?: number;
  isPerDay?: boolean;
  isPerNominee?: boolean;
  extraDaysBefore?: number;
  extraDaysAfter?: number;
}

export interface FinancialItemGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
  isActive?: boolean | null;
}

export interface FinancialItemRankAmountDto extends FullAuditedEntityDto<string> {
  financialItemId?: string;
  rankId?: string;
  rankNameAr?: string;
  rankNameEn?: string;
  amountOMR?: number;
}

export interface FinancialItemSubItemDto {
  id?: string;
  nameAr?: string;
  nameEn?: string;
  code?: string;
  parentId?: string;
  parentNameAr?: string;
  parentNameEn?: string;
}

export interface PriceQuoteDto extends FullAuditedEntityDto<string> {
  sessionId?: string;
  sessionCode?: string;
  courseName?: string;
  providerId?: string;
  providerName?: string;
  pricingType?: PricingType;
  quotedPrice?: number;
  pricePerPerson?: number | null;
  totalPrice?: number | null;
  participantsCount?: number;
  status?: ApprovalStatus;
  notes?: string | null;
}

export interface PriceQuoteGetListInput extends PagedAndSortedResultRequestDto {
  sessionId?: string | null;
  providerId?: string | null;
  status?: ApprovalStatus | null;
}

export interface TrainingBudgetDto extends EntityDto<string> {
  year?: number;
  financialItemId?: string;
  financialItemNameAr?: string;
  financialItemNameEn?: string;
  totalAmount?: number;
  spentAmount?: number;
  remaining?: number;
  alertThreshold?: number;
  isOverThreshold?: boolean;
  spentPercent?: number;
  isFinancialItemActive?: boolean;
}

export interface TrainingBudgetGetListInput extends PagedAndSortedResultRequestDto {
  year?: number | null;
  financialItemId?: string | null;
}

export interface TrainingProviderDto extends FullAuditedEntityDto<string> {
  providerNameAr?: string;
  providerNameEn?: string;
  contactPerson?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  website?: string | null;
  averageRating?: number;
  totalRatings?: number;
  isApproved?: boolean;
  isActive?: boolean;
}

export interface TrainingProviderGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
  isActive?: boolean | null;
  isApproved?: boolean | null;
}

export interface UpdateAlertThresholdDto {
  alertThreshold: number;
}
