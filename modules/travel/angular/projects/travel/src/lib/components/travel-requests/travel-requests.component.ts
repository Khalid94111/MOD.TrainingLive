import { Component, inject, OnInit, OnDestroy, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PagedResultDto, PagedAndSortedResultRequestDto, PermissionDirective } from '@abp/ng.core';
import { LocalizationModule } from '@abp/ng.core';
import { Confirmation, ConfirmationService } from '@abp/ng.theme.shared';
import {
  TravelRequestService,
  TravelRequestDto,
  GetTravelRequestListInput,
  CreateUpdateTravelRequestDto,
  AllowanceSummaryDto,
  TicketClass,
} from '../../services/travel-request.service';
import {
  TravelEmployeeLookupResult,
  TravelLookupService,
} from '../../services/travel-lookup.service';
import { RequestStatus, RequestStatusLabels } from '../../models/travel-types';
import { TravelTypeDefinitionDto, TravelTypeDefinitionService } from '../../services/travel-type-definition.service';
import { getStatusBadgeClass, getTicketClassLabel, travelLocalize } from '../../utils/travel-display.utils';
import { AllowanceRateService } from '../../services/allowance-rate.service';
import { TenantLookupDto, TenantLookupService } from '../../services/tenant-lookup.service';

// Extended employee type used in the create/edit modal
interface SelectedEmployee extends TravelEmployeeLookupResult {
  dailyAllowanceRate: number;
  ticketClass: TicketClass;
}

