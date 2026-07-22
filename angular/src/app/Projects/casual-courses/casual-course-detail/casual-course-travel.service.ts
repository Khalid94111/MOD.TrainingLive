import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';
import type { SessionTravelDto } from '../../sessions/session-detail/session-travel.service';

export interface CasualCourseTravelDto extends Omit<SessionTravelDto, 'sessionId'> {
  casualCourseId: string;
}

@Injectable({ providedIn: 'root' })
export class CasualCourseTravelService {
  private readonly rest = inject(RestService);

  get(casualCourseId: string): Observable<CasualCourseTravelDto> {
    return this.rest.request<void, CasualCourseTravelDto>(
      { method: 'GET', url: `/api/training/casual-courses/${casualCourseId}/travel` },
      { apiName: 'Default' },
    );
  }

  refresh(casualCourseId: string): Observable<CasualCourseTravelDto> {
    return this.rest.request<void, CasualCourseTravelDto>(
      { method: 'POST', url: `/api/training/casual-courses/${casualCourseId}/travel/refresh` },
      { apiName: 'Default' },
    );
  }

  send(casualCourseId: string): Observable<CasualCourseTravelDto> {
    return this.rest.request<void, CasualCourseTravelDto>(
      { method: 'POST', url: `/api/training/casual-courses/${casualCourseId}/travel/send` },
      { apiName: 'Default' },
    );
  }
}
