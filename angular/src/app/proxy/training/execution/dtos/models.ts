import type { FullAuditedEntityDto } from '@abp/ng.core';
import type { TravelInstructionStatus } from '../../enums/travel-instruction-status.enum';

export interface CreateUpdateTravelInstructionDto {
  casualCourseId?: string | null;
  sessionId?: string | null;
  departureDate: string;
  arrivalDate: string;
  returnDate: string;
  arrivalBackDate: string;
  visaRequired?: boolean;
  visaNotes?: string | null;
  insuranceArranged?: boolean;
  insuranceProvider?: string | null;
  ticketsBooked?: boolean;
  ticketReference?: string | null;
  overrideTravelDays?: number | null;
}

export interface TravelInstructionDto extends FullAuditedEntityDto<string> {
  casualCourseId?: string | null;
  sessionId?: string | null;
  departureDate?: string;
  arrivalDate?: string;
  returnDate?: string;
  arrivalBackDate?: string;
  visaRequired?: boolean;
  visaNotes?: string | null;
  insuranceArranged?: boolean;
  insuranceProvider?: string | null;
  ticketsBooked?: boolean;
  ticketReference?: string | null;
  calculatedTravelDays?: number;
  overrideTravelDays?: number | null;
  effectiveTravelDays?: number;
  status?: TravelInstructionStatus;
}
