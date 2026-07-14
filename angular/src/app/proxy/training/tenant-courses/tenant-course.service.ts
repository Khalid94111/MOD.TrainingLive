import type { AddFromCatalogDto, TenantCourseDto, TenantCourseGetListInput, UpdateTenantCourseDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedAndSortedResultRequestDto, PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';
import type { CourseCatalogDto } from '../catalog/dtos/models';

@Injectable({
  providedIn: 'root',
})
export class TenantCourseService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  addFromCatalog = (input: AddFromCatalogDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TenantCourseDto[]>({
      method: 'POST',
      url: '/api/app/tenant-course/from-catalog',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/tenant-course/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TenantCourseDto>({
      method: 'GET',
      url: `/api/app/tenant-course/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getAvailableCatalogCourses = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<CourseCatalogDto>>({
      method: 'GET',
      url: '/api/app/tenant-course/available-catalog-courses',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getList = (input: TenantCourseGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TenantCourseDto>>({
      method: 'GET',
      url: '/api/app/tenant-course',
      params: { filter: input.filter, fieldId: input.fieldId, resultType: input.resultType, isActive: input.isActive, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateTenantCourseDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TenantCourseDto>({
      method: 'PUT',
      url: `/api/app/tenant-course/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}