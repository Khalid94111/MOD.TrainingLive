import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { RestService, PagedResultDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import { RequestStatus } from '../models/travel-types';

export enum FlightDirection {
  Departure = 1,
  Return = 2,
}

export enum TicketClass {
  Economy = 1,
  Business = 2,
}

export interface TravelRequestEmployeeDto {
  travelRequestId: string;
  employeeId: string;
  employeeName: string;
  employeeNumber: string;
  rankId?: string;
  rankName: string;
  category?: number;
  dailyAllowanceRate: number;
  ticketClass: TicketClass;
}

export interface TravelDocumentDto {
  travelRequestId: string;
  name: string;
  fileExtension: string;
  fileSize: number;
  blobName: string;
}

export interface TravelFlightOfferDto {
  id: string;
  travelRequestId: string;
  direction: FlightDirection;
  ticketClass: TicketClass;
  airline: string;
  flightNumber: string;
  departureTime: string;
  arrivalTime: string;
  price: number;
  currency: string;
  duration: string;
  isSelected: boolean;
}

export interface TravelEmployeeDocumentDto {
  id: string;
  travelRequestId: string;
  employeeId: string;
  employeeName: string;
  employeeNumber: string;
  ticketNumber: string;
  pnr: string;
  ticketFileName: string;
  ticketBlobName: string;
  visaFileName: string;
  visaBlobName: string;
  insuranceFileName: string;
  insuranceBlobName: string;
}

export interface AllowanceSnapshotDto {
  overseasTotal: number;
  clothingTotal: number;
  deductionAmount: number;
  netTotal: number;
  calculatedAt: string;
  warnings: string[];
}

export interface AllowanceCalculationSegmentDto {
  fromDay: number;
  toDay: number;
  days: number;
  percentage: number;
  amount: number;
}

export interface TravelRequestAllowanceDetailDto {
  travelRequestId: string;
  employeeId: string;
  employeeName: string;
  employeeNumber: string;
  rankName: string;
  category: number;
  dailyRate: number;
  calculatedDays: number;
  accommodationPaymentPercentage: number;
  overseasTotal: number;
  clothingAmount: number;
  deductionAmount: number;
  netTotal: number;
  hasMatchingRule: boolean;
  segments: AllowanceCalculationSegmentDto[];
  clothingCalculationNote: string;
  calculatedAt: string;
}

export interface TravelRequestDto {
  id: string;
  title: string;
  description: string;
  travelTypeDefinitionId: string;
  status: RequestStatus;
  startDate: string;
  endDate?: string;
  destinationCountry: string;
  destinationCity: string;
  department: string;
  category: number;
  dailyAllowanceRate: number;
  currency: string;
  needsPermission: boolean;
  needsAwareness: boolean;
  hasTicketCompensation: boolean;
  needsTransportation: boolean;
  includesAccommodation: boolean;
  useHighestAllowance: boolean;
  allowanceTiers: string;
  requesterId: string;
  requesterName: string;
  requiresVisa: boolean;
  visaCostPerEmployee: number;
  requiresTravelInsurance: boolean;
  travelInsuranceCostPerEmployee: number;
  ticketAirline: string;
  ticketFlightNumber: string;
  ticketDepartureTime?: string;
  ticketArrivalTime?: string;
  ticketCostPerEmployee: number;
  travelOfficeNotes: string;
  selectedDepartureFlightOfferId?: string;
  selectedReturnFlightOfferId?: string;
  sourceSystem: string;
  sourceTenantId?: string;
  sourceTrainingCourseId?: string;
  sourceTrainingCourseName: string;
  fundingSourceVoteCode: string;
  ticketFundingSourceVoteCode: string;
  visaFundingSourceVoteCode: string;
  healthInsuranceFundingSourceVoteCode: string;
  dailyAllowanceFundingSourceVoteCode: string;
  clothingAllowanceFundingSourceVoteCode: string;
  integrationWarnings: string;
  creationTime: string;
  employees?: TravelRequestEmployeeDto[];
  documents?: TravelDocumentDto[];
  flightOffers?: TravelFlightOfferDto[];
  employeeDocuments?: TravelEmployeeDocumentDto[];
  allowanceDetails?: TravelRequestAllowanceDetailDto[];
  allowanceSnapshot?: AllowanceSnapshotDto;
}

export interface CreateUpdateTravelRequestDto {
  title: string;
  description: string;
  travelTypeDefinitionId: string;
  startDate: string;
  endDate?: string;
  destinationCountry: string;
  destinationCity: string;
  department: string;
  category: number;
  dailyAllowanceRate: number;
  currency: string;
  needsPermission: boolean;
  needsAwareness: boolean;
  hasTicketCompensation: boolean;
  needsTransportation: boolean;
  includesAccommodation: boolean;
  useHighestAllowance: boolean;
  allowanceTiers: string;
  employeeIds: string[];
  employees: TravelRequestEmployeeInputDto[];
}

export interface CreateTravelRequestFromTrainingDto {
  trainingCourseId: string;
  courseName: string;
  startDate: string;
  endDate: string;
  destinationCountry: string;
  destinationCity: string;
  tenantId: string;
  ticketFundingSourceVoteCode: string;
  visaFundingSourceVoteCode: string;
  healthInsuranceFundingSourceVoteCode: string;
  dailyAllowanceFundingSourceVoteCode: string;
  clothingAllowanceFundingSourceVoteCode: string;
  employeeNumbers: string[];
}

export interface CreateTravelRequestFromTrainingResultDto {
  travelRequestId: string;
  status: RequestStatus;
  alreadyExists: boolean;
  missingEmployeeNumbers: string[];
  warningMessage: string;
}

export interface TrainingCourseLookupDto {
  trainingCourseId: string;
  tenantId?: string;
  tenantName: string;
  courseName: string;
  startDate: string;
  endDate: string;
  destinationCountry: string;
  destinationCity: string;
  ticketFundingSourceVoteCode: string;
  visaFundingSourceVoteCode: string;
  healthInsuranceFundingSourceVoteCode: string;
  dailyAllowanceFundingSourceVoteCode: string;
  clothingAllowanceFundingSourceVoteCode: string;
  employeeNumbers: string[];
}

export interface TrainingTravelPaymentDto {
  type: string;
  label: string;
  fundingSourceVoteCode: string;
  amount: number;
}

export interface TrainingTravelEmployeeResultDto {
  personnelId: string;
  employeeNumber: string;
  employeeName: string;
  rankName: string;
  ticketAmount: number;
  visaAmount: number;
  healthInsuranceAmount: number;
  travelAllowanceAmount: number;
  clothingAllowanceAmount: number;
  deductionAmount: number;
  allowanceTotal: number;
  payments: TrainingTravelPaymentDto[];
  totalAmount: number;
}

export interface TrainingTravelResultDto {
  tenantId?: string;
  trainingCourseId: string;
  travelRequestId?: string;
  courseName: string;
  travelRequestTitle: string;
  fundingSourceVoteCode: string;
  ticketFundingSourceVoteCode: string;
  visaFundingSourceVoteCode: string;
  healthInsuranceFundingSourceVoteCode: string;
  dailyAllowanceFundingSourceVoteCode: string;
  clothingAllowanceFundingSourceVoteCode: string;
  courseStartDate?: string;
  courseEndDate?: string;
  destinationCountry: string;
  destinationCity: string;
  status?: RequestStatus;
  isFound: boolean;
  isCompleted: boolean;
  completedAt?: string;
  travelDepartureDate?: string;
  travelReturnDate?: string;
  allowanceStartDate?: string;
  allowanceEndDate?: string;
  calculatedDays: number;
  employeeCount: number;
  currency: string;
  warningMessage: string;
  ticketsTotal: number;
  visaTotal: number;
  healthInsuranceTotal: number;
  travelAllowanceTotal: number;
  clothingAllowanceTotal: number;
  deductionTotal: number;
  allowancesTotal: number;
  grandTotal: number;
  employees: TrainingTravelEmployeeResultDto[];
  totalAmount: number;
}

export interface TravelRequestEmployeeInputDto {
  employeeId: string;
  employeeName: string;
  employeeNumber: string;
  rankId?: string;
  rankName: string;
  category?: number;
  dailyAllowanceRate: number;
  ticketClass: TicketClass;
}

export interface GetTravelRequestListInput extends PagedAndSortedResultRequestDto {
  travelTypeDefinitionId?: string;
  status?: RequestStatus;
  statuses?: RequestStatus[];
  searchText?: string;
  startDateFrom?: string;
  startDateTo?: string;
  department?: string;
}

export interface ChangeStatusDto {
  newStatus: RequestStatus;
  reason?: string;
  payrollElementId?: string;
}

export interface PayrollElementLookupDto {
  id: string;
  arabicName: string;
  englishName: string;
  payrollElementSetId: string;
  segmentCode: number;
  isActive: boolean;
}

export interface OverseasPayrollEmployeePreviewDto {
  employeeId: string;
  employeeName: string;
  employeeNumber: string;
  rankName: string;
  dailyRate: number;
  percentage: number;
  accommodationPaymentPercentage: number;
  dailyAmount: number;
  totalAmount: number;
  extraDays: number;
}

export interface OverseasPayrollPreviewDto {
  totalDays: number;
  extraDays: number;
  extraPeriodStartDate: string;
  extraPeriodEndDate: string;
  requiresPayrollElementSelection: boolean;
  employees: OverseasPayrollEmployeePreviewDto[];
}

export interface AssignFlightsDto {
  offers: FlightOfferDto[];
}

export interface FlightOfferDto {
  airline: string;
  flightNumber: string;
  departureTime: string;
  arrivalTime: string;
  price: number;
  currency: string;
}

export interface TravelOfficeDetailsDto {
  requiresVisa: boolean;
  visaCostPerEmployee: number;
  requiresTravelInsurance: boolean;
  travelInsuranceCostPerEmployee: number;
  ticket: FlightOfferDto;
  flightOffers: TravelFlightOfferInputDto[];
  travelOfficeNotes: string;
}

export interface TravelFlightOfferInputDto {
  direction: FlightDirection;
  ticketClass: TicketClass;
  airline: string;
  flightNumber: string;
  departureTime: string;
  arrivalTime: string;
  price: number;
  currency: string;
  duration: string;
}

export interface SelectFlightOffersDto {
  departureFlightOfferId: string;
  returnFlightOfferId: string;
  selections: TicketClassFlightSelectionDto[];
}

export interface TicketClassFlightSelectionDto {
  ticketClass: TicketClass;
  departureFlightOfferId: string;
  returnFlightOfferId: string;
}

export interface TravelEmployeeDocumentInputDto {
  employeeId: string;
  employeeName: string;
  employeeNumber: string;
  ticketNumber: string;
  pnr: string;
  ticketFileName: string;
  ticketBlobName: string;
  visaFileName: string;
  visaBlobName: string;
  insuranceFileName: string;
  insuranceBlobName: string;
}

export interface TravelFileUploadResultDto {
  fileName: string;
  blobName: string;
}

export interface ConfirmTicketBookingDto {
  employeeDocuments: TravelEmployeeDocumentInputDto[];
}

export interface AllowanceSummaryDto {
  totalDays: number;
  dailyBaseRate: number;
  accommodationMultiplier?: number;
  overseasTotal: number;
  overseasSegments: AllowanceCalculationSegmentDto[];
  employeeAllowances: Array<{
    employeeId: string;
    employeeName: string;
    employeeNumber: string;
    rankName: string;
    category: number;
    dailyRate: number;
    accommodationPaymentPercentage: number;
    overseasTotal: number;
    clothingAmount: number;
    clothingEligible: boolean;
    clothingCalculationNote: string;
    deductionAmount: number;
    netTotal: number;
    hasMatchingRule: boolean;
    segments: AllowanceCalculationSegmentDto[];
  }>;
  clothingAmount: number;
  clothingEligible: boolean;
  clothingCalculationNote: string;
  deductionAmount: number;
  netTotal: number;
  calculatedAt: string;
  warnings: string[];
}

export interface TravelCostSummaryDto {
  employeeCount: number;
  ticketCostPerEmployee: number;
  ticketsTotal: number;
  visaCostPerEmployee: number;
  visaTotal: number;
  travelInsuranceCostPerEmployee: number;
  travelInsuranceTotal: number;
  allowancesTotal: number;
  grandTotal: number;
  isAllowanceCalculated: boolean;
}

export interface OverseasAllowanceResult {
  days: number;
  dailyRate: number;
  totalPerEmployee: number;
  employeeCount: number;
  grandTotal: number;
}

export interface ClothingAllowanceResult {
  yearsOfService: number;
  amount: number;
  eligible: boolean;
  employeeCount: number;
  grandTotal: number;
}

@Injectable({
  providedIn: 'root',
})
export class TravelRequestService {
  apiName = 'default';

  private restService = inject(RestService);

  getList(input: GetTravelRequestListInput) {
    return this.restService.request<GetTravelRequestListInput, PagedResultDto<TravelRequestDto>>(
      { method: 'GET', url: '/api/travel/requests', params: input as any },
      { apiName: this.apiName }
    );
  }

  get(id: string) {
    return this.restService.request<void, TravelRequestDto>(
      { method: 'GET', url: `/api/travel/requests/${id}` },
      { apiName: this.apiName }
    );
  }

  getListByType(travelTypeDefinitionId: string, input: PagedAndSortedResultRequestDto) {
    return this.restService.request<PagedAndSortedResultRequestDto, PagedResultDto<TravelRequestDto>>(
      { method: 'GET', url: `/api/travel/requests/by-type/${travelTypeDefinitionId}`, params: input as any },
      { apiName: this.apiName }
    );
  }

  create(input: CreateUpdateTravelRequestDto) {
    return this.restService.request<CreateUpdateTravelRequestDto, TravelRequestDto>(
      { method: 'POST', url: '/api/travel/requests', body: input },
      { apiName: this.apiName }
    );
  }

  createFromTraining(input: CreateTravelRequestFromTrainingDto) {
    return this.restService.request<CreateTravelRequestFromTrainingDto, CreateTravelRequestFromTrainingResultDto>(
      { method: 'POST', url: '/api/travel/requests/from-training', body: input },
      { apiName: this.apiName }
    );
  }

  getTrainingCourseLookup() {
    return this.restService.request<void, TrainingCourseLookupDto[]>(
      { method: 'GET', url: '/api/travel/requests/training-courses/lookup' },
      { apiName: this.apiName }
    );
  }

  getTrainingResult(trainingCourseId: string) {
    return this.restService.request<void, TrainingTravelResultDto>(
      { method: 'GET', url: '/api/travel/requests/training-result', params: { trainingCourseId } },
      { apiName: this.apiName }
    );
  }

  update(id: string, input: CreateUpdateTravelRequestDto) {
    return this.restService.request<CreateUpdateTravelRequestDto, TravelRequestDto>(
      { method: 'PUT', url: `/api/travel/requests/${id}`, body: input },
      { apiName: this.apiName }
    );
  }

  delete(id: string) {
    return this.restService.request<void, void>(
      { method: 'DELETE', url: `/api/travel/requests/${id}` },
      { apiName: this.apiName }
    );
  }

  changeStatus(id: string, input: ChangeStatusDto) {
    return this.restService.request<ChangeStatusDto, TravelRequestDto>(
      { method: 'POST', url: `/api/travel/requests/${id}/status`, body: input },
      { apiName: this.apiName }
    );
  }

  assignFlights(id: string, input: AssignFlightsDto) {
    return this.restService.request<AssignFlightsDto, TravelRequestDto>(
      { method: 'POST', url: `/api/travel/requests/${id}/flights`, body: input },
      { apiName: this.apiName }
    );
  }

  saveTravelOfficeDetails(id: string, input: TravelOfficeDetailsDto) {
    return this.restService.request<TravelOfficeDetailsDto, TravelRequestDto>(
      { method: 'POST', url: `/api/travel/requests/${id}/travel-office`, body: input },
      { apiName: this.apiName }
    );
  }

  selectFlightOffers(id: string, input: SelectFlightOffersDto) {
    return this.restService.request<SelectFlightOffersDto, TravelRequestDto>(
      { method: 'POST', url: `/api/travel/requests/${id}/flight-selection`, body: input },
      { apiName: this.apiName }
    );
  }

  confirmTicketBooking(id: string, input: ConfirmTicketBookingDto) {
    return this.restService.request<ConfirmTicketBookingDto, TravelRequestDto>(
      { method: 'POST', url: `/api/travel/requests/${id}/ticket-booking`, body: input },
      { apiName: this.apiName }
    );
  }

  calculateAllowances(id: string, includeClothingAllowance = true) {
    return this.restService.request<void, AllowanceSummaryDto>(
      { method: 'GET', url: `/api/travel/requests/${id}/allowances`, params: { includeClothingAllowance } },
      { apiName: this.apiName }
    );
  }

  previewAllowances(input: CreateUpdateTravelRequestDto) {
    return this.restService.request<CreateUpdateTravelRequestDto, AllowanceSummaryDto>(
      { method: 'POST', url: '/api/travel/requests/allowances/preview', body: input },
      { apiName: this.apiName }
    );
  }

  getCostSummary(id: string) {
    return this.restService.request<void, TravelCostSummaryDto>(
      { method: 'GET', url: `/api/travel/requests/${id}/cost-summary` },
      { apiName: this.apiName }
    );
  }

  confirmByEmployee(id: string, employeeId: string) {
    return this.restService.request<void, void>(
      { method: 'POST', url: `/api/travel/requests/${id}/employee-confirm`, params: { employeeId } },
      { apiName: this.apiName }
    );
  }

  getPayrollElementLookup() {
    return this.restService.request<void, PayrollElementLookupDto[]>(
      { method: 'GET', url: '/api/travel/requests/payroll-elements/lookup' },
      { apiName: this.apiName }
    );
  }

  getOverseasPayrollPreview(id: string) {
    return this.restService.request<void, OverseasPayrollPreviewDto>(
      { method: 'GET', url: `/api/travel/requests/${id}/overseas-payroll-preview` },
      { apiName: this.apiName }
    );
  }

  uploadFile(file: File) {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.restService.request<FormData, TravelFileUploadResultDto>(
      { method: 'POST', url: '/api/travel/requests/upload-file', body: formData },
      { apiName: this.apiName }
    );
  }

  downloadFile(blobName: string): Observable<Blob> {
    return this.restService.request<void, Blob>(
      { method: 'GET', url: `/api/travel/requests/download-file/${encodeURIComponent(blobName)}`, responseType: 'blob' },
      { apiName: this.apiName }
    );
  }
}
