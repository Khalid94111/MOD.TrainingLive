import type {
  MarkTrainingExpenseRecoveryReviewedDto,
  MarkTrainingExpenseRecoverySettledDto,
  TrainingExpenseRecoveryDto,
  TrainingExpenseRecoveryGetListInput,
} from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TrainingExpenseRecoveryService {
  private restService = inject(RestService);
  apiName = 'Default';

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingExpenseRecoveryDto>({
      method: 'GET',
      url: `/api/app/training-expense-recovery/${id}`,
    }, { apiName: this.apiName, ...config });

  getList = (input: TrainingExpenseRecoveryGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TrainingExpenseRecoveryDto>>({
      method: 'GET',
      url: '/api/app/training-expense-recovery',
      params: {
        casualCourseId: input.casualCourseId,
        status: input.status,
        search: input.search,
        sorting: input.sorting,
        skipCount: input.skipCount,
        maxResultCount: input.maxResultCount,
      },
    }, { apiName: this.apiName, ...config });

  markReviewed = (id: string, input: MarkTrainingExpenseRecoveryReviewedDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingExpenseRecoveryDto>({
      method: 'POST',
      url: `/api/app/training-expense-recovery/${id}/mark-reviewed`,
      body: input,
    }, { apiName: this.apiName, ...config });

  refresh = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, number>({
      method: 'POST',
      url: '/api/app/training-expense-recovery/refresh',
    }, { apiName: this.apiName, ...config });

  markItemSettled = (id: string, itemId: string, input: MarkTrainingExpenseRecoverySettledDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingExpenseRecoveryDto>({
      method: 'POST',
      url: `/api/app/training-expense-recovery/${id}/mark-item-settled/${itemId}`,
      body: input,
    }, { apiName: this.apiName, ...config });

  markAllSettled = (id: string, input: MarkTrainingExpenseRecoverySettledDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TrainingExpenseRecoveryDto>({
      method: 'POST',
      url: `/api/app/training-expense-recovery/${id}/mark-all-settled`,
      body: input,
    }, { apiName: this.apiName, ...config });
}
