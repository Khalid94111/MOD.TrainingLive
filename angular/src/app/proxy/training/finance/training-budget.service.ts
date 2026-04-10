import type { CreateUpdateTrainingBudgetDto, TrainingBudgetDto, TrainingBudgetGetListInput } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TrainingBudgetService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateTrainingBudgetDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingBudgetDto>({
      method: 'POST',
      url: '/api/app/training-budget',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/training-budget/${id}`,
    },
    { apiName: this.apiName,...config });
  

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
      params: { year: input.year, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateTrainingBudgetDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingBudgetDto>({
      method: 'PUT',
      url: `/api/app/training-budget/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}