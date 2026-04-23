import type { AssignScenarioDto, CasualCourseDetailDto, CasualCourseDto, CasualCourseGetListInput, CreateUpdateCasualCourseDto, EstimatePreviewDto, EstimatePreviewInput, RejectDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';
import type { CreatePlanNoteDto, ReturnReasonDto } from '../plans/dtos/models';

@Injectable({
  providedIn: 'root',
})
export class CasualCourseService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  assignScenario = (id: string, input: AssignScenarioDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseDto>({
      method: 'POST',
      url: `/api/app/casual-course/${id}/assign-scenario`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateUpdateCasualCourseDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseDto>({
      method: 'POST',
      url: '/api/app/casual-course',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/casual-course/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseDto>({
      method: 'GET',
      url: `/api/app/casual-course/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getDetail = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseDetailDto>({
      method: 'GET',
      url: `/api/app/casual-course/${id}/detail`,
    },
    { apiName: this.apiName,...config });
  

  getEstimatePreview = (input: EstimatePreviewInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, EstimatePreviewDto>({
      method: 'GET',
      url: '/api/app/casual-course/estimate-preview',
      params: { tenantCourseId: input.tenantCourseId, courseType: input.courseType, durationDays: input.durationDays, nomineeCount: input.nomineeCount },
    },
    { apiName: this.apiName,...config });
  

  getList = (input: CasualCourseGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<CasualCourseDto>>({
      method: 'GET',
      url: '/api/app/casual-course',
      params: { year: input.year, status: input.status, unitId: input.unitId, onlyMyRequests: input.onlyMyRequests, isReturnedOnly: input.isReturnedOnly, search: input.search, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  headApprove = (id: string, note: CreatePlanNoteDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseDto>({
      method: 'POST',
      url: `/api/app/casual-course/${id}/head-approve`,
      body: note,
    },
    { apiName: this.apiName,...config });
  

  reject = (id: string, input: RejectDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseDto>({
      method: 'POST',
      url: `/api/app/casual-course/${id}/reject`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  resubmit = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseDto>({
      method: 'POST',
      url: `/api/app/casual-course/${id}/resubmit`,
    },
    { apiName: this.apiName,...config });
  

  return = (id: string, input: ReturnReasonDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseDto>({
      method: 'POST',
      url: `/api/app/casual-course/${id}/return`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  startReview = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseDto>({
      method: 'POST',
      url: `/api/app/casual-course/${id}/start-review`,
    },
    { apiName: this.apiName,...config });
  

  submit = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseDto>({
      method: 'POST',
      url: `/api/app/casual-course/${id}/submit`,
    },
    { apiName: this.apiName,...config });
  

  tdApprove = (id: string, note: CreatePlanNoteDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseDto>({
      method: 'POST',
      url: `/api/app/casual-course/${id}/t-dApprove`,
      body: note,
    },
    { apiName: this.apiName,...config });
  

  ugmApprove = (id: string, note: CreatePlanNoteDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseDto>({
      method: 'POST',
      url: `/api/app/casual-course/${id}/u-gMApprove`,
      body: note,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateCasualCourseDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseDto>({
      method: 'PUT',
      url: `/api/app/casual-course/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}