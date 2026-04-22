import type { CreateUpdateFinancialItemRankAmountDto, FinancialItemRankAmountDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class FinancialItemRankAmountService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateFinancialItemRankAmountDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FinancialItemRankAmountDto>({
      method: 'POST',
      url: '/api/app/financial-item-rank-amount',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/financial-item-rank-amount/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FinancialItemRankAmountDto>({
      method: 'GET',
      url: `/api/app/financial-item-rank-amount/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getByFinancialItem = (financialItemId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FinancialItemRankAmountDto[]>({
      method: 'GET',
      url: `/api/app/financial-item-rank-amount/by-financial-item/${financialItemId}`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateFinancialItemRankAmountDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FinancialItemRankAmountDto>({
      method: 'PUT',
      url: `/api/app/financial-item-rank-amount/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}