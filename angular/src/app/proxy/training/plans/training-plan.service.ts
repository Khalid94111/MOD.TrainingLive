import type { CreateUpdateTrainingPlanDto, ReturnReasonDto, TrainingPlanDto, TrainingPlanGetListInput } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TrainingPlanService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  approve = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/training-plan/${id}/approve`,
    },
    { apiName: this.apiName,...config });
  

  closeSubmissionWindow = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/training-plan/${id}/close-submission-window`,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateUpdateTrainingPlanDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingPlanDto>({
      method: 'POST',
      url: '/api/app/training-plan',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/training-plan/${id}`,
    },
    { apiName: this.apiName,...config });
  

  finalApprove = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/training-plan/${id}/final-approve`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingPlanDto>({
      method: 'GET',
      url: `/api/app/training-plan/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: TrainingPlanGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TrainingPlanDto>>({
      method: 'GET',
      url: '/api/app/training-plan',
      params: { year: input.year, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  openSubmissionWindow = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/training-plan/${id}/open-submission-window`,
    },
    { apiName: this.apiName,...config });
  

  reject = (id: string, reason: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/training-plan/${id}/reject`,
      params: { reason },
    },
    { apiName: this.apiName,...config });
  

  resubmit = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/training-plan/${id}/resubmit`,
    },
    { apiName: this.apiName,...config });
  

  returnToCreator = (id: string, input: ReturnReasonDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/training-plan/${id}/return-to-creator`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  returnToStaff = (id: string, reason: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/training-plan/${id}/return-to-staff`,
      params: { reason },
    },
    { apiName: this.apiName,...config });
  

  submitForReview = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/training-plan/${id}/submit-for-review`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateTrainingPlanDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingPlanDto>({
      method: 'PUT',
      url: `/api/app/training-plan/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}