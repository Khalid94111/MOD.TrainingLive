import type { CenterRoleAssignmentDto, CreateUpdateTrainingCenterDto, SetCenterRoleAssignmentsDto, TrainingCenterDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedAndSortedResultRequestDto, PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TrainingCenterService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateTrainingCenterDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingCenterDto>({
      method: 'POST',
      url: '/api/app/training-center',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/training-center/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingCenterDto>({
      method: 'GET',
      url: `/api/app/training-center/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TrainingCenterDto>>({
      method: 'GET',
      url: '/api/app/training-center',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getRoleAssignments = (centerId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CenterRoleAssignmentDto[]>({
      method: 'GET',
      url: `/api/app/training-center/role-assignments/${centerId}`,
    },
    { apiName: this.apiName,...config });
  

  setRoleAssignments = (centerId: string, input: SetCenterRoleAssignmentsDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CenterRoleAssignmentDto[]>({
      method: 'POST',
      url: `/api/app/training-center/set-role-assignments/${centerId}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateTrainingCenterDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingCenterDto>({
      method: 'PUT',
      url: `/api/app/training-center/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}