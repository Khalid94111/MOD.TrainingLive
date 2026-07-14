import type { CourseCatalogDto, CourseCatalogGetListInput, CourseCatalogSubscribedTenantDto, CreateUpdateCourseCatalogDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CourseCatalogService {
  private restService = inject(RestService);
  apiName = 'Default';
  

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
  

  getList = (input: CourseCatalogGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<CourseCatalogDto>>({
      method: 'GET',
      url: '/api/app/course-catalog',
      params: { filter: input.filter, fieldId: input.fieldId, category: input.category, isActive: input.isActive, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getSubscribedTenants = (catalogCourseId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseCatalogSubscribedTenantDto[]>({
      method: 'GET',
      url: `/api/app/course-catalog/subscribed-tenants/${catalogCourseId}`,
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