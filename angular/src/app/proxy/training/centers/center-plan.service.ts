import type { CenterPlanGetListInput, CreateUpdateCenterPlanDto, TrainingCenterPlanDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CenterPlanService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  approve = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingCenterPlanDto>({
      method: 'POST',
      url: `/api/app/center-plan/${id}/approve`,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateUpdateCenterPlanDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingCenterPlanDto>({
      method: 'POST',
      url: '/api/app/center-plan',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/center-plan/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingCenterPlanDto>({
      method: 'GET',
      url: `/api/app/center-plan/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: CenterPlanGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TrainingCenterPlanDto>>({
      method: 'GET',
      url: '/api/app/center-plan',
      params: { centerId: input.centerId, year: input.year, status: input.status, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  reject = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingCenterPlanDto>({
      method: 'POST',
      url: `/api/app/center-plan/${id}/reject`,
    },
    { apiName: this.apiName,...config });
  

  return = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingCenterPlanDto>({
      method: 'POST',
      url: `/api/app/center-plan/${id}/return`,
    },
    { apiName: this.apiName,...config });
  

  submit = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingCenterPlanDto>({
      method: 'POST',
      url: `/api/app/center-plan/${id}/submit`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateCenterPlanDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingCenterPlanDto>({
      method: 'PUT',
      url: `/api/app/center-plan/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}