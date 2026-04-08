import type { CourseProposalDto, CourseProposalGetListInput, CreateCourseProposalDto, ReviewCourseProposalDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CourseProposalService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateCourseProposalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseProposalDto>({
      method: 'POST',
      url: '/api/app/course-proposal',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseProposalDto>({
      method: 'GET',
      url: `/api/app/course-proposal/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: CourseProposalGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<CourseProposalDto>>({
      method: 'GET',
      url: '/api/app/course-proposal',
      params: { filter: input.filter, status: input.status, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  review = (id: string, input: ReviewCourseProposalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseProposalDto>({
      method: 'POST',
      url: `/api/app/course-proposal/${id}/review`,
      body: input,
    },
    { apiName: this.apiName,...config });
}