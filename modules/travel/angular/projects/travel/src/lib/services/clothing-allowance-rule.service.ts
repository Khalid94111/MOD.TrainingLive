import { inject, Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface ClothingAllowanceRuleDto {
  id: string;
  name: string;
  fullAmount: number;
  annualPartialAmount: number;
  fullPaymentPeriodYears: number;
  priority: number;
  isActive: boolean;
  rankIds: string[];
  rankNames: string[];
}

export interface CreateUpdateClothingAllowanceRuleDto {
  name: string;
  fullAmount: number;
  annualPartialAmount: number;
  fullPaymentPeriodYears: number;
  priority: number;
  isActive: boolean;
  rankIds: string[];
}

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class ClothingAllowanceRuleService {
  private restService = inject(RestService);

  getList(params?: { filter?: string; isActive?: boolean; skipCount?: number; maxResultCount?: number }): Observable<PagedResultDto<ClothingAllowanceRuleDto>> {
    return this.restService.request<any, PagedResultDto<ClothingAllowanceRuleDto>>(
      { method: 'GET', url: '/api/travel/clothing-allowance-rules', params },
      { apiName: 'Travel' }
    );
  }

  create(body: CreateUpdateClothingAllowanceRuleDto): Observable<ClothingAllowanceRuleDto> {
    return this.restService.request<CreateUpdateClothingAllowanceRuleDto, ClothingAllowanceRuleDto>(
      { method: 'POST', url: '/api/travel/clothing-allowance-rules', body },
      { apiName: 'Travel' }
    );
  }

  update(id: string, body: CreateUpdateClothingAllowanceRuleDto): Observable<ClothingAllowanceRuleDto> {
    return this.restService.request<CreateUpdateClothingAllowanceRuleDto, ClothingAllowanceRuleDto>(
      { method: 'PUT', url: `/api/travel/clothing-allowance-rules/${id}`, body },
      { apiName: 'Travel' }
    );
  }

  delete(id: string): Observable<void> {
    return this.restService.request<void, void>(
      { method: 'DELETE', url: `/api/travel/clothing-allowance-rules/${id}` },
      { apiName: 'Travel' }
    );
  }
}
