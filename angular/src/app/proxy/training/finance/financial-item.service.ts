import type { CreateUpdateFinancialItemDto, FinancialItemDto, FinancialItemGetListInput, FinancialItemSubItemDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class FinancialItemService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateFinancialItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FinancialItemDto>({
      method: 'POST',
      url: '/api/app/financial-item',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/financial-item/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FinancialItemDto>({
      method: 'GET',
      url: `/api/app/financial-item/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: FinancialItemGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<FinancialItemDto>>({
      method: 'GET',
      url: '/api/app/financial-item',
      params: { filter: input.filter, isActive: input.isActive, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getSubItems = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, FinancialItemSubItemDto[]>({
      method: 'GET',
      url: '/api/app/financial-item/sub-items',
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateFinancialItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FinancialItemDto>({
      method: 'PUT',
      url: `/api/app/financial-item/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}