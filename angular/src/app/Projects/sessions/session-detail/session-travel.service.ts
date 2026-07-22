import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface SessionTravelDto {
  sessionId: string;
  travelRequestId?: string;
  integrationAvailable: boolean;
  isFound: boolean;
  isCompleted: boolean;
  canSend: boolean;
  statusCode: string;
  warningMessage: string;
  blockingReasons: string[];
  completedAt?: string;
  calculatedDays: number;
  employeeCount: number;
  currency: string;
  ticketsTotal: number;
  visaTotal: number;
  healthInsuranceTotal: number;
  travelAllowanceTotal: number;
  clothingAllowanceTotal: number;
  deductionTotal: number;
  grandTotal: number;
}

@Injectable({ providedIn: 'root' })
export class SessionTravelService {
  private readonly rest = inject(RestService);

  get(sessionId: string): Observable<SessionTravelDto> {
    return this.rest.request<void, SessionTravelDto>(
      { method: 'GET', url: `/api/training/sessions/${sessionId}/travel` },
      { apiName: 'Default' },
    );
  }

  refresh(sessionId: string): Observable<SessionTravelDto> {
    return this.rest.request<void, SessionTravelDto>(
      { method: 'POST', url: `/api/training/sessions/${sessionId}/travel/refresh` },
      { apiName: 'Default' },
    );
  }

  send(sessionId: string): Observable<SessionTravelDto> {
    return this.rest.request<void, SessionTravelDto>(
      { method: 'POST', url: `/api/training/sessions/${sessionId}/travel/send` },
      { apiName: 'Default' },
    );
  }
}
