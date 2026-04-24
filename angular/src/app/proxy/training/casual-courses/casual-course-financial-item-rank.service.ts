import type { CasualCourseFinancialDto, UpdateRankRateDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CasualCourseFinancialItemRankService {
  private restService = inject(RestService);
  apiName = 'Default';


  updateRate = (id: string, input: UpdateRankRateDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CasualCourseFinancialDto>({
      method: 'PUT',
      url: `/api/app/casual-course-financial-item-rank/${id}/rate`,
      body: input,
    },
    { apiName: this.apiName,...config });
}
