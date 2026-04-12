import type { EntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { CenterPlanStatus } from '../../enums/center-plan-status.enum';
import type { CenterRoleType } from '../../enums/center-role-type.enum';
import type { CenterAssignmentType } from '../../enums/center-assignment-type.enum';
import type { BeneficiaryType } from '../../enums/beneficiary-type.enum';

export interface CenterPlanGetListInput extends PagedAndSortedResultRequestDto {
  centerId?: string | null;
  year?: number | null;
  status?: CenterPlanStatus | null;
}

export interface CenterPlanItemGetListInput extends PagedAndSortedResultRequestDto {
  planId?: string;
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

export interface HrEmployeeLookupDto {
  employeeId?: string;
  serviceNumber?: string;
  fullNameAr?: string;
  fullNameEn?: string;
  rankName?: string | null;
  positionName?: string | null;
}

export interface HrPositionLookupDto {
  positionId?: string;
  positionNameAr?: string;
  positionNameEn?: string;
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
