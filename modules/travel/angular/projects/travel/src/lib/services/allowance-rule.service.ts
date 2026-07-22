import { inject, Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface AllowanceRuleDto {
  id: string;
  name: string;
  allowanceType: number;
  travelTypeDefinitionId?: string;
  category?: number;
  appliesWhenAccommodationIncluded?: boolean;
  accommodationMultiplier?: number;
  priority: number;
  isActive: boolean;
  segments: AllowanceRuleSegmentDto[];
}

export interface AllowanceRuleSegmentDto {
  id: string;
  allowanceRuleId: string;
  fromDay: number;
  toDay: number;
  percentage: number;
  appliesWhenTotalDaysFrom?: number;
  appliesWhenTotalDaysTo?: number;
}

export interface CreateUpdateAllowanceRuleDto {
  name: string;
  allowanceType: number;
  travelTypeDefinitionId?: string | null;
  category?: number;
  appliesWhenAccommodationIncluded?: boolean;
  accommodationMultiplier?: number;
  priority: number;
  isActive: boolean;
  segments: { fromDay: number; toDay: number; percentage: number; appliesWhenTotalDaysFrom?: number; appliesWhenTotalDaysTo?: number }[];
}

export interface AllowanceRuleTestInputDto {
  days: number;
  dailyRate: number;
  employeeCount: number;
  accommodationPaymentPercentage: number;
}

export interface AllowanceRuleTestResultDto {
  days: number;
  dailyRate: number;
  employeeCount: number;
  accommodationPaymentPercentage: number;
  totalPerEmployee: number;
  grandTotal: number;
  segments: Array<{
    fromDay: number;
    toDay: number;
    days: number;
    percentage: number;
    amount: number;
  }>;
}

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class AllowanceRuleService {
  private restService = inject(RestService);

  getList(params?: { allowanceType?: number; travelTypeDefinitionId?: string; filter?: string; skipCount?: number; maxResultCount?: number }): Observable<PagedResultDto<AllowanceRuleDto>> {
    return this.restService.request<any, PagedResultDto<AllowanceRuleDto>>(
      { method: 'GET', url: '/api/travel/allowance-rules', params },
      { apiName: 'Travel' }
    );
  }

  get(id: string): Observable<AllowanceRuleDto> {
    return this.restService.request<void, AllowanceRuleDto>(
      { method: 'GET', url: `/api/travel/allowance-rules/${id}` },
      { apiName: 'Travel' }
    );
  }

  test(id: string, body: AllowanceRuleTestInputDto): Observable<AllowanceRuleTestResultDto> {
    return this.restService.request<AllowanceRuleTestInputDto, AllowanceRuleTestResultDto>(
      { method: 'POST', url: `/api/travel/allowance-rules/${id}/test`, body },
      { apiName: 'Travel' }
    );
  }

  create(body: CreateUpdateAllowanceRuleDto): Observable<AllowanceRuleDto> {
    return this.restService.request<CreateUpdateAllowanceRuleDto, AllowanceRuleDto>(
      { method: 'POST', url: '/api/travel/allowance-rules', body },
      { apiName: 'Travel' }
    );
  }

  update(id: string, body: CreateUpdateAllowanceRuleDto): Observable<AllowanceRuleDto> {
    return this.restService.request<CreateUpdateAllowanceRuleDto, AllowanceRuleDto>(
      { method: 'PUT', url: `/api/travel/allowance-rules/${id}`, body },
      { apiName: 'Travel' }
    );
  }

  delete(id: string): Observable<void> {
    return this.restService.request<void, void>(
      { method: 'DELETE', url: `/api/travel/allowance-rules/${id}` },
      { apiName: 'Travel' }
    );
  }
}
