import type { CourseFieldDto, CreateUpdateCourseFieldDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedAndSortedResultRequestDto, PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CourseFieldService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateCourseFieldDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseFieldDto>({
      method: 'POST',
      url: '/api/app/course-field',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/course-field/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseFieldDto>({
      method: 'GET',
      url: `/api/app/course-field/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getAllActive = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseFieldDto[]>({
      method: 'GET',
      url: '/api/app/course-field/active',
    },
    { apiName: this.apiName,...config });
  

  getList = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<CourseFieldDto>>({
      method: 'GET',
      url: '/api/app/course-field',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateCourseFieldDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseFieldDto>({
      method: 'PUT',
      url: `/api/app/course-field/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}