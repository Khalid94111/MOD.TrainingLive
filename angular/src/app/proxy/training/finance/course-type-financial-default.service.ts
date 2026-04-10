import type { CourseTypeFinancialItemDefaultDto, CreateCourseTypeFinancialItemDefaultDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';
import type { CourseType } from '../enums/course-type.enum';

@Injectable({
  providedIn: 'root',
})
export class CourseTypeFinancialDefaultService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateCourseTypeFinancialItemDefaultDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseTypeFinancialItemDefaultDto>({
      method: 'POST',
      url: '/api/app/course-type-financial-default',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/course-type-financial-default/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (courseType: CourseType, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseTypeFinancialItemDefaultDto[]>({
      method: 'GET',
      url: '/api/app/course-type-financial-default',
      params: { courseType },
    },
    { apiName: this.apiName,...config });
}