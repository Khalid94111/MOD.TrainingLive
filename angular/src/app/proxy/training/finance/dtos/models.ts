import type { EntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { CourseType } from '../../enums/course-type.enum';
import type { BudgetType } from '../../enums/budget-type.enum';

export interface CourseTypeFinancialItemDefaultDto extends EntityDto<string> {
  courseType?: CourseType;
  financialItemId?: string;
  financialItemNameAr?: string;
  financialItemNameEn?: string;
  sortOrder?: number;
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
}

export interface CreateUpdateTrainingBudgetDto {
  year?: number;
  budgetType?: BudgetType;
  totalAmount?: number;
  spentAmount?: number;
  alertThreshold?: number;
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
}

export interface FinancialItemGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
  isActive?: boolean | null;
}

export interface TrainingBudgetDto extends EntityDto<string> {
  year?: number;
  budgetType?: BudgetType;
  totalAmount?: number;
  spentAmount?: number;
  remaining?: number;
  alertThreshold?: number;
  isOverThreshold?: boolean;
  spentPercent?: number;
}

export interface TrainingBudgetGetListInput extends PagedAndSortedResultRequestDto {
  year?: number | null;
}
