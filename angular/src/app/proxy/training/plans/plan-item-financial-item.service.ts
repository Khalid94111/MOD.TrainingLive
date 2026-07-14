import type { CreateUpdatePlanItemFinancialItemDto, PlanItemFinancialItemDto, UpdateAmountDto, UpdateNotesDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PlanItemFinancialItemService {
  private restService = inject(RestService);
  apiName = 'Default';
  

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
  

  updateAmount = (id: string, input: UpdateAmountDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PlanItemFinancialItemDto>({
      method: 'PUT',
      url: `/api/app/plan-item-financial-item/${id}/amount`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  updateNotes = (id: string, input: UpdateNotesDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'PUT',
      url: `/api/app/plan-item-financial-item/${id}/notes`,
      body: input,
    },
    { apiName: this.apiName,...config });
}