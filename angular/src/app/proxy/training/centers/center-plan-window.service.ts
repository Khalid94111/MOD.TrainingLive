import type { CenterPlanWindowDto, CenterPlanWindowGetListInput, CreateUpdateCenterPlanWindowDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CenterPlanWindowService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateCenterPlanWindowDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CenterPlanWindowDto>({
      method: 'POST',
      url: '/api/app/center-plan-window',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/center-plan-window/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CenterPlanWindowDto>({
      method: 'GET',
      url: `/api/app/center-plan-window/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: CenterPlanWindowGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<CenterPlanWindowDto>>({
      method: 'GET',
      url: '/api/app/center-plan-window',
      params: { year: input.year, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateCenterPlanWindowDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CenterPlanWindowDto>({
      method: 'PUT',
      url: `/api/app/center-plan-window/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });


  close = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CenterPlanWindowDto>({
      method: 'POST',
      url: `/api/app/center-plan-window/${id}/close`,
    },
    { apiName: this.apiName,...config });
}