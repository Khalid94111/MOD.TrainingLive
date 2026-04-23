import type { CasualCourseNominationDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';
import type { ReturnReasonDto } from '../plans/dtos/models';

@Injectable({
  providedIn: 'root',
})
export class CasualCourseNominationService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  add = (casualCourseId: string, employeeId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseNominationDto>({
      method: 'POST',
      url: '/api/app/casual-course-nomination',
      params: { casualCourseId, employeeId },
    },
    { apiName: this.apiName,...config });
  

  getListByCasualCourse = (casualCourseId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseNominationDto[]>({
      method: 'GET',
      url: `/api/app/casual-course-nomination/by-casual-course/${casualCourseId}`,
    },
    { apiName: this.apiName,...config });
  

  remove = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/casual-course-nomination/${id}`,
    },
    { apiName: this.apiName,...config });
  

  replace = (id: string, newEmployeeId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseNominationDto>({
      method: 'POST',
      url: `/api/app/casual-course-nomination/${id}/replace/${newEmployeeId}`,
    },
    { apiName: this.apiName,...config });
  

  return = (id: string, input: ReturnReasonDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseNominationDto>({
      method: 'POST',
      url: `/api/app/casual-course-nomination/${id}/return`,
      body: input,
    },
    { apiName: this.apiName,...config });
}