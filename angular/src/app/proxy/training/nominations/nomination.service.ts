import type { ApproveRejectNominationDto, CreateNominationDto, NominationApprovalDto, NominationDto, NominationGetListInput } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';
import type { ReturnReasonDto } from '../plans/dtos/models';

@Injectable({
  providedIn: 'root',
})
export class NominationService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  approve = (id: string, input: ApproveRejectNominationDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/nomination/${id}/approve`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  createBatch = (input: CreateNominationDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, NominationDto[]>({
      method: 'POST',
      url: '/api/app/nomination/batch',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/nomination/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, NominationDto>({
      method: 'GET',
      url: `/api/app/nomination/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getApprovalChain = (nominationId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, NominationApprovalDto[]>({
      method: 'GET',
      url: `/api/app/nomination/approval-chain/${nominationId}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: NominationGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<NominationDto>>({
      method: 'GET',
      url: '/api/app/nomination',
      params: { planItemId: input.planItemId, sessionId: input.sessionId, status: input.status, employeeId: input.employeeId, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  reject = (id: string, input: ApproveRejectNominationDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/nomination/${id}/reject`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  return = (id: string, input: ReturnReasonDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/nomination/${id}/return`,
      body: input,
    },
    { apiName: this.apiName,...config });
}