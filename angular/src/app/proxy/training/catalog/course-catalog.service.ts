import type { CatalogEnrollmentConditionDto, CourseCatalogDto, CourseCatalogGetListInput, CreateUpdateCatalogEnrollmentConditionDto, CreateUpdateCourseCatalogDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CourseCatalogService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  addCondition = (catalogCourseId: string, input: CreateUpdateCatalogEnrollmentConditionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CatalogEnrollmentConditionDto>({
      method: 'POST',
      url: `/api/app/course-catalog/condition/${catalogCourseId}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateUpdateCourseCatalogDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseCatalogDto>({
      method: 'POST',
      url: '/api/app/course-catalog',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/course-catalog/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseCatalogDto>({
      method: 'GET',
      url: `/api/app/course-catalog/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getConditions = (catalogCourseId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CatalogEnrollmentConditionDto[]>({
      method: 'GET',
      url: `/api/app/course-catalog/conditions/${catalogCourseId}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: CourseCatalogGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<CourseCatalogDto>>({
      method: 'GET',
      url: '/api/app/course-catalog',
      params: { filter: input.filter, fieldId: input.fieldId, category: input.category, isActive: input.isActive, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  removeCondition = (conditionId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/course-catalog/condition/${conditionId}`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateCourseCatalogDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseCatalogDto>({
      method: 'PUT',
      url: `/api/app/course-catalog/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}