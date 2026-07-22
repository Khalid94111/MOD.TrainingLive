import { inject, Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable, shareReplay } from 'rxjs';

export interface TravelTypeDefinitionDto {
  id: string;
  code: number;
  name: string;
  isActive: boolean;
}

export interface CreateUpdateTravelTypeDefinitionDto {
  code: number;
  name: string;
  isActive: boolean;
}

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class TravelTypeDefinitionService {
  private restService = inject(RestService);

  private activeTypes$: Observable<TravelTypeDefinitionDto[]> | null = null;

  getActiveTypes(): Observable<TravelTypeDefinitionDto[]> {
    if (!this.activeTypes$) {
      this.activeTypes$ = new Observable<TravelTypeDefinitionDto[]>((observer) => {
        this.getList({ isActive: true, maxResultCount: 100 }).subscribe({
          next: (res) => { observer.next(res.items || []); observer.complete(); },
          error: (err) => observer.error(err),
        });
      }).pipe(shareReplay(1));
    }
    return this.activeTypes$;
  }

  invalidateCache(): void {
    this.activeTypes$ = null;
  }

  getList(params?: { isActive?: boolean; filter?: string; skipCount?: number; maxResultCount?: number }): Observable<PagedResultDto<TravelTypeDefinitionDto>> {
    return this.restService.request<any, PagedResultDto<TravelTypeDefinitionDto>>(
      { method: 'GET', url: '/api/travel/travel-types', params },
      { apiName: 'Travel' }
    );
  }

  create(body: CreateUpdateTravelTypeDefinitionDto): Observable<TravelTypeDefinitionDto> {
    return this.restService.request<CreateUpdateTravelTypeDefinitionDto, TravelTypeDefinitionDto>(
      { method: 'POST', url: '/api/travel/travel-types', body },
      { apiName: 'Travel' }
    );
  }

  update(id: string, body: CreateUpdateTravelTypeDefinitionDto): Observable<TravelTypeDefinitionDto> {
    return this.restService.request<CreateUpdateTravelTypeDefinitionDto, TravelTypeDefinitionDto>(
      { method: 'PUT', url: `/api/travel/travel-types/${id}`, body },
      { apiName: 'Travel' }
    );
  }

  delete(id: string): Observable<void> {
    return this.restService.request<void, void>(
      { method: 'DELETE', url: `/api/travel/travel-types/${id}` },
      { apiName: 'Travel' }
    );
  }
}
