import type { CreateUpdateTrainingPlanItemDto, PlanItemConditionDto, ReturnReasonDto, TrainingPlanItemDto, TrainingPlanItemGetListInput } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TrainingPlanItemService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateTrainingPlanItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingPlanItemDto>({
      method: 'POST',
      url: '/api/app/training-plan-item',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/training-plan-item/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingPlanItemDto>({
      method: 'GET',
      url: `/api/app/training-plan-item/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getConditions = (planItemId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PlanItemConditionDto[]>({
      method: 'GET',
      url: `/api/app/training-plan-item/conditions/${planItemId}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: TrainingPlanItemGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TrainingPlanItemDto>>({
      method: 'GET',
      url: '/api/app/training-plan-item',
      params: { planId: input.planId, courseType: input.courseType, preferredQuarter: input.preferredQuarter, unitId: input.unitId, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  return = (id: string, input: ReturnReasonDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/training-plan-item/${id}/return`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateTrainingPlanItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingPlanItemDto>({
      method: 'PUT',
      url: `/api/app/training-plan-item/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}