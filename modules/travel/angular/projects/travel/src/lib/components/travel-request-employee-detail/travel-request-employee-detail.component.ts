import { Component, inject, OnInit, OnDestroy, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { LocalizationModule } from '@abp/ng.core';
import {
  FlightDirection,
  TravelEmployeeDocumentDto,
  TravelFlightOfferDto,
  TravelRequestAllowanceDetailDto,
  TravelRequestDto,
  TravelRequestEmployeeDto,
  TravelRequestService,
  TicketClass,
} from '../../services/travel-request.service';
import { TravelEmployeeLookupResult, TravelLookupService } from '../../services/travel-lookup.service';
import { RequestStatus, RequestStatusLabels } from '../../models/travel-types';
import { getStatusBadgeClass, getTicketClassLabel, getMimeType, travelLocalize } from '../../utils/travel-display.utils';
import { TravelTypeDefinitionDto, TravelTypeDefinitionService } from '../../services/travel-type-definition.service';

@Component({
  selector: 'lib-travel-request-employee-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, LocalizationModule],
  templateUrl: './travel-request-employee-detail.component.html',
  styleUrls: ['./travel-request-employee-detail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TravelRequestEmployeeDetailComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();
  private readonly route = inject(ActivatedRoute);
  private readonly requestService = inject(TravelRequestService);
  private readonly lookupService = inject(TravelLookupService);
  private readonly travelTypeService = inject(TravelTypeDefinitionService);

  readonly loading = signal<boolean>(false);
  readonly request = signal<TravelRequestDto | null>(null);
  readonly travelTypes = signal<TravelTypeDefinitionDto[]>([]);
  readonly employee = signal<TravelRequestEmployeeDto | null>(null);
  readonly lookupEmployee = signal<TravelEmployeeLookupResult | null>(null);

  readonly allowanceDetail = computed(() => {
    const req = this.request();
    const emp = this.employee();
    if (!req || !emp) return null;
    return (req.allowanceDetails || []).find((item) => item.employeeId === emp.employeeId) || null;
  });

  readonly employeeDocument = computed(() => {
    const req = this.request();
    const emp = this.employee();
    if (!req || !emp) return null;
    return (req.employeeDocuments || []).find((item) => item.employeeId === emp.employeeId) || null;
  });

  readonly selectedDepartureOffer = computed(() => this.getSelectedOffer(FlightDirection.Departure));
  readonly selectedReturnOffer = computed(() => this.getSelectedOffer(FlightDirection.Return));

  readonly durationDays = computed(() => {
    const req = this.request();
    if (!req?.startDate || !req?.endDate) return 0;
    const start = this.toDateOnly(req.ticketDepartureTime || req.startDate);
    const end = this.toDateOnly(req.ticketArrivalTime || req.endDate);
    const diff = start && end ? Math.floor((end.getTime() - start.getTime()) / 86400000) + 1 : 0;
    return diff > 0 ? diff : 0;
  });

  readonly ticketCost = computed(() => Number(this.request()?.ticketCostPerEmployee || 0));

  readonly visaCost = computed(() => {
    const req = this.request();
    return req?.requiresVisa ? Number(req.visaCostPerEmployee || 0) : 0;
  });

  readonly insuranceCost = computed(() => {
    const req = this.request();
    return req?.requiresTravelInsurance ? Number(req.travelInsuranceCostPerEmployee || 0) : 0;
  });

  readonly employeeGrandTotal = computed(() => {
    return this.ticketCost() + this.visaCost() + this.insuranceCost() + Number(this.allowanceDetail()?.netTotal || 0);
  });

  ngOnInit(): void {
    this.travelTypeService.getActiveTypes()
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (items) => this.travelTypes.set(items), error: () => {} });

    const requestId = this.route.snapshot.paramMap.get('id');
    const employeeId = this.route.snapshot.paramMap.get('employeeId');
    if (!requestId || !employeeId) return;

    this.loading.set(true);
    this.requestService.get(requestId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (req) => {
          this.request.set(req);
          this.employee.set((req.employees || []).find((item) => item.employeeId === employeeId) || null);
          this.loadLookupEmployee(employeeId);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  l(key: string): string {
    return travelLocalize(key);
  }

  getTypeLabel(req: TravelRequestDto): string {
    return this.travelTypes().find((x) => x.id === req.travelTypeDefinitionId)?.name || req.travelTypeDefinitionId;
  }

  getStatusLabel(req: TravelRequestDto): string {
    const key = RequestStatusLabels[req.status];
    return key ? this.l(key) : 'Travel::RequestStatus';
  }

  getStatusBadgeClass(status: RequestStatus): string {
    return getStatusBadgeClass(status);
  }

  getFlightOfferLabel(offer: TravelFlightOfferDto | null): string {
    if (!offer) return '';
    return `${offer.airline} ${offer.flightNumber} | ${new Date(offer.departureTime).toLocaleString()} - ${new Date(offer.arrivalTime).toLocaleString()} | ${offer.price} OMR`;
  }

  getTicketClassLabel(ticketClass: TicketClass | number): string {
    return getTicketClassLabel(ticketClass, (k) => this.l(k));
  }

  viewFile(blobName: string, fileName: string): void {
    if (!blobName) return;
    this.requestService.downloadFile(blobName)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (blob) => {
          const ext = (fileName || blobName).split('.').pop()?.toLowerCase() || '';
          const mime = this.getMimeType(ext);
          const typedBlob = new Blob([blob], { type: mime });
          const url = URL.createObjectURL(typedBlob);
          window.open(url, '_blank');
          setTimeout(() => URL.revokeObjectURL(url), 10_000);
        },
        error: () => window.alert(this.l('FileDownloadError')),
      });
  }

  downloadFile(blobName: string, fileName: string): void {
    if (!blobName) return;
    this.requestService.downloadFile(blobName)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = fileName || blobName;
          a.style.display = 'none';
          document.body.appendChild(a);
          a.click();
          document.body.removeChild(a);
          URL.revokeObjectURL(url);
        },
        error: () => window.alert(this.l('FileDownloadError')),
      });
  }

  private getMimeType(ext: string): string {
    return getMimeType(ext);
  }

  hasAttachments(doc: TravelEmployeeDocumentDto | null): boolean {
    if (!doc) return false;
    return !!(doc.ticketBlobName || doc.visaBlobName || doc.insuranceBlobName);
  }

  private loadLookupEmployee(employeeId: string): void {
    const empNumber = this.employee()?.employeeNumber;
    if (!empNumber) return;

    this.lookupService.findEmployees([empNumber])
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (results) => {
          this.lookupEmployee.set(
            results?.find((e) => e.id === employeeId || e.employeeNumber === empNumber) || null
          );
        },
        error: () => this.lookupEmployee.set(null),
      });
  }

  private getSelectedOffer(direction: FlightDirection): TravelFlightOfferDto | null {
    const req = this.request();
    if (!req) return null;
    const ticketClass = this.employee()?.ticketClass || TicketClass.Economy;
    return (
      (req.flightOffers || []).find(
        (offer) => offer.isSelected && offer.direction === direction && (offer.ticketClass || TicketClass.Economy) === ticketClass
      ) || null
    );
  }

  private toDateOnly(value?: string): Date | null {
    if (!value) return null;
    const datePart = value.slice(0, 10);
    const parts = datePart.split('-').map(Number);
    if (parts.length !== 3 || parts.some(Number.isNaN)) return null;
    return new Date(parts[0], parts[1] - 1, parts[2]);
  }
}
