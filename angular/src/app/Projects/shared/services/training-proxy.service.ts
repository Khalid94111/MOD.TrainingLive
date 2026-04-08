import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

// ── Import from ABP-generated proxy (src/app/proxy/training) ──
// ABP CLI generates these at: src/app/proxy/training/
// Run: abp generate-proxy -t ng
import { CourseCatalogService as CourseCatalogProxy } from '../../../proxy/training/catalog';
import { CourseFieldService as CourseFieldProxy } from '../../../proxy/training/course-fields';
import { CourseProposalService as CourseProposalProxy } from '../../../proxy/training/course-proposals';
import { TenantCourseService as TenantCourseProxy } from '../../../proxy/training/tenant-courses';

import type {
  CourseCatalogDto,
  CreateUpdateCourseCatalogDto,
  CourseCatalogGetListInput,
  CatalogEnrollmentConditionDto,
  CreateUpdateCatalogEnrollmentConditionDto,
} from '../../../proxy/training/catalog/dtos';
import type {
  CourseFieldDto,
  CreateUpdateCourseFieldDto,
} from '../../../proxy/training/course-fields/dtos';
import type {
  CourseProposalDto,
  CreateCourseProposalDto,
  ReviewCourseProposalDto,
  CourseProposalGetListInput,
} from '../../../proxy/training/course-proposals/dtos';
import type {
  TenantCourseDto,
  AddFromCatalogDto,
  UpdateTenantCourseDto,
  TenantCourseGetListInput,
  TenantCourseConditionDto,
} from '../../../proxy/training/tenant-courses/dtos';

import type { PagedResultDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

/**
 * Wraps ABP-generated CourseCatalogService proxy.
 * All methods return Promise via firstValueFrom (no .toPromise()).
 *
 * Routes:
 *   GET    /api/app/course-catalog/{id}
 *   GET    /api/app/course-catalog
 *   POST   /api/app/course-catalog
 *   PUT    /api/app/course-catalog/{id}
 *   DELETE /api/app/course-catalog/{id}
 *   GET    /api/app/course-catalog/conditions/{catalogCourseId}
 *   POST   /api/app/course-catalog/condition/{catalogCourseId}
 *   DELETE /api/app/course-catalog/condition/{conditionId}
 */
@Injectable({ providedIn: 'root' })
export class CourseCatalogService {
  private readonly proxy = inject(CourseCatalogProxy);

  get(id: string): Promise<CourseCatalogDto> {
    return firstValueFrom(this.proxy.get(id));
  }

  getList(params: CourseCatalogGetListInput): Promise<PagedResultDto<CourseCatalogDto>> {
    return firstValueFrom(this.proxy.getList(params));
  }

  create(body: CreateUpdateCourseCatalogDto): Promise<CourseCatalogDto> {
    return firstValueFrom(this.proxy.create(body));
  }

  update(id: string, body: CreateUpdateCourseCatalogDto): Promise<CourseCatalogDto> {
    return firstValueFrom(this.proxy.update(id, body));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.proxy.delete(id));
  }

  getConditions(catalogCourseId: string): Promise<CatalogEnrollmentConditionDto[]> {
    return firstValueFrom(this.proxy.getConditions(catalogCourseId));
  }

  addCondition(catalogCourseId: string, body: CreateUpdateCatalogEnrollmentConditionDto): Promise<CatalogEnrollmentConditionDto> {
    return firstValueFrom(this.proxy.addCondition(catalogCourseId, body));
  }

  removeCondition(conditionId: string): Promise<void> {
    return firstValueFrom(this.proxy.removeCondition(conditionId));
  }
}

/**
 * Wraps ABP-generated CourseFieldService proxy.
 *
 * Routes:
 *   GET    /api/app/course-field/{id}
 *   GET    /api/app/course-field
 *   POST   /api/app/course-field
 *   PUT    /api/app/course-field/{id}
 *   DELETE /api/app/course-field/{id}
 *   GET    /api/app/course-field/active
 */
@Injectable({ providedIn: 'root' })
export class CourseFieldService {
  private readonly proxy = inject(CourseFieldProxy);

  get(id: string): Promise<CourseFieldDto> {
    return firstValueFrom(this.proxy.get(id));
  }

  getList(params: PagedAndSortedResultRequestDto): Promise<PagedResultDto<CourseFieldDto>> {
    return firstValueFrom(this.proxy.getList(params));
  }

  create(body: CreateUpdateCourseFieldDto): Promise<CourseFieldDto> {
    return firstValueFrom(this.proxy.create(body));
  }

  update(id: string, body: CreateUpdateCourseFieldDto): Promise<CourseFieldDto> {
    return firstValueFrom(this.proxy.update(id, body));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.proxy.delete(id));
  }

  getAllActive(): Promise<CourseFieldDto[]> {
    return firstValueFrom(this.proxy.getAllActive());
  }
}

/**
 * Wraps ABP-generated CourseProposalService proxy.
 *
 * Routes:
 *   GET    /api/app/course-proposal/{id}
 *   GET    /api/app/course-proposal
 *   POST   /api/app/course-proposal
 *   PUT    /api/app/course-proposal/{id}/review
 */
@Injectable({ providedIn: 'root' })
export class CourseProposalService {
  private readonly proxy = inject(CourseProposalProxy);

  get(id: string): Promise<CourseProposalDto> {
    return firstValueFrom(this.proxy.get(id));
  }

  getList(params: CourseProposalGetListInput): Promise<PagedResultDto<CourseProposalDto>> {
    return firstValueFrom(this.proxy.getList(params));
  }

  create(body: CreateCourseProposalDto): Promise<CourseProposalDto> {
    return firstValueFrom(this.proxy.create(body));
  }

  review(id: string, body: ReviewCourseProposalDto): Promise<CourseProposalDto> {
    return firstValueFrom(this.proxy.review(id, body));
  }
}

/**
 * Wraps ABP-generated TenantCourseService proxy.
 *
 * Routes:
 *   GET    /api/app/tenant-course/{id}
 *   GET    /api/app/tenant-course
 *   POST   /api/app/tenant-course/from-catalog
 *   PUT    /api/app/tenant-course/{id}
 *   DELETE /api/app/tenant-course/{id}
 *   GET    /api/app/tenant-course/conditions/{tenantCourseId}
 *   GET    /api/app/tenant-course/available-catalog-courses
 */
@Injectable({ providedIn: 'root' })
export class TenantCourseService {
  private readonly proxy = inject(TenantCourseProxy);

  get(id: string): Promise<TenantCourseDto> {
    return firstValueFrom(this.proxy.get(id));
  }

  getList(params: TenantCourseGetListInput): Promise<PagedResultDto<TenantCourseDto>> {
    return firstValueFrom(this.proxy.getList(params));
  }

  addFromCatalog(body: AddFromCatalogDto): Promise<TenantCourseDto[]> {
    return firstValueFrom(this.proxy.addFromCatalog(body));
  }

  update(id: string, body: UpdateTenantCourseDto): Promise<TenantCourseDto> {
    return firstValueFrom(this.proxy.update(id, body));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.proxy.delete(id));
  }

  getConditions(tenantCourseId: string): Promise<TenantCourseConditionDto[]> {
    return firstValueFrom(this.proxy.getConditions(tenantCourseId));
  }

  getAvailableCatalogCourses(params: PagedAndSortedResultRequestDto): Promise<PagedResultDto<CourseCatalogDto>> {
    return firstValueFrom(this.proxy.getAvailableCatalogCourses(params));
  }
}
