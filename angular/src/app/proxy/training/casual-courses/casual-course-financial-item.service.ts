import type { CasualCourseFinancialItemDto, CreateCasualCourseFinancialItemDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CasualCourseFinancialItemService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  addItem = (casualCourseId: string, input: CreateCasualCourseFinancialItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseFinancialItemDto>({
      method: 'POST',
      url: `/api/app/casual-course-financial-item/item/${casualCourseId}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  deleteItem = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/casual-course-financial-item/${id}/item`,
    },
    { apiName: this.apiName,...config });
  

  getListByCasualCourse = (casualCourseId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseFinancialItemDto[]>({
      method: 'GET',
      url: `/api/app/casual-course-financial-item/by-casual-course/${casualCourseId}`,
    },
    { apiName: this.apiName,...config });
}