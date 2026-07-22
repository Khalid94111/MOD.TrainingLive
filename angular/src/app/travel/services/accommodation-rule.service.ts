import { inject, Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface AccommodationRuleDto {
  id: string;
  name: string;
  paymentPercentage: number;
  priority: number;
  isActive: boolean;
  allowanceRuleIds: string[];
  allowanceRuleNames: string[];
}

export interface CreateUpdateAccommodationRuleDto {
  name: string;
  paymentPercentage: number;
  priority: number;
  isActive: boolean;
  allowanceRuleIds: string[];
}

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class AccommodationRuleService {
  private restService = inject(RestService);

  getList(params?: { filter?: string; skipCount?: number; maxResultCount?: number }): Observable<PagedResultDto<AccommodationRuleDto>> {
    return this.restService.request<any, PagedResultDto<AccommodationRuleDto>>(
      { method: 'GET', url: '/api/travel/accommodation-rules', params },
      { apiName: 'Travel' }
    );
  }

  create(body: CreateUpdateAccommodationRuleDto): Observable<AccommodationRuleDto> {
    return this.restService.request<CreateUpdateAccommodationRuleDto, AccommodationRuleDto>(
      { method: 'POST', url: '/api/travel/accommodation-rules', body },
      { apiName: 'Travel' }
    );
  }

  update(id: string, body: CreateUpdateAccommodationRuleDto): Observable<AccommodationRuleDto> {
    return this.restService.request<CreateUpdateAccommodationRuleDto, AccommodationRuleDto>(
      { method: 'PUT', url: `/api/travel/accommodation-rules/${id}`, body },
      { apiName: 'Travel' }
    );
  }

  delete(id: string): Observable<void> {
    return this.restService.request<void, void>(
      { method: 'DELETE', url: `/api/travel/accommodation-rules/${id}` },
      { apiName: 'Travel' }
    );
  }
}
