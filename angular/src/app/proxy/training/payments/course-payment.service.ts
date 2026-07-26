import type { CoursePaymentDto, CoursePaymentGetListInput, CreateUpdateCoursePaymentDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CoursePaymentService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  confirm = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CoursePaymentDto>({
      method: 'POST',
      url: `/api/app/course-payment/${id}/confirm`,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateUpdateCoursePaymentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CoursePaymentDto>({
      method: 'POST',
      url: '/api/app/course-payment',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/course-payment/${id}`,
    },
    { apiName: this.apiName,...config });
  

  downloadInvoice = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, Blob>({
      method: 'POST',
      responseType: 'blob',
      url: `/api/app/course-payment/${id}/download-invoice`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CoursePaymentDto>({
      method: 'GET',
      url: `/api/app/course-payment/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: CoursePaymentGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<CoursePaymentDto>>({
      method: 'GET',
      url: '/api/app/course-payment',
      params: { casualCourseId: input.casualCourseId, sessionId: input.sessionId, trainingProviderId: input.trainingProviderId, status: input.status, invoiceDateFrom: input.invoiceDateFrom, invoiceDateTo: input.invoiceDateTo, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateCoursePaymentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CoursePaymentDto>({
      method: 'PUT',
      url: `/api/app/course-payment/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  uploadInvoice = (id: string, file: FormData, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CoursePaymentDto>({
      method: 'POST',
      url: `/api/app/course-payment/${id}/upload-invoice`,
      body: file,
    },
    { apiName: this.apiName,...config });
}
