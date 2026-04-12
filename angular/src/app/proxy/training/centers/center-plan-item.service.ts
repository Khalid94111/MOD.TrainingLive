import type { CenterPlanItemGetListInput, CreateUpdateCenterPlanItemDto, SetPlanItemUnitsDto, TrainingCenterPlanItemDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CenterPlanItemService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateCenterPlanItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingCenterPlanItemDto>({
      method: 'POST',
      url: '/api/app/center-plan-item',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/center-plan-item/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingCenterPlanItemDto>({
      method: 'GET',
      url: `/api/app/center-plan-item/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: CenterPlanItemGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TrainingCenterPlanItemDto>>({
      method: 'GET',
      url: '/api/app/center-plan-item',
      params: { planId: input.planId, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  setUnits = (id: string, input: SetPlanItemUnitsDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingCenterPlanItemDto>({
      method: 'POST',
      url: `/api/app/center-plan-item/${id}/set-units`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateCenterPlanItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingCenterPlanItemDto>({
      method: 'PUT',
      url: `/api/app/center-plan-item/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}