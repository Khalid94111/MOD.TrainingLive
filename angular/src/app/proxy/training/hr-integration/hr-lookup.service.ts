import type { EmployeeLookupDto, RankLookupDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class HrLookupService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  getAllRanks = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, RankLookupDto[]>({
      method: 'GET',
      url: '/api/app/hr-lookup/ranks',
    },
    { apiName: this.apiName,...config });
  

  getByServiceNumber = (serviceNumber: string, orgUnitId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, EmployeeLookupDto>({
      method: 'GET',
      url: `/api/app/hr-lookup/by-service-number/${orgUnitId}`,
      params: { serviceNumber },
    },
    { apiName: this.apiName,...config });
  

  getCurrentEmployee = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, EmployeeLookupDto>({
      method: 'GET',
      url: '/api/app/hr-lookup/current-employee',
    },
    { apiName: this.apiName,...config });
  

  getEmployeesByUnit = (unitId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, EmployeeLookupDto[]>({
      method: 'GET',
      url: `/api/app/hr-lookup/employees-by-unit/${unitId}`,
    },
    { apiName: this.apiName,...config });
}