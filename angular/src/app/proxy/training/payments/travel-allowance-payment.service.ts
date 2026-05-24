import type { CreateUpdateTravelAllowancePaymentDto, TravelAllowancePaymentDefaultsDto, TravelAllowancePaymentDto, TravelAllowancePaymentGetListInput } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TravelAllowancePaymentService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  confirm = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TravelAllowancePaymentDto>({
      method: 'POST',
      url: `/api/app/travel-allowance-payment/${id}/confirm`,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateUpdateTravelAllowancePaymentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TravelAllowancePaymentDto>({
      method: 'POST',
      url: '/api/app/travel-allowance-payment',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/travel-allowance-payment/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TravelAllowancePaymentDto>({
      method: 'GET',
      url: `/api/app/travel-allowance-payment/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getDefaults = (casualCourseId: string, nominationId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TravelAllowancePaymentDefaultsDto>({
      method: 'GET',
      url: '/api/app/travel-allowance-payment/defaults',
      params: { casualCourseId, nominationId },
    },
    { apiName: this.apiName,...config });
  

  getList = (input: TravelAllowancePaymentGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TravelAllowancePaymentDto>>({
      method: 'GET',
      url: '/api/app/travel-allowance-payment',
      params: { casualCourseId: input.casualCourseId, sessionId: input.sessionId, nominationId: input.nominationId, status: input.status, personnelType: input.personnelType, search: input.search, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateTravelAllowancePaymentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TravelAllowancePaymentDto>({
      method: 'PUT',
      url: `/api/app/travel-allowance-payment/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}