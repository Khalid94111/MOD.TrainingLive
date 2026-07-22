import type { TravelAllowancePaymentDto, TravelAllowancePaymentGetListInput } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TravelAllowancePaymentService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TravelAllowancePaymentDto>({
      method: 'GET',
      url: `/api/app/travel-allowance-payment/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: TravelAllowancePaymentGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TravelAllowancePaymentDto>>({
      method: 'GET',
      url: '/api/app/travel-allowance-payment',
      params: { casualCourseId: input.casualCourseId, sessionId: input.sessionId, nominationId: input.nominationId, status: input.status, personnelType: input.personnelType, search: input.search, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
}