@Component({
  selector: 'lib-travel-requests',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationModule, PermissionDirective],
  templateUrl: './travel-requests.component.html',
  styleUrls: ['./travel-requests.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TravelRequestsComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();
  protected readonly service = inject(TravelRequestService);
  protected readonly lookupService = inject(TravelLookupService);
  protected readonly travelTypeService = inject(TravelTypeDefinitionService);
  protected readonly allowanceRateService = inject(AllowanceRateService);
  protected readonly tenantLookupService = inject(TenantLookupService);
  protected readonly confirmation = inject(ConfirmationService);
  protected readonly router = inject(Router);
  protected readonly Math = Math;
  protected readonly RequestStatus = RequestStatus;
  protected readonly TicketClass = TicketClass;

  // ─── Signals ─────────────────────────────────────────────
  readonly requests = signal<PagedResultDto<TravelRequestDto>>({ items: [], totalCount: 0 });
  readonly isLoading = signal<boolean>(false);
  readonly saving = signal<boolean>(false);
  readonly createModalVisible = signal<boolean>(false);
  readonly editingRequestId = signal<string | null>(null);
  readonly allowancePreview = signal<AllowanceSummaryDto | null>(null);
  readonly allowancePreviewLoading = signal<boolean>(false);
  readonly currentIntegrationWarnings = signal<string>('');

  readonly travelTypes = signal<TravelTypeDefinitionDto[]>([]);
  readonly tenants = signal<TenantLookupDto[]>([]);
  readonly employees = signal<TravelEmployeeLookupResult[]>([]);

  // Filters
  readonly searchText = signal<string>('');
  readonly filterType = signal<string | null>(null);
  readonly filterStatus = signal<RequestStatus | null>(null);
  readonly filterDepartment = signal<string>('');
  readonly filterStartDateFrom = signal<string>('');
  readonly filterStartDateTo = signal<string>('');
  readonly quickFilter = signal<'all' | 'needsAction' | 'travelOffice'>('all');

  // Pagination
  readonly listQuery = signal<PagedAndSortedResultRequestDto>({
    maxResultCount: 10,
    skipCount: 0,
    sorting: 'creationTime desc',
  });

  // ─── Computed ────────────────────────────────────────────
  readonly pageIndex = computed(() =>
    Math.floor(this.listQuery().skipCount / this.listQuery().maxResultCount) + 1
  );

  readonly totalPages = computed(() =>
    Math.ceil(this.requests().totalCount / this.listQuery().maxResultCount)
  );

  readonly pageNumbers = computed(() => {
    const total = this.totalPages();
    const current = this.pageIndex();
    const pages: number[] = [];

    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
      return pages;
    }

    pages.push(1);
    if (current > 3) pages.push(-1);

    const start = Math.max(2, current - 1);
    const end = Math.min(total - 1, current + 1);
    for (let i = start; i <= end; i++) pages.push(i);

    if (current < total - 2) pages.push(-1);
    pages.push(total);
    return pages;
  });

  readonly activeRequestsCount = computed(() =>
    this.requests().items.filter((r) => r.status !== RequestStatus.Completed && r.status !== RequestStatus.Cancelled).length
  );

  readonly completedRequestsCount = computed(() =>
    this.requests().items.filter((r) => r.status === RequestStatus.Completed).length
  );

  // ─── Properties (not signals - too complex for modal state) ───
  travelTypeOptions: Array<{ label: string; value: string }> = [];
  selectedEmployees: SelectedEmployee[] = [];
  employeeNumberDraft = '';
  createInput: CreateUpdateTravelRequestDto = this.getDefaultCreateInput();

  readonly categoryOptions = [
    { value: 1, label: this.l('AllowanceCategory.A') },
    { value: 2, label: this.l('AllowanceCategory.B') },
  ];

  readonly statusFilterOptions = [
    { value: RequestStatus.Draft, label: this.l('RequestStatus.Draft') },
    { value: RequestStatus.AtTravelOffice, label: this.l('RequestStatus.AtTravelOffice') },
    { value: RequestStatus.TicketsBooked, label: this.l('RequestStatus.TicketsBooked') },
    { value: RequestStatus.CalculatingAllowances, label: this.l('RequestStatus.CalculatingAllowances') },
    { value: RequestStatus.Completed, label: this.l('RequestStatus.Completed') },
  ];

  readonly timelineStages = [
    { status: RequestStatus.Draft },
    { status: RequestStatus.AtTravelOffice },
    { status: RequestStatus.TicketsBooked },
    { status: RequestStatus.CalculatingAllowances },
    { status: RequestStatus.Completed },
  ];

  get hasInvalidEmployeeAllowance(): boolean {
    return this.selectedEmployees.some((employee) => !employee.dailyAllowanceRate || employee.dailyAllowanceRate <= 0);
  }

  // ─── Lifecycle ───────────────────────────────────────────
  ngOnInit(): void {
    this.loadLookups();
    this.loadTravelTypes();
    this.loadRequests();
  }

  // ─── Data Loading ────────────────────────────────────────
  loadTravelTypes(): void {
    this.travelTypeService.getActiveTypes()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (items) => {
          const sorted = items.sort((a, b) => a.code - b.code);
          this.travelTypes.set(sorted);
          this.travelTypeOptions = sorted.map((type) => ({
            label: type.name,
            value: type.id,
          }));

          if (
            this.travelTypeOptions.length > 0 &&
            !this.travelTypeOptions.some((x) => x.value === this.createInput.travelTypeDefinitionId)
          ) {
            this.createInput = { ...this.createInput, travelTypeDefinitionId: this.travelTypeOptions[0].value };
          }
        },
        error: () => {},
      });
  }

  loadRequests(): void {
    this.isLoading.set(true);
    const finish = () => this.isLoading.set(false);

    const input: GetTravelRequestListInput = {
      ...this.listQuery(),
      travelTypeDefinitionId: this.filterType() ?? undefined,
      status: this.filterStatus() ?? undefined,
      statuses: this.filterStatus() === null ? this.getQuickFilterStatuses() : undefined,
      department: this.filterDepartment() || undefined,
      searchText: this.searchText() || undefined,
      startDateFrom: this.filterStartDateFrom() || undefined,
      startDateTo: this.filterStartDateTo() || undefined,
    };

    this.service.getList(input)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.requests.set(result);
          finish();
        },
        error: finish,
      });
  }

  loadLookups(): void {
    // الموظفون يُجلبون عند البحث بالرقم الوظيفي عبر addEmployeeByNumber
    this.tenantLookupService.getTenants()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (items) => this.tenants.set(items),
        error: () => {},
      });
  }

  getTenantLabel(tenant: TenantLookupDto): string {
    return tenant.arabicDescription || tenant.englishDescription || tenant.name;
  }

  // ─── Pagination ──────────────────────────────────────────
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.listQuery.update((q) => ({ ...q, skipCount: (page - 1) * q.maxResultCount }));
    this.loadRequests();
  }

  // ─── Filters ─────────────────────────────────────────────
  applyFilters(): void {
    this.listQuery.update((q) => ({ ...q, skipCount: 0 }));
    this.loadRequests();
  }

  resetFilters(): void {
    this.searchText.set('');
    this.filterType.set(null);
    this.filterStatus.set(null);
    this.filterDepartment.set('');
    this.filterStartDateFrom.set('');
    this.filterStartDateTo.set('');
    this.quickFilter.set('all');
    this.listQuery.update((q) => ({ ...q, skipCount: 0 }));
    this.loadRequests();
  }

  setQuickFilter(filter: 'all' | 'needsAction' | 'travelOffice'): void {
    this.quickFilter.set(filter);
    this.filterStatus.set(null);
    this.listQuery.update((q) => ({ ...q, skipCount: 0 }));
    this.loadRequests();
  }

  private getQuickFilterStatuses(): RequestStatus[] | undefined {
    if (this.quickFilter() === 'needsAction') {
      return [
        RequestStatus.AtTravelOffice,
        RequestStatus.TicketsBooked,
        RequestStatus.CalculatingAllowances,
      ];
    }
    if (this.quickFilter() === 'travelOffice') {
      return [
        RequestStatus.AtTravelOffice,
        RequestStatus.FlightSelection,
        RequestStatus.VisaCheck,
        RequestStatus.InsuranceCheck,
        RequestStatus.PendingApproval,
        RequestStatus.Approved,
      ];
    }
    return undefined;
  }

  // ─── Helpers ─────────────────────────────────────────────
  l(key: string): string {
    return travelLocalize(key);
  }

  getTypeLabel(travelTypeDefinitionId: string): string {
    return this.travelTypes().find((x) => x.id === travelTypeDefinitionId)?.name || travelTypeDefinitionId;
  }

  getStatusLabel(status: RequestStatus): string {
    return this.l(RequestStatusLabels[status] || String(status));
  }

  getStatusBadgeClass(status: RequestStatus): string {
    return getStatusBadgeClass(status);
  }

  getStatusAlertClass(status: RequestStatus): string {
    switch (status) {
      case RequestStatus.Completed:
        return 'success';
      case RequestStatus.Rejected:
      case RequestStatus.Cancelled:
        return 'danger';
      case RequestStatus.Returned:
        return 'warning';
      default:
        return 'secondary';
    }
  }

  getTimelineStageState(stageStatus: RequestStatus, currentStatus: RequestStatus): 'completed' | 'active' | 'pending' {
    const order = this.timelineStages.map((s) => s.status);
    const stageIndex = order.indexOf(stageStatus);
    const currentIndex = order.indexOf(currentStatus);
    if (stageIndex === -1 || currentIndex === -1) return 'pending';
    if (stageIndex < currentIndex) return 'completed';
    if (stageIndex === currentIndex) return 'active';
    return 'pending';
  }

  getTimelineProgress(currentStatus: RequestStatus): number {
    const order = this.timelineStages.map((s) => s.status);
    const currentIndex = order.indexOf(currentStatus);
    if (currentIndex === -1) return 0;
    return (currentIndex / (order.length - 1)) * 100;
  }

  getTicketClassLabel(ticketClass: TicketClass | number): string {
    return getTicketClassLabel(ticketClass, (k) => this.l(k));
  }

  // ─── Modal ───────────────────────────────────────────────
  openCreateModal(): void {
    this.editingRequestId.set(null);
    this.createInput = this.getDefaultCreateInput();
    this.selectedEmployees = [];
    this.allowancePreview.set(null);
    this.employeeNumberDraft = '';
    this.currentIntegrationWarnings.set('');
    this.createModalVisible.set(true);
  }

  closeCreateModal(): void {
    if (this.saving()) return;
    this.createModalVisible.set(false);
    this.editingRequestId.set(null);
    this.currentIntegrationWarnings.set('');
  }

  openDetailsModal(id: string): void {
    this.router.navigate(['travel', 'requests', id]);
  }

  openEditModal(id: string): void {
    this.service.get(id).pipe(takeUntil(this.destroy$)).subscribe((result) => {
      this.editingRequestId.set(result.id);
      this.currentIntegrationWarnings.set(result.integrationWarnings || '');
      this.createInput = this.mapRequestToInput(result);
      this.selectedEmployees = (result.employees || []).map((employee) => ({
        id: employee.employeeId,
        name: employee.employeeName,
        employeeNumber: employee.employeeNumber,
        department: result.department,
        rankId: employee.rankId,
        rankName: employee.rankName,
        category: result.category || 1,
        dailyAllowanceRate: employee.dailyAllowanceRate,
        ticketClass: employee.ticketClass || TicketClass.Economy,
      } as SelectedEmployee));
      this.employeeNumberDraft = '';
      this.createModalVisible.set(true);
      this.previewAllowances();
    });
  }

  // ─── CRUD ────────────────────────────────────────────────
  canEditRequest(request: TravelRequestDto): boolean {
    return request.status === RequestStatus.Draft || request.status === RequestStatus.Returned;
  }

  canDeleteRequest(request: TravelRequestDto): boolean {
    return request.status === RequestStatus.Draft || request.status === RequestStatus.Returned;
  }

  deleteRequest(request: TravelRequestDto): void {
    this.confirmation
      .warn(this.l('DeleteTravelRequestConfirmation'), this.l('AreYouSure'))
      .subscribe((status) => {
        if (status !== Confirmation.Status.confirm) return;
        this.service.delete(request.id).pipe(takeUntil(this.destroy$)).subscribe(() => this.loadRequests());
      });
  }

  saveRequest(submitForApproval: boolean): void {
    this.createInput = this.buildCreateInput();
    this.saving.set(true);

    const request$ = this.editingRequestId()
      ? this.service.update(this.editingRequestId()!, this.createInput)
      : this.service.create(this.createInput);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (created) => {
        if (submitForApproval && !this.editingRequestId()) {
          this.service.changeStatus(created.id, { newStatus: RequestStatus.AtTravelOffice })
            .pipe(takeUntil(this.destroy$))
            .subscribe({
              next: () => this.finishSavingRequest(),
              error: () => this.saving.set(false),
            });
          return;
        }
        this.finishSavingRequest();
      },
      error: () => this.saving.set(false),
    });
  }

  private finishSavingRequest(): void {
    this.saving.set(false);
    this.createModalVisible.set(false);
    this.editingRequestId.set(null);
    this.currentIntegrationWarnings.set('');
    this.loadRequests();
  }

  // ─── Allowance Preview ───────────────────────────────────
  previewAllowances(): void {
    if (this.selectedEmployees.length === 0) {
      this.allowancePreview.set(null);
      return;
    }
    this.allowancePreviewLoading.set(true);
    this.service.previewAllowances(this.buildCreateInput()).pipe(takeUntil(this.destroy$)).subscribe({
      next: (result) => {
        this.allowancePreview.set(result);
        this.allowancePreviewLoading.set(false);
      },
      error: () => this.allowancePreviewLoading.set(false),
    });
  }

  // ─── Employees ───────────────────────────────────────────
  isEmployeeSelected(id: string): boolean {
    return this.selectedEmployees.some((employee) => employee.id === id);
  }

  addEmployeeByNumber(): void {
    const employeeNumber = this.employeeNumberDraft.trim();
    if (!employeeNumber) return;

    this.lookupService.findEmployees([employeeNumber])
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (results) => {
          const employee = results?.[0];
          if (!employee || this.isEmployeeSelected(employee.id)) return;

          this.selectedEmployees = [
            ...this.selectedEmployees,
            {
              ...employee,
              dailyAllowanceRate: 0,
              category: this.createInput.category,
              ticketClass: TicketClass.Economy,
            } as SelectedEmployee,
          ];
          this.employeeNumberDraft = '';
          this.setEmployeeAllowanceRate(employee.id);
        },
        error: () => {},
      });
  }

  refreshSelectedEmployeeAllowanceRates(): void {
    this.selectedEmployees.forEach((employee) => this.setEmployeeAllowanceRate(employee.id));
  }

  private setEmployeeAllowanceRate(employeeId: string): void {
    const employee = this.selectedEmployees.find((item) => item.id === employeeId);
    if (!employee?.rankId) return;

    this.allowanceRateService
      .getList({
        rankId: employee.rankId,
        category: this.createInput.category,
        allowanceType: 1,
        maxResultCount: 10,
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          const rate = result.items.find((item) => item.isActive) || result.items[0];
          employee.dailyAllowanceRate = rate?.amount || 0;
          employee.ticketClass = rate?.ticketClass || TicketClass.Economy;
          employee.category = this.createInput.category;
          this.previewAllowances();
        },
        error: () => {},
      });
  }

  removeEmployee(id: string): void {
    this.selectedEmployees = this.selectedEmployees.filter((employee) => employee.id !== id);
    this.previewAllowances();
  }

  // ─── Mapping ─────────────────────────────────────────────
  private mapRequestToInput(request: TravelRequestDto): CreateUpdateTravelRequestDto {
    return {
      title: request.title,
      description: request.description,
      travelTypeDefinitionId: request.travelTypeDefinitionId,
      startDate: this.toDateInputValue(request.startDate),
      endDate: request.endDate ? this.toDateInputValue(request.endDate) : undefined,
      destinationCountry: request.destinationCountry,
      destinationCity: request.destinationCity,
      department: request.department,
      category: request.category || 1,
      dailyAllowanceRate: 0,
      currency: 'OMR',
      needsPermission: request.needsPermission,
      needsAwareness: request.needsAwareness,
      hasTicketCompensation: request.hasTicketCompensation,
      needsTransportation: request.needsTransportation,
      includesAccommodation: request.includesAccommodation,
      useHighestAllowance: request.useHighestAllowance,
      allowanceTiers: request.allowanceTiers || '',
      employeeIds: (request.employees || []).map((employee) => employee.employeeId),
      employees: (request.employees || []).map((employee) => ({
        employeeId: employee.employeeId,
        employeeName: employee.employeeName,
        employeeNumber: employee.employeeNumber,
        rankId: employee.rankId,
        rankName: employee.rankName,
        category: request.category || 1,
        dailyAllowanceRate: employee.dailyAllowanceRate,
        ticketClass: employee.ticketClass || TicketClass.Economy,
      })),
    };
  }

  private buildCreateInput(): CreateUpdateTravelRequestDto {
    return {
      ...this.createInput,
      employeeIds: this.selectedEmployees.map((employee) => employee.id),
      employees: this.selectedEmployees.map((employee) => ({
        employeeId: employee.id,
        employeeName: employee.name,
        employeeNumber: employee.employeeNumber,
        rankId: employee.rankId,
        rankName: employee.rankName,
        category: this.createInput.category,
        dailyAllowanceRate: employee.dailyAllowanceRate || 0,
        ticketClass: employee.ticketClass || TicketClass.Economy,
      })),
      allowanceTiers: '',
    };
  }

  private toDateInputValue(value: string): string {
    return value ? value.slice(0, 10) : new Date().toISOString().slice(0, 10);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private getDefaultCreateInput(): CreateUpdateTravelRequestDto {
    const today = new Date().toISOString().slice(0, 10);
    return {
      title: '',
      description: '',
      travelTypeDefinitionId: '',
      startDate: today,
      endDate: undefined,
      destinationCountry: '',
      destinationCity: '',
      department: '',
      category: 1,
      dailyAllowanceRate: 0,
      currency: 'OMR',
      needsPermission: false,
      needsAwareness: false,
      hasTicketCompensation: false,
      needsTransportation: false,
      includesAccommodation: false,
      useHighestAllowance: false,
      allowanceTiers: '',
      employeeIds: [],
      employees: [],
    };
  }
}
