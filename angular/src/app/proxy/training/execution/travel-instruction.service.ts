import type { CreateUpdateTravelInstructionDto, TravelInstructionDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TravelInstructionService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  cancel = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TravelInstructionDto>({
      method: 'POST',
      url: `/api/app/travel-instruction/${id}/cancel`,
    },
    { apiName: this.apiName,...config });
  

  createOrUpdate = (input: CreateUpdateTravelInstructionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TravelInstructionDto>({
      method: 'POST',
      url: '/api/app/travel-instruction/or-update',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  getByParent = (casualCourseId: string, sessionId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TravelInstructionDto>({
      method: 'GET',
      url: '/api/app/travel-instruction/by-parent',
      params: { casualCourseId, sessionId },
    },
    { apiName: this.apiName,...config });
  

  issue = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TravelInstructionDto>({
      method: 'POST',
      url: `/api/app/travel-instruction/${id}/issue`,
    },
    { apiName: this.apiName,...config });
}