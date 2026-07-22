import { inject, Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface TravelFundingSourceVoteRuleDto {
  id: string;
  paymentType: string;
  fundingSourceVoteCode: string;
  isActive: boolean;
}

export interface UpdateTravelFundingSourceVoteRuleDto {
  fundingSourceVoteCode: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class FundingSourceVoteRuleService {
  private restService = inject(RestService);

  getList(): Observable<TravelFundingSourceVoteRuleDto[]> {
    return this.restService.request<void, TravelFundingSourceVoteRuleDto[]>(
      { method: 'GET', url: '/api/travel/funding-source-vote-rules' },
      { apiName: 'Travel' }
    );
  }

  update(id: string, body: UpdateTravelFundingSourceVoteRuleDto): Observable<TravelFundingSourceVoteRuleDto> {
    return this.restService.request<UpdateTravelFundingSourceVoteRuleDto, TravelFundingSourceVoteRuleDto>(
      { method: 'PUT', url: `/api/travel/funding-source-vote-rules/${id}`, body },
      { apiName: 'Travel' }
    );
  }
}
