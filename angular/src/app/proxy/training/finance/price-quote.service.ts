import type { CreateUpdatePriceQuoteDto, PriceQuoteDto, PriceQuoteGetListInput } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PriceQuoteService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  approve = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/price-quote/${id}/approve`,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateUpdatePriceQuoteDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PriceQuoteDto>({
      method: 'POST',
      url: '/api/app/price-quote',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/price-quote/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PriceQuoteDto>({
      method: 'GET',
      url: `/api/app/price-quote/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: PriceQuoteGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<PriceQuoteDto>>({
      method: 'GET',
      url: '/api/app/price-quote',
      params: { sessionId: input.sessionId, providerId: input.providerId, status: input.status, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  reject = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/price-quote/${id}/reject`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdatePriceQuoteDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PriceQuoteDto>({
      method: 'PUT',
      url: `/api/app/price-quote/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}