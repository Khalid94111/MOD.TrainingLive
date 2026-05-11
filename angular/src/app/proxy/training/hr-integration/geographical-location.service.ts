import type { GeographicalLocationDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { ListResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class GeographicalLocationService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  getCities = (countryId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ListResultDto<GeographicalLocationDto>>({
      method: 'GET',
      url: `/api/app/geographical-location/cities/${countryId}`,
    },
    { apiName: this.apiName,...config });
  

  getCountries = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, ListResultDto<GeographicalLocationDto>>({
      method: 'GET',
      url: '/api/app/geographical-location/countries',
    },
    { apiName: this.apiName,...config });
}