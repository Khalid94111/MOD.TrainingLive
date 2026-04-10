import type { CreateExchangeRateDto, ExchangeRateDto, ExchangeRateGetListInput } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ExchangeRateService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateExchangeRateDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ExchangeRateDto>({
      method: 'POST',
      url: '/api/app/exchange-rate',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/exchange-rate/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getActive = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, ExchangeRateDto>({
      method: 'GET',
      url: '/api/app/exchange-rate/active',
    },
    { apiName: this.apiName,...config });
  

  getList = (input: ExchangeRateGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<ExchangeRateDto>>({
      method: 'GET',
      url: '/api/app/exchange-rate',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
}