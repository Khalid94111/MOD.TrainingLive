import { inject, Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface AllowanceRateDto {
  id: string;
  rankId: string;
  rankName: string;
  category: number;
  allowanceType: number;
  amount: number;
  annualPartialAmount?: number;
  ticketClass: number;
  isActive: boolean;
}

export interface CreateUpdateAllowanceRateDto {
  rankId: string;
  category: number;
  allowanceType: number;
  amount: number;
  annualPartialAmount?: number;
  ticketClass: number;
  isActive: boolean;
}

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class AllowanceRateService {
  private restService = inject(RestService);

  getList(params?: { rankId?: string; category?: number; allowanceType?: number; skipCount?: number; maxResultCount?: number }): Observable<PagedResultDto<AllowanceRateDto>> {
    return this.restService.request<any, PagedResultDto<AllowanceRateDto>>(
      { method: 'GET', url: '/api/travel/allowance-rates', params },
      { apiName: 'Travel' }
    );
  }

  get(id: string): Observable<AllowanceRateDto> {
    return this.restService.request<void, AllowanceRateDto>(
      { method: 'GET', url: `/api/travel/allowance-rates/${id}` },
      { apiName: 'Travel' }
    );
  }

  create(body: CreateUpdateAllowanceRateDto): Observable<AllowanceRateDto> {
    return this.restService.request<CreateUpdateAllowanceRateDto, AllowanceRateDto>(
      { method: 'POST', url: '/api/travel/allowance-rates', body },
      { apiName: 'Travel' }
    );
  }

  update(id: string, body: CreateUpdateAllowanceRateDto): Observable<AllowanceRateDto> {
    return this.restService.request<CreateUpdateAllowanceRateDto, AllowanceRateDto>(
      { method: 'PUT', url: `/api/travel/allowance-rates/${id}`, body },
      { apiName: 'Travel' }
    );
  }

  delete(id: string): Observable<void> {
    return this.restService.request<void, void>(
      { method: 'DELETE', url: `/api/travel/allowance-rates/${id}` },
      { apiName: 'Travel' }
    );
  }
}
