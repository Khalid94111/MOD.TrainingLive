import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { PagedResultDto } from '@abp/ng.core';

// ABP generated proxies — run: abp generate-proxy -t ng
import { FinancialItemService as FinancialItemProxy } from '../../../proxy/training/finance/financial-item.service';
import { CourseTypeFinancialDefaultService as CourseTypeDefaultProxy } from '../../../proxy/training/finance/course-type-financial-default.service';
import { ExchangeRateService as ExchangeRateProxy } from '../../../proxy/training/finance/exchange-rate.service';
import { TrainingBudgetService as TrainingBudgetProxy } from '../../../proxy/training/finance/training-budget.service';

import type {
  FinancialItemDto,
  CreateUpdateFinancialItemDto,
  FinancialItemGetListInput,
} from '../../../proxy/training/finance/dtos';

import type {
  CourseTypeFinancialItemDefaultDto,
  CreateCourseTypeFinancialItemDefaultDto,
} from '../../../proxy/training/finance/dtos';

import type {
  ExchangeRateDto,
  CreateExchangeRateDto,
  ExchangeRateGetListInput,
} from '../../../proxy/training/finance/dtos';

import type {
  TrainingBudgetDto,
  TrainingBudgetGetListInput,
  UpdateAlertThresholdDto,
} from '../../../proxy/training/finance/dtos';
import { CourseType } from '../models/training-enums';

 
// ============================================================
// Financial Items Service
// ============================================================
@Injectable({ providedIn: 'root' })
export class FinancialItemService {
  private readonly proxy = inject(FinancialItemProxy);

  getList(params: FinancialItemGetListInput): Promise<PagedResultDto<FinancialItemDto>> {
    return firstValueFrom(this.proxy.getList(params));
  }

  get(id: string): Promise<FinancialItemDto> {
    return firstValueFrom(this.proxy.get(id));
  }

  create(input: CreateUpdateFinancialItemDto): Promise<FinancialItemDto> {
    return firstValueFrom(this.proxy.create(input));
  }

  update(id: string, input: CreateUpdateFinancialItemDto): Promise<FinancialItemDto> {
    return firstValueFrom(this.proxy.update(id, input));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.proxy.delete(id));
  }

  getSubItems(): Promise<FinancialItemDto[]> {
    return firstValueFrom(this.proxy.getSubItems());
  }
}

// ============================================================
// Course Type Financial Defaults Service
// ============================================================
@Injectable({ providedIn: 'root' })
export class CourseTypeFinancialDefaultService {
  private readonly proxy = inject(CourseTypeDefaultProxy);

  
  create(input: CreateCourseTypeFinancialItemDefaultDto): Promise<CourseTypeFinancialItemDefaultDto> {
    return firstValueFrom(this.proxy.create(input));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.proxy.delete(id));
  }
}

// ============================================================
// Exchange Rate Service
// ============================================================
@Injectable({ providedIn: 'root' })
export class ExchangeRateService {
  private readonly proxy = inject(ExchangeRateProxy);

  getList(params: ExchangeRateGetListInput): Promise<PagedResultDto<ExchangeRateDto>> {
    return firstValueFrom(this.proxy.getList(params));
  }

  getActive(): Promise<ExchangeRateDto> {
    return firstValueFrom(this.proxy.getActive());
  }

  create(input: CreateExchangeRateDto): Promise<ExchangeRateDto> {
    return firstValueFrom(this.proxy.create(input));
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.proxy.delete(id));
  }
}

// ============================================================
// Training Budget Service
// ============================================================
@Injectable({ providedIn: 'root' })
export class TrainingBudgetService {
  private readonly proxy = inject(TrainingBudgetProxy);

  getList(params: TrainingBudgetGetListInput): Promise<PagedResultDto<TrainingBudgetDto>> {
    return firstValueFrom(this.proxy.getList(params));
  }

  get(id: string): Promise<TrainingBudgetDto> {
    return firstValueFrom(this.proxy.get(id));
  }

  updateThreshold(id: string, input: UpdateAlertThresholdDto): Promise<TrainingBudgetDto> {
    return firstValueFrom(this.proxy.update(id, input));
  }
}
