import type { CreationAuditedEntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { ApprovalStatus } from '../../enums/approval-status.enum';
import type { NominationStatus } from '../../enums/nomination-status.enum';
import type { AttendanceStatus } from '../../enums/attendance-status.enum';
import type { ResultType } from '../../enums/result-type.enum';

export interface ApproveRejectNominationDto {
  notes?: string | null;
}

export interface CreateNominationDto {
  sessionId: string;
  employeeIds: string[];
}

export interface NominationApprovalDto extends CreationAuditedEntityDto<string> {
  nominationId?: string;
  approvalLevel?: number;
  approvalLevelName?: string;
  approvedById?: string | null;
  approvedByName?: string | null;
  status?: ApprovalStatus;
  actionDate?: string | null;
  notes?: string | null;
}

export interface NominationDto extends FullAuditedEntityDto<string> {
  sessionId?: string | null;
  sessionCode?: string;
  courseName?: string;
  planItemId?: string;
  employeeId?: string;
  employeeName?: string;
  rankNameAr?: string;
  nominatedById?: string;
  nominatedByName?: string;
  status?: NominationStatus;
  nominatedAt?: string;
  approvedAt?: string | null;
  attendanceStatus?: AttendanceStatus | null;
  resultType?: ResultType | null;
  resultValue?: string | null;
  isReturned?: boolean;
  lastReturnNoteId?: string | null;
}

export interface NominationGetListInput extends PagedAndSortedResultRequestDto {
  planItemId?: string | null;
  sessionId?: string | null;
  status?: NominationStatus | null;
  employeeId?: string | null;
}
