import type { CreateUpdateTrainingProviderDto, TrainingProviderDto, TrainingProviderGetListInput } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TrainingProviderService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateTrainingProviderDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingProviderDto>({
      method: 'POST',
      url: '/api/app/training-provider',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/training-provider/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingProviderDto>({
      method: 'GET',
      url: `/api/app/training-provider/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getAllActive = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingProviderDto[]>({
      method: 'GET',
      url: '/api/app/training-provider/active',
    },
    { apiName: this.apiName,...config });
  

  getList = (input: TrainingProviderGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TrainingProviderDto>>({
      method: 'GET',
      url: '/api/app/training-provider',
      params: { filter: input.filter, isActive: input.isActive, isApproved: input.isApproved, scope: input.scope, countryId: input.countryId, isFromNebras: input.isFromNebras, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateTrainingProviderDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingProviderDto>({
      method: 'PUT',
      url: `/api/app/training-provider/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}