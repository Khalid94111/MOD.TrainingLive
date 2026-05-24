import type { PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { PreferredQuarter } from '../../enums/preferred-quarter.enum';
import type { CourseType } from '../../enums/course-type.enum';

export interface AnnualPlanProgressDto {
  year?: number;
  totalPlanItems?: number;
  plannedSessionCount?: number;
  scheduledSessionCount?: number;
  inProgressSessionCount?: number;
  completedSessionCount?: number;
  cancelledSessionCount?: number;
  overdueCount?: number;
  overallProgressPercent?: number;
  progressByQuarter?: QuarterProgressDto[];
  progressByUnit?: UnitProgressDto[];
  alerts?: OverdueAlertDto[];
}

export interface AvailableSubstituteDto {
  employeeId?: string;
  rankId?: string;
  serviceNumber?: string;
  fullNameAr?: string;
  fullNameEn?: string;
  rankNameAr?: string | null;
}

export interface CreateExternalSessionDto {
  trainingPlanItemId: string;
  substitutions?: NomineeSubstitutionDto[];
  notes?: string | null;
}

export interface CreateInternalSessionDto {
  trainingPlanItemId: string;
  actualStartDate: string;
  actualEndDate: string;
  substitutions?: NomineeSubstitutionDto[];
  notes?: string | null;
}

export interface NomineeSubstitutionDto {
  originalEmployeeId?: string;
  replacementEmployeeId?: string;
  reason?: string | null;
}

export interface OverdueAlertDto {
  type?: string;
  entityId?: string;
  entityType?: string;
  message?: string;
  daysOverdue?: number;
}

export interface PlanItemQueueGetListInput extends PagedAndSortedResultRequestDto {
  year?: number | null;
  quarter?: PreferredQuarter | null;
  priority?: number | null;
  courseType?: CourseType | null;
  unitId?: string | null;
}

export interface PlanItemQueueItemDto {
  id?: string;
  planId?: string;
  planYear?: number;
  tenantCourseId?: string;
  tenantCourseNameAr?: string;
  tenantCourseNameEn?: string;
  courseType?: CourseType;
  preferredQuarter?: PreferredQuarter;
  priority?: number;
  unitId?: string | null;
  nomineesCount?: number;
  isOverdue?: boolean;
  creationTime?: string;
}

export interface QuarterProgressDto {
  quarter?: number;
  total?: number;
  completed?: number;
  inProgress?: number;
  pending?: number;
}

export interface UnitProgressDto {
  unitId?: string | null;
  unitName?: string | null;
  total?: number;
  completed?: number;
}
