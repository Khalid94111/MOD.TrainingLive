import type { EntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { CourseType } from '../../enums/course-type.enum';
import type { FinancialItemType } from '../../enums/financial-item-type.enum';
import type { PricingType } from '../../enums/pricing-type.enum';
import type { ProviderScope } from '../../enums/provider-scope.enum';
import type { ApprovalStatus } from '../../enums/approval-status.enum';

export interface CourseTypeFinancialItemDefaultDto extends EntityDto<string> {
  courseType?: CourseType;
  financialItemId?: string;
  financialItemNameAr?: string;
  financialItemNameEn?: string;
  sortOrder?: number;
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
  nameAr?: string;
  nameEn?: string;
  voteCode?: string;
  isActive?: boolean;
  defaultAmountOMR?: number;
  isPerDay?: boolean;
  isPerNominee?: boolean;
  extraDaysBefore?: number;
  extraDaysAfter?: number;
  itemType?: FinancialItemType | null;
}

export interface CreateUpdateFinancialItemRankAmountDto {
  financialItemId?: string;
  rankId?: string;
  amountOMR?: number;
}

export interface CreateUpdatePriceQuoteDto {
  sessionId?: string | null;
  casualCourseId?: string | null;
  providerId: string;
  pricingType?: PricingType;
  quotedPrice?: number;
  participantsCount?: number;
  quotedPriceOMR?: number;
  countryId?: string | null;
  cityId?: string | null;
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
  scope?: ProviderScope;
  countryId?: string | null;
}

export interface ExchangeRateDto extends EntityDto<string> {
  fromCurrency?: string;
  toCurrency?: string;
  rate?: number;
  notes?: string | null;
  isActive?: boolean;
  setAt?: string;
}

export interface ExchangeRateGetListInput extends PagedAndSortedResultRequestDto {
}

export interface FinancialItemDto extends EntityDto<string> {
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
  itemType?: FinancialItemType | null;
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
}

export interface PriceQuoteDto extends FullAuditedEntityDto<string> {
  sessionId?: string | null;
  casualCourseId?: string | null;
  sessionCode?: string;
  courseName?: string;
  providerId?: string;
  providerName?: string;
  providerScope?: ProviderScope;
  pricingType?: PricingType;
  quotedPrice?: number;
  pricePerPerson?: number | null;
  totalPrice?: number | null;
  participantsCount?: number;
  status?: ApprovalStatus;
  notes?: string | null;
  quotedPriceOMR?: number;
  isSelected?: boolean;
  countryId?: string | null;
  countryNameAr?: string | null;
  countryNameEn?: string | null;
  cityId?: string | null;
  cityNameAr?: string | null;
  cityNameEn?: string | null;
}

export interface PriceQuoteGetListInput extends PagedAndSortedResultRequestDto {
  sessionId?: string | null;
  casualCourseId?: string | null;
  providerId?: string | null;
  status?: ApprovalStatus | null;
  isSelected?: boolean | null;
}

export interface TrainingBudgetDto extends EntityDto<string> {
  year?: number;
  financialItemId?: string;
  financialItemNameAr?: string;
  financialItemNameEn?: string;
  totalAmount?: number;
  spentAmount?: number;
  remaining?: number;
  amountToRecoverOMR?: number;
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
  isFromNebras?: boolean;
  nebrasId?: string | null;
  scope?: ProviderScope;
  countryId?: string | null;
  countryNameAr?: string | null;
  countryNameEn?: string | null;
}

export interface TrainingProviderGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
  isActive?: boolean | null;
  isApproved?: boolean | null;
  scope?: ProviderScope | null;
  countryId?: string | null;
  isFromNebras?: boolean | null;
}

export interface UpdateAlertThresholdDto {
  alertThreshold: number;
}
