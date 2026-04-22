import type { CreatePlanNoteDto, PlanNoteDto, PlanNoteGetListInput } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PlanNoteService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreatePlanNoteDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PlanNoteDto>({
      method: 'POST',
      url: '/api/app/plan-note',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: PlanNoteGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PlanNoteDto[]>({
      method: 'GET',
      url: '/api/app/plan-note',
      params: { entityType: input.entityType, entityId: input.entityId, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
}