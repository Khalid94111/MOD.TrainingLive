import type { AdjustCenterPlanItemCapacityDto, AvailableCenterPlanItemDto, CenterPlanItemGetListInput, CenterPlanNominationDto, CenterPlanNominationGetListInput, CreateUpdateCenterPlanItemDto, SetPlanItemUnitsDto, TrainingCenterPlanItemDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { ListResultDto, PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CenterPlanItemService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  adjustCapacity = (id: string, input: AdjustCenterPlanItemCapacityDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingCenterPlanItemDto>({
      method: 'POST',
      url: `/api/app/center-plan-item/${id}/adjust-capacity`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

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
  

  getAvailableForAnnualPlan = (annualPlanId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ListResultDto<AvailableCenterPlanItemDto>>({
      method: 'GET',
      url: `/api/app/center-plan-item/available-for-annual-plan/${annualPlanId}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: CenterPlanItemGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TrainingCenterPlanItemDto>>({
      method: 'GET',
      url: '/api/app/center-plan-item',
      params: { planId: input.planId, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getNominations = (input: CenterPlanNominationGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<CenterPlanNominationDto>>({
      method: 'GET',
      url: '/api/app/center-plan-item/nominations',
      params: { centerPlanId: input.centerPlanId, centerId: input.centerId, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
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