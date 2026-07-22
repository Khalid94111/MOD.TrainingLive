import { inject, Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';

export interface TravelEmployeeLookupResult {
  id: string;
  name: string;
  employeeNumber: string;
  department: string;
  rankId?: string;
  rankName: string;
  category: number;
  jobTitle?: string;
  phone?: string;
  email?: string;
  passportNumber?: string;
  passportExpiryDate?: string;
  totalTrips?: number;
  lastClothingAllowanceDate?: string;
  yearsOfService?: number;
}

@Injectable({
  providedIn: 'root',
})
export class TravelLookupService {
  private restService = inject(RestService);

  findEmployees(employeeNumbers: string[]) {
    return this.restService.request<string[], TravelEmployeeLookupResult[]>(
      { method: 'POST', url: '/api/travel/employees/find', body: employeeNumbers },
      { apiName: 'default' }
    );
  }
}
