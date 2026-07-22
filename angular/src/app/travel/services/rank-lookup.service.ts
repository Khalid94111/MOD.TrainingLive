import { inject, Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface RankLookupDto {
  id: string;
  name: string;
  category: number;
  level: number;
}

@Injectable({ providedIn: 'root' })
export class RankLookupService {
  private restService = inject(RestService);

  getRanks(): Observable<RankLookupDto[]> {
    return this.restService.request<void, RankLookupDto[]>(
      { method: 'GET', url: '/api/travel/ranks' },
      { apiName: 'Travel' }
    );
  }

  getRank(id: string): Observable<RankLookupDto> {
    return this.restService.request<void, RankLookupDto>(
      { method: 'GET', url: `/api/travel/ranks/${id}` },
      { apiName: 'Travel' }
    );
  }
}
