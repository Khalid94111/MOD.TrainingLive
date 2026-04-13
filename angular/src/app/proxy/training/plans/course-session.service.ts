import type { CourseSessionDto, CourseSessionGetListInput, CreateUpdateCourseSessionDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CourseSessionService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateCourseSessionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseSessionDto>({
      method: 'POST',
      url: '/api/app/course-session',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/course-session/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseSessionDto>({
      method: 'GET',
      url: `/api/app/course-session/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: CourseSessionGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<CourseSessionDto>>({
      method: 'GET',
      url: '/api/app/course-session',
      params: { courseId: input.courseId, status: input.status, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateCourseSessionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseSessionDto>({
      method: 'PUT',
      url: `/api/app/course-session/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}