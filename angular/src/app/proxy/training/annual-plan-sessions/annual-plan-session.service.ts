import type { AnnualPlanProgressDto, AvailableSubstituteDto, CreateExternalSessionDto, CreateInternalSessionDto, PlanItemQueueGetListInput, PlanItemQueueItemDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';
import type { CourseSessionDto } from '../plans/dtos/models';

@Injectable({
  providedIn: 'root',
})
export class AnnualPlanSessionService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  createExternalSession = (input: CreateExternalSessionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseSessionDto>({
      method: 'POST',
      url: '/api/app/annual-plan-session/external-session',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  createInternalSession = (input: CreateInternalSessionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CourseSessionDto>({
      method: 'POST',
      url: '/api/app/annual-plan-session/internal-session',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  getAvailableSubstitutes = (planItemId: string, originalEmployeeId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AvailableSubstituteDto[]>({
      method: 'GET',
      url: '/api/app/annual-plan-session/available-substitutes',
      params: { planItemId, originalEmployeeId },
    },
    { apiName: this.apiName,...config });
  

  getPlanItemsQueue = (input: PlanItemQueueGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<PlanItemQueueItemDto>>({
      method: 'GET',
      url: '/api/app/annual-plan-session/plan-items-queue',
      params: { year: input.year, quarter: input.quarter, priority: input.priority, courseType: input.courseType, unitId: input.unitId, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getProgressDashboard = (year: number, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AnnualPlanProgressDto>({
      method: 'GET',
      url: '/api/app/annual-plan-session/progress-dashboard',
      params: { year },
    },
    { apiName: this.apiName,...config });
}