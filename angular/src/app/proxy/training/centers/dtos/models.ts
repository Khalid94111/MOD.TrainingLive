import type { EntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { BeneficiaryType } from '../../enums/beneficiary-type.enum';
import type { CenterPlanStatus } from '../../enums/center-plan-status.enum';
import type { CenterRoleType } from '../../enums/center-role-type.enum';
import type { CenterAssignmentType } from '../../enums/center-assignment-type.enum';

export interface AdjustCenterPlanItemCapacityDto {
  capacity?: number;
}

export interface AvailableCenterPlanItemDto extends EntityDto<string> {
  centerPlanId?: string;
  centerId?: string;
  centerName?: string;
  sourceTenantId?: string | null;
  sourceTenantName?: string | null;
  tenantCourseId?: string;
  catalogCourseId?: string;
  courseName?: string;
  estimatedStartDate?: string;
  estimatedEndDate?: string;
  durationWeeks?: number;
  capacity?: number;
  reservedSeats?: number;
  remainingSeats?: number;
  beneficiaryType?: BeneficiaryType;
  eligibleUnitIds?: string[];
}

export interface CenterPlanBookingNomineeDto {
  nominationId?: string;
  employeeId?: string;
  serviceNumber?: string;
  employeeName?: string;
  rankName?: string | null;
  unitName?: string | null;
}

export interface CenterPlanGetListInput extends PagedAndSortedResultRequestDto {
  centerId?: string | null;
  year?: number | null;
  status?: CenterPlanStatus | null;
}

export interface CenterPlanItemGetListInput extends PagedAndSortedResultRequestDto {
  planId?: string;
}

export interface CenterPlanNominationDto extends EntityDto<string> {
  centerPlanId?: string;
  centerName?: string;
  courseName?: string;
  estimatedStartDate?: string;
  estimatedEndDate?: string;
  capacity?: number;
  reservedSeats?: number;
  remainingSeats?: number;
  bookings?: PlanItemBookingDto[];
}

export interface CenterPlanNominationGetListInput extends PagedAndSortedResultRequestDto {
  centerPlanId?: string | null;
  centerId?: string | null;
}

export interface CenterPlanWindowDto extends FullAuditedEntityDto<string> {
  year?: number;
  openDate?: string;
  closeDate?: string;
  isOpen?: boolean;
  openedById?: string;
  openedAt?: string;
}

export interface CenterPlanWindowGetListInput extends PagedAndSortedResultRequestDto {
  year?: number | null;
}

export interface CenterRoleAssignmentDto extends EntityDto<string> {
  centerId?: string;
  roleType?: CenterRoleType;
  assignmentType?: CenterAssignmentType;
  employeeId?: string | null;
  serviceNumber?: string | null;
  employeeNameAr?: string | null;
  employeeNameEn?: string | null;
  rankName?: string | null;
  positionId?: string | null;
  positionNameAr?: string | null;
  positionNameEn?: string | null;
}

export interface CenterRoleAssignmentInputDto {
  roleType: CenterRoleType;
  assignmentType: CenterAssignmentType;
  employeeId?: string | null;
  serviceNumber?: string | null;
  positionId?: string | null;
}

export interface CreateUpdateCenterPlanDto {
  centerId: string;
  year: number;
}

export interface CreateUpdateCenterPlanItemDto {
  planId: string;
  tenantCourseId: string;
  estimatedStartDate: string;
  estimatedEndDate: string;
  capacity: number;
  durationWeeks?: number;
  objective?: string | null;
  beneficiaryType: BeneficiaryType;
}

export interface CreateUpdateCenterPlanWindowDto {
  year: number;
  openDate: string;
  closeDate: string;
}

export interface CreateUpdateTrainingCenterDto {
  orgUnitId: string;
  centerNameAr: string;
  centerNameEn: string;
  location?: string | null;
  isActive?: boolean;
}

export interface PlanActionReasonDto {
  reason?: string;
}

export interface PlanItemBookingDto {
  trainingPlanItemId?: string;
  unitId?: string | null;
  unitName?: string;
  nomineeCount?: number;
  nominees?: CenterPlanBookingNomineeDto[];
}

export interface SetCenterRoleAssignmentsDto {
  assignments: CenterRoleAssignmentInputDto[];
}

export interface SetPlanItemUnitsDto {
  unitIds: string[];
}

export interface TrainingCenterDto extends FullAuditedEntityDto<string> {
  orgUnitId?: string;
  orgUnitName?: string;
  centerNameAr?: string;
  centerNameEn?: string;
  location?: string | null;
  isActive?: boolean;
  roleAssignments?: CenterRoleAssignmentDto[];
  tcoCount?: number;
  tcmName?: string | null;
}

export interface TrainingCenterPlanDto extends FullAuditedEntityDto<string> {
  centerId?: string;
  centerName?: string;
  year?: number;
  status?: CenterPlanStatus;
  openedAt?: string | null;
  submittedAt?: string | null;
  approvedAt?: string | null;
  openedById?: string | null;
  submittedById?: string | null;
  approvedById?: string | null;
  rejectionReason?: string | null;
  returnReason?: string | null;
  itemCount?: number;
  items?: TrainingCenterPlanItemDto[];
}

export interface TrainingCenterPlanItemDto extends FullAuditedEntityDto<string> {
  planId?: string;
  tenantCourseId?: string;
  tenantCourseName?: string;
  estimatedStartDate?: string;
  estimatedEndDate?: string;
  capacity?: number;
  durationWeeks?: number;
  objective?: string | null;
  beneficiaryType?: BeneficiaryType;
  batchNumber?: number;
  unitIds?: string[];
}
