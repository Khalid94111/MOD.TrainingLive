import { Component, inject, OnInit, OnDestroy, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LocalizationModule, LocalizationService, PermissionDirective } from '@abp/ng.core';
import { Confirmation, ConfirmationService } from '@abp/ng.theme.shared';
import {
  FlightDirection,
  TravelFlightOfferInputDto,
  TravelEmployeeDocumentInputDto,
  TravelRequestService,
  TravelRequestDto,
  TravelOfficeDetailsDto,
  TravelCostSummaryDto,
  TravelRequestAllowanceDetailDto,
  TravelDocumentDto,
  TicketClass,
  PayrollElementLookupDto,
  OverseasPayrollPreviewDto,
} from '../../services/travel-request.service';
import { RequestStatus, RequestStatusLabels } from '../../models/travel-types';
import { TravelTypeDefinitionDto, TravelTypeDefinitionService } from '../../services/travel-type-definition.service';
import { getStatusBadgeClass, getTicketClassLabel, getMimeType, travelLocalize } from '../../utils/travel-display.utils';

@Component({
  selector: 'lib-travel-request-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LocalizationModule, PermissionDirective],
  templateUrl: './travel-request-detail.component.html',
  styleUrls: ['./travel-request-detail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TravelRequestDetailComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();
  protected readonly route = inject(ActivatedRoute);
  protected readonly service = inject(TravelRequestService);
  protected readonly travelTypeService = inject(TravelTypeDefinitionService);
  protected readonly localizationService = inject(LocalizationService);
  protected readonly confirmation = inject(ConfirmationService);
  protected readonly RequestStatus = RequestStatus;
  protected readonly FlightDirection = FlightDirection;
  protected readonly TicketClass = TicketClass;

  // Signals
  readonly loading = signal<boolean>(false);
  readonly request = signal<TravelRequestDto | null>(null);
  readonly travelTypes = signal<TravelTypeDefinitionDto[]>([]);
  readonly costSummary = signal<TravelCostSummaryDto | null>(null);
  readonly showTicketBookingModal = signal<boolean>(false);
  readonly selectedAllowanceDetail = signal<TravelRequestAllowanceDetailDto | null>(null);
  readonly showPayrollElementModal = signal<boolean>(false);
  readonly payrollElements = signal<PayrollElementLookupDto[]>([]);
  readonly selectedPayrollElementId = signal<string | null>(null);
  readonly overseasPayrollPreview = signal<OverseasPayrollPreviewDto | null>(null);
  readonly loadingPayrollElements = signal<boolean>(false);

  // Properties
  readonly timelineStages = [
    { status: RequestStatus.Draft },
    { status: RequestStatus.AtTravelOffice },
    { status: RequestStatus.TicketsBooked },
    { status: RequestStatus.CalculatingAllowances },
    { status: RequestStatus.Completed },
  ];

  travelOfficeDetails: TravelOfficeDetailsDto = this.getDefaultTravelOfficeDetails();
  newFlightOffer: TravelFlightOfferInputDto = this.getDefaultFlightOfferInput(FlightDirection.Departure);
  selectedDepartureFlightOfferIds: Record<number, string> = {};
  selectedReturnFlightOfferIds: Record<number, string> = {};
  employeeDocumentInputs: TravelEmployeeDocumentInputDto[] = [];

  // Computed
  readonly allowanceDetails = computed(() => this.request()?.allowanceDetails || []);

  // ── Document helpers ──────────────────────────────────────────────────────

  getDocumentIcon(extension: string): string {
    const ext = (extension || '').toLowerCase().replace('.', '');
    const map: Record<string, string> = {
      pdf: 'fa-solid fa-file-pdf',
      doc: 'fa-solid fa-file-word',
      docx: 'fa-solid fa-file-word',
      xls: 'fa-solid fa-file-excel',
      xlsx: 'fa-solid fa-file-excel',
      png: 'fa-solid fa-file-image',
      jpg: 'fa-solid fa-file-image',
      jpeg: 'fa-solid fa-file-image',
      zip: 'fa-solid fa-file-zipper',
    };
    return map[ext] || 'fa-solid fa-file-lines';
  }

  viewDocument(doc: TravelDocumentDto): void {
    this.service.downloadFile(doc.blobName).pipe(takeUntil(this.destroy$)).subscribe({
      next: (blob) => {
        const mimeType = this.getMimeType(doc.fileExtension);
        const file = new Blob([blob], { type: mimeType });
        const url = URL.createObjectURL(file);
        window.open(url, '_blank');
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: () => {},
    });
  }

  downloadDocument(doc: TravelDocumentDto): void {
    this.service.downloadFile(doc.blobName).pipe(takeUntil(this.destroy$)).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = doc.name;
        anchor.click();
        setTimeout(() => URL.revokeObjectURL(url), 10_000);
      },
      error: () => {},
    });
  }

  private getMimeType(extension: string): string {
    return getMimeType(extension);
  }

  get showTravelOfficeSetupPanel(): boolean {
    const req = this.request();
    return !!req &&
      this.getTimelineStatus(req.status) === RequestStatus.AtTravelOffice &&
      (req.flightOffers || []).length === 0 &&
      !req.selectedDepartureFlightOfferId &&
      !req.selectedReturnFlightOfferId;
  }

  get showFlightSelectionPanel(): boolean {
    const req = this.request();
    return !!req &&
      this.getTimelineStatus(req.status) === RequestStatus.AtTravelOffice &&
      (req.flightOffers || []).length > 0 &&
      !req.selectedDepartureFlightOfferId &&
      !req.selectedReturnFlightOfferId;
  }

  get showTicketBookingPanel(): boolean {
    const req = this.request();
    return !!req &&
      this.getTimelineStatus(req.status) === RequestStatus.AtTravelOffice &&
      !!req.selectedDepartureFlightOfferId &&
      !!req.selectedReturnFlightOfferId;
  }

  get canSendFlightOffers(): boolean {
    return this.requiredTicketClasses.every((ticketClass) =>
      this.departureOfferInputs.some((offer) => offer.ticketClass === ticketClass) &&
      this.returnOfferInputs.some((offer) => offer.ticketClass === ticketClass)
    );
  }

  get canConfirmFlightSelection(): boolean {
    return this.requiredTicketClasses.every((ticketClass) =>
      !!this.selectedDepartureFlightOfferIds[ticketClass] &&
      !!this.selectedReturnFlightOfferIds[ticketClass] &&
      this.isSelectedFlightDateRangeValid(ticketClass)
    );
  }

  get canConfirmTicketBooking(): boolean {
    const req = this.request();
    if (!req || this.employeeDocumentInputs.length === 0) return false;

    const hasAllEmployees = (req.employees || [])
      .every((employee) => this.employeeDocumentInputs.some((document) => document.employeeId === employee.employeeId));
    if (!hasAllEmployees) return false;

    return this.employeeDocumentInputs.every((document) =>
      !!document.ticketNumber?.trim() &&
      !!document.pnr?.trim() &&
      (!!document.ticketFileName?.trim() || !!document.ticketBlobName?.trim()) &&
      (!req?.requiresVisa || !!document.visaFileName?.trim() || !!document.visaBlobName?.trim()) &&
      (!req?.requiresTravelInsurance || !!document.insuranceFileName?.trim() || !!document.insuranceBlobName?.trim())
    );
  }

  get travelOfficeTotal(): number {
    const req = this.request();
    const employeeCount = req?.employees?.length || 0;
    const visa = this.travelOfficeDetails.requiresVisa ? Number(this.travelOfficeDetails.visaCostPerEmployee || 0) : 0;
    const insurance = this.travelOfficeDetails.requiresTravelInsurance ? Number(this.travelOfficeDetails.travelInsuranceCostPerEmployee || 0) : 0;
    const ticket = this.selectedFlightTotal || 0;
    return employeeCount * (visa + insurance) + ticket;
  }

  get departureOfferInputs(): TravelFlightOfferInputDto[] {
    return this.travelOfficeDetails.flightOffers.filter((offer) => offer.direction === FlightDirection.Departure);
  }

  get returnOfferInputs(): TravelFlightOfferInputDto[] {
    return this.travelOfficeDetails.flightOffers.filter((offer) => offer.direction === FlightDirection.Return);
  }

  get departureOffers() {
    return (this.request()?.flightOffers || []).filter((offer) => offer.direction === FlightDirection.Departure);
  }

  get returnOffers() {
    return (this.request()?.flightOffers || []).filter((offer) => offer.direction === FlightDirection.Return);
  }

  get selectedDepartureOffers() {
    return this.departureOffers.filter((offer) => offer.isSelected);
  }

  get selectedReturnOffers() {
    return this.returnOffers.filter((offer) => offer.isSelected);
  }

  get requiredTicketClasses(): TicketClass[] {
    const req = this.request();
    const employees = req?.employees || [];
    const classes = Array.from(
      new Set(employees.map((employee) => employee.ticketClass || TicketClass.Economy))
    ).sort((left, right) => left - right);

    return classes.length ? classes : [TicketClass.Economy];
  }

  get selectedFlightTotal(): number {
    return this.requiredTicketClasses.reduce((total, ticketClass) => {
      const req = this.request();
      const employeeCount = (req?.employees || []).filter((employee) => (employee.ticketClass || TicketClass.Economy) === ticketClass).length || 1;
      const departure = this.departureOffers.find((offer) => offer.id === this.selectedDepartureFlightOfferIds[ticketClass]);
      const returnOffer = this.returnOffers.find((offer) => offer.id === this.selectedReturnFlightOfferIds[ticketClass]);
      return total + employeeCount * (Number(departure?.price || 0) + Number(returnOffer?.price || 0));
    }, 0);
  }

  get selectedDepartureOfferLabel(): string {
    const offers = this.departureOffers.filter((item) => item.isSelected);
    return offers.length
      ? offers.map((offer) => `${this.getTicketClassText(offer.ticketClass)}: ${offer.airline} ${offer.flightNumber} (${offer.price} OMR)`).join(' | ')
      : '-';
  }

  get selectedReturnOfferLabel(): string {
    const offers = this.returnOffers.filter((item) => item.isSelected);
    return offers.length
      ? offers.map((offer) => `${this.getTicketClassText(offer.ticketClass)}: ${offer.airline} ${offer.flightNumber} (${offer.price} OMR)`).join(' | ')
      : '-';
  }

  get employeeCount(): number {
    return this.request()?.employees?.length || 0;
  }

  get ticketTotal(): number {
    return this.employeeCount * Number(this.request()?.ticketCostPerEmployee || 0);
  }

  get visaTotal(): number {
    const req = this.request();
    return req?.requiresVisa ? this.employeeCount * Number(req.visaCostPerEmployee || 0) : 0;
  }

  get insuranceTotal(): number {
    const req = this.request();
    return req?.requiresTravelInsurance ? this.employeeCount * Number(req.travelInsuranceCostPerEmployee || 0) : 0;
  }

  get allowanceTotal(): number {
    return Number(this.request()?.allowanceSnapshot?.netTotal || 0);
  }

  get grandTravelCost(): number {
    return this.ticketTotal + this.visaTotal + this.insuranceTotal + this.allowanceTotal;
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadRequest(id);
    this.travelTypeService.getActiveTypes()
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (items) => this.travelTypes.set(items), error: () => {} });
  }

  loadRequest(id: string): void {
    this.loading.set(true);
    this.service.get(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.request.set(result);
          this.setTravelOfficeDetailsFromRequest(result);
          this.loadCostSummary(result.id);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  openAllowanceDetail(detail: TravelRequestAllowanceDetailDto): void {
    this.selectedAllowanceDetail.set(detail);
  }

  closeAllowanceDetail(): void {
    this.selectedAllowanceDetail.set(null);
  }

  submitRequest(): void {
    const req = this.request();
    if (!req) return;
    this.confirmation
      .warn(this.l('SubmitTravelRequestConfirmation'), this.l('AreYouSure'))
      .pipe(takeUntil(this.destroy$))
      .subscribe((status) => {
        if (status !== Confirmation.Status.confirm) return;
        this.service.changeStatus(req.id, { newStatus: RequestStatus.AtTravelOffice })
          .pipe(takeUntil(this.destroy$))
          .subscribe((result) => {
            this.request.set(result);
            this.loadCostSummary(result.id);
          });
      });
  }

  approveRequest(): void {
    const req = this.request();
    if (!req) return;
    this.confirmation
      .success(this.l('ApproveTravelRequestConfirmation'), this.l('AreYouSure'))
      .pipe(takeUntil(this.destroy$))
      .subscribe((status) => {
        if (status !== Confirmation.Status.confirm) return;
        this.service.changeStatus(req.id, { newStatus: RequestStatus.Approved })
          .pipe(takeUntil(this.destroy$))
          .subscribe((result) => {
            this.request.set(result);
            this.loadCostSummary(result.id);
          });
      });
  }

  returnRequest(): void {
    const req = this.request();
    if (!req) return;
    const reason = window.prompt(this.l('ReturnReason'));
    if (!reason?.trim()) return;
    this.service.changeStatus(req.id, { newStatus: RequestStatus.Returned, reason })
      .pipe(takeUntil(this.destroy$))
      .subscribe((result) => {
        this.request.set(result);
        this.loadCostSummary(result.id);
      });
  }

  rejectRequest(): void {
    const req = this.request();
    if (!req) return;
    const reason = window.prompt(this.l('RejectReason'));
    if (!reason?.trim()) return;
    this.service.changeStatus(req.id, { newStatus: RequestStatus.Rejected, reason })
      .pipe(takeUntil(this.destroy$))
      .subscribe((result) => {
        this.request.set(result);
        this.loadCostSummary(result.id);
      });
  }

  moveToNextWorkflowStep(): void {
    const req = this.request();
    if (!req) return;
    const nextStatus = this.getNextWorkflowStatus(req.status);
    if (!nextStatus) return;

    this.confirmation
      .info(this.getNextWorkflowConfirmation(req.status), this.l('AreYouSure'))
      .pipe(takeUntil(this.destroy$))
      .subscribe((status) => {
        if (status !== Confirmation.Status.confirm) return;

        if (req.status === RequestStatus.CalculatingAllowances) {
          this.service.getOverseasPayrollPreview(req.id)
            .pipe(takeUntil(this.destroy$))
            .subscribe((preview) => {
              if (preview.requiresPayrollElementSelection) {
                this.overseasPayrollPreview.set(preview);
                this.openPayrollElementModal();
                return;
              }

              this.completeRequestWithoutPayrollElement(req.id);
            });
          return;
        }

        this.service.changeStatus(req.id, { newStatus: nextStatus }).pipe(takeUntil(this.destroy$)).subscribe((result) => {
          this.request.set(result);
          this.loadCostSummary(result.id);
        });
      });
  }

  openPayrollElementModal(): void {
    this.selectedPayrollElementId.set(null);
    this.loadingPayrollElements.set(true);
    this.showPayrollElementModal.set(true);
    this.service.getPayrollElementLookup()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (elements) => {
          this.payrollElements.set(elements);
          this.loadingPayrollElements.set(false);
        },
        error: () => {
          this.payrollElements.set([]);
          this.loadingPayrollElements.set(false);
        },
      });
  }

  closePayrollElementModal(): void {
    this.showPayrollElementModal.set(false);
    this.selectedPayrollElementId.set(null);
    this.overseasPayrollPreview.set(null);
  }

  confirmPayrollElementAndComplete(): void {
    const req = this.request();
    const payrollElementId = this.selectedPayrollElementId();
    if (!req || !payrollElementId) return;

    this.service.calculateAllowances(req.id, true)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.service.changeStatus(req.id, {
          newStatus: RequestStatus.Completed,
          payrollElementId,
        })
          .pipe(takeUntil(this.destroy$))
          .subscribe((result) => {
            this.request.set(result);
            this.closePayrollElementModal();
            this.loadCostSummary(result.id);
          });
      });
  }

  private completeRequestWithoutPayrollElement(id: string): void {
    this.service.calculateAllowances(id, true)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.service.changeStatus(id, { newStatus: RequestStatus.Completed })
          .pipe(takeUntil(this.destroy$))
          .subscribe((result) => {
            this.request.set(result);
            this.loadCostSummary(result.id);
          });
      });
  }

  sendFlightOffersForSelection(): void {
    const req = this.request();
    if (!req) return;
    this.confirmation
      .info(this.l('TravelOfficeDetailsSavedConfirmation'), this.l('AreYouSure'))
      .pipe(takeUntil(this.destroy$))
      .subscribe((status) => {
        if (status !== Confirmation.Status.confirm) return;
        this.service.saveTravelOfficeDetails(req.id, this.travelOfficeDetails).pipe(takeUntil(this.destroy$)).subscribe((result) => {
          this.request.set(result);
          this.setTravelOfficeDetailsFromRequest(result);
          this.loadCostSummary(result.id);
        });
      });
  }

  selectFlightOffers(): void {
    const req = this.request();
    if (!req || !this.canConfirmFlightSelection) return;

    this.confirmation
      .info(this.l('ConfirmFlightSelectionConfirmation'), this.l('AreYouSure'))
      .pipe(takeUntil(this.destroy$))
      .subscribe((status) => {
        if (status !== Confirmation.Status.confirm) return;
        this.service.selectFlightOffers(req.id, {
          departureFlightOfferId: this.selectedDepartureFlightOfferIds[this.requiredTicketClasses[0]] || '',
          returnFlightOfferId: this.selectedReturnFlightOfferIds[this.requiredTicketClasses[0]] || '',
          selections: this.requiredTicketClasses.map((ticketClass) => ({
            ticketClass,
            departureFlightOfferId: this.selectedDepartureFlightOfferIds[ticketClass],
            returnFlightOfferId: this.selectedReturnFlightOfferIds[ticketClass],
          })),
        }).pipe(takeUntil(this.destroy$)).subscribe((result) => {
          this.request.set(result);
          this.setTravelOfficeDetailsFromRequest(result);
          this.loadCostSummary(result.id);
        });
      });
  }

  // Common file toggles & state
  useCommonTicketFile = false;
  useCommonVisaFile = false;
  useCommonInsuranceFile = false;
  commonTicketFile: { fileName: string; blobName: string } | null = null;
  commonVisaFile: { fileName: string; blobName: string } | null = null;
  commonInsuranceFile: { fileName: string; blobName: string } | null = null;
  uploadingFiles: Record<string, boolean> = {};

  openTicketBookingModal(): void {
    const req = this.request();
    if (!req) return;
    this.useCommonTicketFile = false;
    this.useCommonVisaFile = false;
    this.useCommonInsuranceFile = false;
    this.commonTicketFile = null;
    this.commonVisaFile = null;
    this.commonInsuranceFile = null;
    this.employeeDocumentInputs = (req.employees || []).map((employee) => {
      const existing = (req.employeeDocuments || []).find((document) => document.employeeId === employee.employeeId);
      return {
        employeeId: employee.employeeId,
        employeeName: employee.employeeName,
        employeeNumber: employee.employeeNumber,
        ticketNumber: existing?.ticketNumber || '',
        pnr: existing?.pnr || '',
        ticketFileName: existing?.ticketFileName || '',
        ticketBlobName: existing?.ticketBlobName || '',
        visaFileName: existing?.visaFileName || '',
        visaBlobName: existing?.visaBlobName || '',
        insuranceFileName: existing?.insuranceFileName || '',
        insuranceBlobName: existing?.insuranceBlobName || '',
      };
    });
    this.showTicketBookingModal.set(true);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onCommonFileSelected(event: Event, type: 'ticket' | 'visa' | 'insurance'): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    const key = `common-${type}`;
    this.uploadingFiles[key] = true;
    this.service.uploadFile(file).pipe(takeUntil(this.destroy$)).subscribe({
      next: (result) => {
        const fileInfo = { fileName: result.fileName, blobName: result.blobName };
        if (type === 'ticket') {
          this.commonTicketFile = fileInfo;
          if (this.useCommonTicketFile) this.applyCommonFileToAll(type);
        } else if (type === 'visa') {
          this.commonVisaFile = fileInfo;
          if (this.useCommonVisaFile) this.applyCommonFileToAll(type);
        } else {
          this.commonInsuranceFile = fileInfo;
          if (this.useCommonInsuranceFile) this.applyCommonFileToAll(type);
        }
        this.uploadingFiles[key] = false;
      },
      error: () => { this.uploadingFiles[key] = false; },
    });
  }

  onEmployeeFileSelected(event: Event, type: 'ticket' | 'visa' | 'insurance', employeeId: string): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    const key = `${employeeId}-${type}`;
    this.uploadingFiles[key] = true;
    this.service.uploadFile(file).pipe(takeUntil(this.destroy$)).subscribe({
      next: (result) => {
        const doc = this.employeeDocumentInputs.find((d) => d.employeeId === employeeId);
        if (!doc) return;
        if (type === 'ticket') {
          doc.ticketFileName = result.fileName;
          doc.ticketBlobName = result.blobName;
        } else if (type === 'visa') {
          doc.visaFileName = result.fileName;
          doc.visaBlobName = result.blobName;
        } else {
          doc.insuranceFileName = result.fileName;
          doc.insuranceBlobName = result.blobName;
        }
        this.uploadingFiles[key] = false;
      },
      error: () => { this.uploadingFiles[key] = false; },
    });
  }

  applyCommonFileToAll(type: 'ticket' | 'visa' | 'insurance'): void {
    const common = type === 'ticket' ? this.commonTicketFile : type === 'visa' ? this.commonVisaFile : this.commonInsuranceFile;
    if (!common) return;
    this.employeeDocumentInputs.forEach((doc) => {
      if (type === 'ticket') {
        doc.ticketFileName = common.fileName;
        doc.ticketBlobName = common.blobName;
      } else if (type === 'visa') {
        doc.visaFileName = common.fileName;
        doc.visaBlobName = common.blobName;
      } else {
        doc.insuranceFileName = common.fileName;
        doc.insuranceBlobName = common.blobName;
      }
    });
  }

  clearEmployeeFile(type: 'ticket' | 'visa' | 'insurance', employeeId: string): void {
    const doc = this.employeeDocumentInputs.find((d) => d.employeeId === employeeId);
    if (!doc) return;
    if (type === 'ticket') { doc.ticketFileName = ''; doc.ticketBlobName = ''; }
    else if (type === 'visa') { doc.visaFileName = ''; doc.visaBlobName = ''; }
    else { doc.insuranceFileName = ''; doc.insuranceBlobName = ''; }
  }

  getFileUploadKey(type: 'ticket' | 'visa' | 'insurance', employeeId?: string): string {
    return employeeId ? `${employeeId}-${type}` : `common-${type}`;
  }

  hasEmployeeFile(document: TravelEmployeeDocumentInputDto, type: 'ticket' | 'visa' | 'insurance'): boolean {
    if (type === 'ticket') return !!document.ticketBlobName;
    if (type === 'visa') return !!document.visaBlobName;
    return !!document.insuranceBlobName;
  }

  viewFile(blobName: string, fileName: string): void {
    if (!blobName) return;
    this.service.downloadFile(blobName).pipe(takeUntil(this.destroy$)).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        window.open(url, '_blank');
        setTimeout(() => window.URL.revokeObjectURL(url), 60000);
      },
      error: () => {
        window.alert(this.l('FileViewError'));
      },
    });
  }

  downloadFile(blobName: string, fileName: string): void {
    if (!blobName) return;
    this.service.downloadFile(blobName).pipe(takeUntil(this.destroy$)).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        setTimeout(() => window.URL.revokeObjectURL(url), 60000);
      },
      error: () => {
        window.alert(this.l('FileDownloadError'));
      },
    });
  }

  confirmTicketBooking(): void {
    const req = this.request();
    if (!req) return;
    this.confirmation
      .success(this.l('ConfirmTicketBookingConfirmation'), this.l('AreYouSure'))
      .pipe(takeUntil(this.destroy$))
      .subscribe((status) => {
        if (status !== Confirmation.Status.confirm) return;
        this.service.confirmTicketBooking(req.id, { employeeDocuments: this.employeeDocumentInputs })
          .pipe(takeUntil(this.destroy$))
          .subscribe((result) => {
            this.request.set(result);
            this.showTicketBookingModal.set(false);
            this.setTravelOfficeDetailsFromRequest(result);
            this.loadCostSummary(result.id);
          });
      });
  }

  addFlightOffer(): void {
    if (!this.newFlightOffer.airline.trim() || !this.newFlightOffer.flightNumber.trim()) return;
    if (!this.isFlightOfferDateRangeValid(this.newFlightOffer)) {
      window.alert(this.l('InvalidTicketTravelDates'));
      return;
    }
    const offer = { ...this.newFlightOffer, currency: 'OMR' };
    offer.duration = this.calculateFlightDuration(offer.departureTime, offer.arrivalTime);
    this.travelOfficeDetails.flightOffers = [
      ...this.travelOfficeDetails.flightOffers,
      offer,
    ];
    this.newFlightOffer = this.getDefaultFlightOfferInput(this.newFlightOffer.direction);
  }

  removeFlightOffer(offer: TravelFlightOfferInputDto): void {
    this.travelOfficeDetails.flightOffers = this.travelOfficeDetails.flightOffers.filter((item) => item !== offer);
  }

  onNewFlightOfferDirectionChange(direction: FlightDirection): void {
    const current = this.newFlightOffer;
    this.newFlightOffer = {
      ...this.getDefaultFlightOfferInput(direction),
      ticketClass: current.ticketClass,
      airline: current.airline,
      flightNumber: current.flightNumber,
      price: current.price,
      currency: current.currency,
      duration: current.duration,
    };
  }

  getDepartureOffers(ticketClass: TicketClass | number) {
    return this.departureOffers.filter((offer) => offer.ticketClass === ticketClass);
  }

  getReturnOffers(ticketClass: TicketClass | number) {
    return this.returnOffers.filter((offer) => offer.ticketClass === ticketClass);
  }

  // Helpers
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

  getTicketClassLabel(ticketClass: TicketClass | number): string {
    return getTicketClassLabel(ticketClass, (k) => this.l(k));
  }

  getTicketClassText(ticketClass: TicketClass | number): string {
    return this.getTicketClassLabel(ticketClass);
  }

  getDurationDays(req: TravelRequestDto): number {
    const start = this.toDateOnly(req.ticketDepartureTime || req.startDate);
    const end = this.toDateOnly(req.ticketArrivalTime || req.endDate);
    const diff = start && end ? Math.floor((end.getTime() - start.getTime()) / 86400000) + 1 : 0;
    return diff > 0 ? diff : 0;
  }

  calculateFlightDuration(departureTime: string, arrivalTime: string): string {
    const dep = new Date(departureTime);
    const arr = new Date(arrivalTime);
    if (isNaN(dep.getTime()) || isNaN(arr.getTime()) || arr <= dep) return '-';
    const diffMs = arr.getTime() - dep.getTime();
    const hours = Math.floor(diffMs / 3600000);
    const minutes = Math.floor((diffMs % 3600000) / 60000);
    if (hours === 0) return `${minutes}m`;
    if (minutes === 0) return `${hours}h`;
    return `${hours}h ${minutes}m`;
  }

  private toDateOnly(value?: string): Date | null {
    if (!value) return null;
    const datePart = value.slice(0, 10);
    const parts = datePart.split('-').map(Number);
    if (parts.length !== 3 || parts.some(Number.isNaN)) return null;
    return new Date(parts[0], parts[1] - 1, parts[2]);
  }

  getTimelineStageState(stageStatus: RequestStatus, currentStatus: RequestStatus): 'completed' | 'active' | 'pending' {
    const order = this.timelineStages.map((s) => s.status);
    const stageIndex = order.indexOf(stageStatus);
    const currentIndex = order.indexOf(this.getTimelineStatus(currentStatus));
    if (stageIndex === -1 || currentIndex === -1) return 'pending';
    if (stageIndex < currentIndex) return 'completed';
    if (stageIndex === currentIndex) return 'active';
    return 'pending';
  }

  getTimelineProgress(currentStatus: RequestStatus): number {
    const order = this.timelineStages.map((s) => s.status);
    const currentIndex = order.indexOf(this.getTimelineStatus(currentStatus));
    if (currentIndex === -1) return 0;
    return (currentIndex / (order.length - 1)) * 100;
  }

  private getTimelineStatus(status: RequestStatus): RequestStatus {
    if ([RequestStatus.PendingApproval, RequestStatus.Approved, RequestStatus.VisaCheck, RequestStatus.InsuranceCheck, RequestStatus.FlightSelection].includes(status)) {
      return RequestStatus.AtTravelOffice;
    }
    if ([RequestStatus.DocsUploaded, RequestStatus.PendingEmployeeConfirm, RequestStatus.Confirmed].includes(status)) {
      return RequestStatus.TicketsBooked;
    }
    return status;
  }

  getNextWorkflowStatus(status: RequestStatus): RequestStatus | null {
    switch (status) {
      case RequestStatus.PendingApproval:
      case RequestStatus.Approved:
      case RequestStatus.VisaCheck:
      case RequestStatus.InsuranceCheck:
      case RequestStatus.FlightSelection:
        return RequestStatus.AtTravelOffice;
      case RequestStatus.TicketsBooked:
      case RequestStatus.DocsUploaded:
      case RequestStatus.PendingEmployeeConfirm:
      case RequestStatus.Confirmed:
        return RequestStatus.CalculatingAllowances;
      case RequestStatus.CalculatingAllowances:
        return RequestStatus.Completed;
      default:
        return null;
    }
  }

  getNextWorkflowLabel(status: RequestStatus): string {
    switch (status) {
      case RequestStatus.PendingApproval:
      case RequestStatus.Approved:
      case RequestStatus.VisaCheck:
      case RequestStatus.InsuranceCheck:
      case RequestStatus.FlightSelection:
        return this.l('SendToTravelOffice');
      case RequestStatus.TicketsBooked:
      case RequestStatus.DocsUploaded:
      case RequestStatus.PendingEmployeeConfirm:
      case RequestStatus.Confirmed:
        return this.l('StartAllowanceCalculation');
      case RequestStatus.CalculatingAllowances:
        return this.l('CompleteRequest');
      default:
        return '';
    }
  }

  getNextWorkflowConfirmation(status: RequestStatus): string {
    switch (status) {
      case RequestStatus.PendingApproval:
      case RequestStatus.Approved:
      case RequestStatus.VisaCheck:
      case RequestStatus.InsuranceCheck:
      case RequestStatus.FlightSelection:
        return this.l('SendToTravelOfficeConfirmation');
      case RequestStatus.TicketsBooked:
      case RequestStatus.DocsUploaded:
      case RequestStatus.PendingEmployeeConfirm:
      case RequestStatus.Confirmed:
        return this.l('StartAllowanceCalculationConfirmation');
      case RequestStatus.CalculatingAllowances:
        return this.l('CompleteRequestConfirmation');
      default:
        return this.l('AreYouSure');
    }
  }

  private loadCostSummary(id: string): void {
    this.service.getCostSummary(id).pipe(takeUntil(this.destroy$)).subscribe((summary) => this.costSummary.set(summary));
  }

  private setTravelOfficeDetailsFromRequest(result: TravelRequestDto): void {
    const now = new Date();
    const later = new Date(now.getTime() + 2 * 60 * 60 * 1000);
    this.travelOfficeDetails = {
      requiresVisa: result.requiresVisa ?? false,
      visaCostPerEmployee: result.visaCostPerEmployee ?? 0,
      requiresTravelInsurance: result.requiresTravelInsurance ?? false,
      travelInsuranceCostPerEmployee: result.travelInsuranceCostPerEmployee ?? 0,
      ticket: {
        airline: '',
        flightNumber: '',
        departureTime: now.toISOString().slice(0, 16),
        arrivalTime: later.toISOString().slice(0, 16),
        price: 0,
        currency: 'OMR',
      },
      flightOffers: [],
      travelOfficeNotes: result.travelOfficeNotes || '',
    };
  }

  private getDefaultTravelOfficeDetails(): TravelOfficeDetailsDto {
    const now = new Date();
    const later = new Date(now.getTime() + 2 * 60 * 60 * 1000);
    return {
      requiresVisa: false,
      visaCostPerEmployee: 0,
      requiresTravelInsurance: false,
      travelInsuranceCostPerEmployee: 0,
      ticket: {
        airline: '',
        flightNumber: '',
        departureTime: now.toISOString().slice(0, 16),
        arrivalTime: later.toISOString().slice(0, 16),
        price: 0,
        currency: 'OMR',
      },
      flightOffers: [],
      travelOfficeNotes: '',
    };
  }

  private getDefaultFlightOfferInput(direction: FlightDirection): TravelFlightOfferInputDto {
    const req = this.request();
    const base = direction === FlightDirection.Return
      ? this.toDateOnly(req?.endDate)
      : this.toDateOnly(req?.startDate);
    const departure = base || new Date();
    departure.setHours(direction === FlightDirection.Return ? 18 : 8, 0, 0, 0);
    const arrival = new Date(departure.getTime() + 2 * 60 * 60 * 1000);

    return {
      direction,
      ticketClass: this.requiredTicketClasses[0] ?? TicketClass.Economy,
      airline: '',
      flightNumber: '',
      departureTime: departure.toISOString().slice(0, 16),
      arrivalTime: arrival.toISOString().slice(0, 16),
      price: 0,
      currency: 'OMR',
      duration: this.calculateFlightDuration(departure.toISOString().slice(0, 16), arrival.toISOString().slice(0, 16)),
    };
  }

  // ── Clothing Note Formatter ─────────────────────────────────────────────
  // Parses structured note keys returned by the backend and translates them.
  // Format: "EmployeeName: KEY:arg1:arg2"
  formatClothingNote(rawNote: string): string {
    if (!rawNote) return '';
    return rawNote
      .split('; ')
      .map(part => {
        const colonIdx = part.indexOf(': ');
        if (colonIdx === -1) return part;
        const name = part.substring(0, colonIdx);
        const payload = part.substring(colonIdx + 2);
        const segments = payload.split(':');
        const key = segments[0];
        switch (key) {
          case 'NotEligible':
            return `${name}: ${this.localizationService.instant('Travel::NotEligible', segments[1] ?? '0')}`;
          case 'FirstTimeFullPayment':
            return `${name}: ${this.localizationService.instant('Travel::FirstTimeFullPayment', segments[1] ?? '')}`;
          case 'FullPayment':
            return `${name}: ${this.localizationService.instant('Travel::ClothingFullPayment', segments[1] ?? '')}`;
          case 'PartialPayment':
            return `${name}: ${this.localizationService.instant('Travel::ClothingPartialPayment', segments[1] ?? '', segments[2] ?? '')}`;
          case 'NoRankAssigned':
            return `${name}: ${this.localizationService.instant('Travel::NoRankAssigned')}`;
          case 'NoClothingRuleConfigured':
            return `${name}: ${this.localizationService.instant('Travel::NoClothingRuleConfigured')}`;
          default:
            return part;
        }
      })
      .join('\n');
  }

  private isFlightOfferDateRangeValid(offer: TravelFlightOfferInputDto): boolean {
    const req = this.request();
    if (!req) return true;
    const departure = new Date(offer.departureTime);
    const arrival = new Date(offer.arrivalTime);
    const startDate = this.toDateOnly(req.startDate);
    const endDate = req.endDate ? this.toDateOnly(req.endDate) : null;

    if (offer.direction === FlightDirection.Departure) {
      // Departure flight must be ON or BEFORE the course start date
      if (startDate && departure > startDate) return false;
    } else {
      // Return flight must be ON or AFTER the course end date
      if (endDate && departure < endDate) return false;
    }
    return arrival >= departure;
  }

  private isSelectedFlightDateRangeValid(ticketClass: TicketClass): boolean {
    const departureOffer = this.departureOffers.find((o) => o.id === this.selectedDepartureFlightOfferIds[ticketClass]);
    const returnOffer = this.returnOffers.find((o) => o.id === this.selectedReturnFlightOfferIds[ticketClass]);
    if (!departureOffer || !returnOffer) return false;
    const depDate = new Date(departureOffer.departureTime);
    const retDate = new Date(returnOffer.departureTime);
    return retDate > depDate;
  }
}
