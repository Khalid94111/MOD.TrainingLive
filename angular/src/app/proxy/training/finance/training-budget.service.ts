import type { TrainingBudgetDto, TrainingBudgetGetListInput, UpdateAlertThresholdDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TrainingBudgetService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingBudgetDto>({
      method: 'GET',
      url: `/api/app/training-budget/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: TrainingBudgetGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TrainingBudgetDto>>({
      method: 'GET',
      url: '/api/app/training-budget',
      params: { year: input.year, financialItemId: input.financialItemId, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });

  getYears = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, number[]>({
      method: 'GET',
      url: '/api/app/training-budget/years',
    },
    { apiName: this.apiName,...config });

  setThreshold = (financialItemId: string, year: number, input: UpdateAlertThresholdDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingBudgetDto>({
      method: 'POST',
      url: `/api/app/training-budget/set-threshold/${financialItemId}`,
      params: { year },
      body: input,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateAlertThresholdDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingBudgetDto>({
      method: 'PUT',
      url: `/api/app/training-budget/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}
