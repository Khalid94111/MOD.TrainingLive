import type { PlanItemFinancialItemRankDto, UpdateRateDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PlanItemFinancialItemRankService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  getListByPifi = (pifiId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PlanItemFinancialItemRankDto[]>({
      method: 'GET',
      url: `/api/app/plan-item-financial-item-rank/by-pifi/${pifiId}`,
    },
    { apiName: this.apiName,...config });
  

  updateRate = (rankRowId: string, input: UpdateRateDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'PUT',
      url: `/api/app/plan-item-financial-item-rank/rate/${rankRowId}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}