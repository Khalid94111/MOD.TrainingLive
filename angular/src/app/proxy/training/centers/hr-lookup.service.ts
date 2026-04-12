import type { HrEmployeeLookupDto, HrPositionLookupDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class HrLookupService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  getEmployees = (serviceNumber: string, orgUnitId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, HrEmployeeLookupDto[]>({
      method: 'GET',
      url: `/api/app/hr-lookup/employees/${orgUnitId}`,
      params: { serviceNumber },
    },
    { apiName: this.apiName,...config });
  

  getPositions = (name: string, orgUnitId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, HrPositionLookupDto[]>({
      method: 'GET',
      url: `/api/app/hr-lookup/positions/${orgUnitId}`,
      params: { name },
    },
    { apiName: this.apiName,...config });
}