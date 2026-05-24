import type { CancelSessionDto, CourseSessionDetailDto, CourseSessionDto, CourseSessionGetListInput, SelectSessionPriceQuoteDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CourseSessionService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  cancel = (id: string, input: CancelSessionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseSessionDetailDto>({
      method: 'POST',
      url: `/api/app/course-session/${id}/cancel`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseSessionDetailDto>({
      method: 'GET',
      url: `/api/app/course-session/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: CourseSessionGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<CourseSessionDto>>({
      method: 'GET',
      url: '/api/app/course-session',
      params: { trainingPlanItemId: input.trainingPlanItemId, trainingCenterPlanItemId: input.trainingCenterPlanItemId, tenantCourseId: input.tenantCourseId, courseType: input.courseType, status: input.status, executionStage: input.executionStage, planYear: input.planYear, preferredQuarter: input.preferredQuarter, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  markCompleted = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseSessionDetailDto>({
      method: 'POST',
      url: `/api/app/course-session/${id}/mark-completed`,
    },
    { apiName: this.apiName,...config });
  

  markInProgress = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseSessionDetailDto>({
      method: 'POST',
      url: `/api/app/course-session/${id}/mark-in-progress`,
    },
    { apiName: this.apiName,...config });
  

  selectPriceQuote = (id: string, input: SelectSessionPriceQuoteDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseSessionDetailDto>({
      method: 'POST',
      url: `/api/app/course-session/${id}/select-price-quote`,
      body: input,
    },
    { apiName: this.apiName,...config });
}