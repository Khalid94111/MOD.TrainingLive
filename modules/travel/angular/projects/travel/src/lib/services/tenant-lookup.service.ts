import { inject, Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface TenantLookupDto {
  id: string;
  name: string;
  arabicDescription: string;
  englishDescription: string;
}

@Injectable({ providedIn: 'root' })
export class TenantLookupService {
  private restService = inject(RestService);

  getTenants(): Observable<TenantLookupDto[]> {
    return this.restService.request<void, TenantLookupDto[]>(
      { method: 'GET', url: '/api/travel/tenants' },
      { apiName: 'Travel' }
    );
  }
}
