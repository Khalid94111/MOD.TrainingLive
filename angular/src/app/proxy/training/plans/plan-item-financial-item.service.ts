import type { CreateUpdatePlanItemFinancialItemDto, PlanItemFinancialItemDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PlanItemFinancialItemService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  autoFillFromDefaults = (planItemId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/plan-item-financial-item/auto-fill-from-defaults/${planItemId}`,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateUpdatePlanItemFinancialItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PlanItemFinancialItemDto>({
      method: 'POST',
      url: '/api/app/plan-item-financial-item',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/plan-item-financial-item/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getListByPlanItem = (planItemId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PlanItemFinancialItemDto[]>({
      method: 'GET',
      url: `/api/app/plan-item-financial-item/by-plan-item/${planItemId}`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdatePlanItemFinancialItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PlanItemFinancialItemDto>({
      method: 'PUT',
      url: `/api/app/plan-item-financial-item/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}