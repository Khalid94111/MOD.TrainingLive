import type { BudgetReallocationDto, BudgetReallocationGetListInput, MarkReallocationApprovedDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class BudgetReallocationService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BudgetReallocationDto>({
      method: 'GET',
      url: `/api/app/budget-reallocation/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: BudgetReallocationGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<BudgetReallocationDto>>({
      method: 'GET',
      url: '/api/app/budget-reallocation',
      params: { casualCourseId: input.casualCourseId, coursePaymentId: input.coursePaymentId, toFinancialItemId: input.toFinancialItemId, fundingSourceVoteCode: input.fundingSourceVoteCode, status: input.status, createdFrom: input.createdFrom, createdTo: input.createdTo, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  markApproved = (id: string, input: MarkReallocationApprovedDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BudgetReallocationDto>({
      method: 'POST',
      url: `/api/app/budget-reallocation/${id}/mark-approved`,
      body: input,
    },
    { apiName: this.apiName,...config });
}