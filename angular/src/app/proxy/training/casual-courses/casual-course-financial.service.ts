import type { CasualCourseFinancialDto, CreateCasualCourseFinancialDto } from './dtos/models';
import type { FundingScenario } from '../enums/funding-scenario.enum';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CasualCourseFinancialService {
  private restService = inject(RestService);
  apiName = 'Default';


  addItem = (casualCourseId: string, input: CreateCasualCourseFinancialDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseFinancialDto>({
      method: 'POST',
      url: `/api/app/casual-course-financial/item/${casualCourseId}`,
      body: input,
    },
    { apiName: this.apiName,...config });


  autoFillFromDefaults = (casualCourseId: string, scenario: FundingScenario, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseFinancialDto[]>({
      method: 'POST',
      url: `/api/app/casual-course-financial/auto-fill-from-defaults/${casualCourseId}`,
      params: { scenario },
    },
    { apiName: this.apiName,...config });


  deleteItem = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/casual-course-financial/${id}/item`,
    },
    { apiName: this.apiName,...config });


  getListByCasualCourse = (casualCourseId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseFinancialDto[]>({
      method: 'GET',
      url: `/api/app/casual-course-financial/by-casual-course/${casualCourseId}`,
    },
    { apiName: this.apiName,...config });
}
