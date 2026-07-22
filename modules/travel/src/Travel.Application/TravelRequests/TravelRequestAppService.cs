using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Travel.Allowances;
using Travel.Localization;
using Travel.Permissions;
using Travel.TravelRequests;
using Travel.TravelTypes;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.BlobStoring;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace Travel.TravelRequests;

[Authorize(TravelManagementPermissions.TravelRequests.Default)]
public class TravelRequestAppService :
    CrudAppService<
        TravelRequest,
        TravelRequestDto,
        Guid,
        GetTravelRequestListInput,
        CreateUpdateTravelRequestDto>,
    ITravelRequestAppService
{
    private readonly TravelRequestManager _travelRequestManager;
    private readonly AllowanceCalculatorService _allowanceCalculatorService;
    private readonly IRepository<AllowanceRule, Guid> _allowanceRuleRepository;
    private readonly IRepository<AllowanceRate, Guid> _allowanceRateRepository;
    private readonly IRepository<EmployeeClothingHistory, Guid> _clothingHistoryRepository;
    private readonly IRepository<TravelFundingSourceVoteRule, Guid> _fundingSourceVoteRuleRepository;
    private readonly ITravelEmployeeLookup _employeeLookup;
    private readonly IRepository<TravelTypeDefinition, Guid> _travelTypeRepository;
    private readonly IPayrollElementLookup _payrollElementLookup;
    private readonly IPayrollOverseasTaskGenerator _payrollOverseasTaskGenerator;
    private readonly IDistributedEventBus _distributedEventBus;

    public TravelRequestAppService(
        IRepository<TravelRequest, Guid> repository,
        TravelRequestManager travelRequestManager,
        AllowanceCalculatorService allowanceCalculatorService,
        IRepository<AllowanceRule, Guid> allowanceRuleRepository,
        IRepository<AllowanceRate, Guid> allowanceRateRepository,
        IRepository<EmployeeClothingHistory, Guid> clothingHistoryRepository,
        IRepository<TravelFundingSourceVoteRule, Guid> fundingSourceVoteRuleRepository,
        ITravelEmployeeLookup employeeLookup,
        IRepository<TravelTypeDefinition, Guid> travelTypeRepository,
        IPayrollElementLookup payrollElementLookup,
        IPayrollOverseasTaskGenerator payrollOverseasTaskGenerator,
        IDistributedEventBus distributedEventBus) : base(repository)
    {
        _travelRequestManager = travelRequestManager;
        _allowanceCalculatorService = allowanceCalculatorService;
        _allowanceRuleRepository = allowanceRuleRepository;
        _allowanceRateRepository = allowanceRateRepository;
        _clothingHistoryRepository = clothingHistoryRepository;
        _fundingSourceVoteRuleRepository = fundingSourceVoteRuleRepository;
        _employeeLookup = employeeLookup;
        _travelTypeRepository = travelTypeRepository;
        _payrollElementLookup = payrollElementLookup;
        _payrollOverseasTaskGenerator = payrollOverseasTaskGenerator;
        _distributedEventBus = distributedEventBus;

        ObjectMapperContext = typeof(TravelApplicationModule);
        LocalizationResource = typeof(TravelResource);
        GetPolicyName = TravelManagementPermissions.TravelRequests.Default;
        GetListPolicyName = TravelManagementPermissions.TravelRequests.Default;
        CreatePolicyName = TravelManagementPermissions.TravelRequests.Create;
        UpdatePolicyName = TravelManagementPermissions.TravelRequests.Edit;
        DeletePolicyName = TravelManagementPermissions.TravelRequests.Delete;
    }

    [Authorize(TravelManagementPermissions.TravelRequests.Create)]
    public virtual async Task<CreateTravelRequestFromTrainingResultDto> CreateFromTrainingAsync(CreateTravelRequestFromTrainingDto input)
    {
        var existing = await FindExistingTrainingRequestAsync(input.TrainingCourseId, input.TenantId);
        if (existing != null)
        {
            var existingMissingNumbers = ParseMissingEmployeeNumbers(existing.IntegrationWarnings);
            return new CreateTravelRequestFromTrainingResultDto
            {
                TravelRequestId = existing.Id,
                Status = existing.Status,
                AlreadyExists = true,
                MissingEmployeeNumbers = existingMissingNumbers,
                WarningMessage = existing.IntegrationWarnings
            };
        }

        var requestedNumbers = NormalizeEmployeeNumbers(input.EmployeeNumbers);
        var lookupEmployees = await _employeeLookup.FindByEmployeeNumbersAsync(requestedNumbers);
        var employeesByNumber = lookupEmployees
            .GroupBy(x => x.EmployeeNumber, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);
        var missingNumbers = requestedNumbers
            .Where(number => !employeesByNumber.ContainsKey(number))
            .ToList();
        var warningMessage = BuildMissingEmployeeWarning(missingNumbers);
        var category = lookupEmployees.FirstOrDefault()?.Category ?? AllowanceCategory.A;

        var courseType = await _travelTypeRepository.FindAsync(x => x.Code == 1 && x.IsActive)
            ?? throw new UserFriendlyException("نوع السفر 'دورة تدريبية' غير موجود في النظام");

        var request = new TravelRequest(
            Guid.NewGuid(),
            BuildTrainingRequestTitle(input.CourseName),
            input.CourseName,
            courseType.Id,
            input.StartDate,
            ResolveEndDate(input.StartDate, input.EndDate),
            input.DestinationCountry,
            input.DestinationCity,
            string.Empty,
            category,
            0m,
            "OMR",
            false,
            false,
            false,
            false,
            false,
            false,
            string.Empty,
            CurrentUser.GetId(),
            CurrentUser.Name ?? "System");

        request.SetTrainingSource(
            input.TrainingCourseId,
            input.CourseName,
            input.TenantId,
            string.Empty,
            input.TicketFundingSourceVoteCode,
            input.VisaFundingSourceVoteCode,
            input.HealthInsuranceFundingSourceVoteCode,
            input.DailyAllowanceFundingSourceVoteCode,
            input.ClothingAllowanceFundingSourceVoteCode);
        request.SetIntegrationWarnings(warningMessage);

        foreach (var employeeNumber in requestedNumbers.Where(employeesByNumber.ContainsKey))
        {
            var employee = employeesByNumber[employeeNumber];
            var rate = await GetDailyAllowanceRateAsync(employee.RankId, category);
            request.AddEmployee(
                employee.Id,
                employee.Name,
                employee.EmployeeNumber,
                employee.RankId,
                employee.RankName,
                category,
                rate.Amount,
                rate.TicketClass);
        }

        await Repository.InsertAsync(request);

        return new CreateTravelRequestFromTrainingResultDto
        {
            TravelRequestId = request.Id,
            Status = request.Status,
            AlreadyExists = false,
            MissingEmployeeNumbers = missingNumbers,
            WarningMessage = warningMessage
        };
    }

    public virtual Task<List<TrainingCourseLookupDto>> GetTrainingCourseLookupAsync()
    {
        return Task.FromResult(new List<TrainingCourseLookupDto>
        {
            new()
            {
                TrainingCourseId = Guid.Parse("70000000-0000-0000-0000-000000000001"),
                TenantId = Guid.Parse("90000000-0000-0000-0000-000000000001"),
                TenantName = "منظومة التدريب - قطاع التقنية",
                CourseName = "دورة إدارة مشاريع تقنية المعلومات",
                StartDate = new DateTime(2026, 6, 7),
                EndDate = new DateTime(2026, 6, 11),
                DestinationCountry = "الإمارات العربية المتحدة",
                DestinationCity = "دبي",
                TicketFundingSourceVoteCode = "TRN-TKT-2026-A01",
                VisaFundingSourceVoteCode = "TRN-VSA-2026-A01",
                HealthInsuranceFundingSourceVoteCode = "TRN-HIN-2026-A01",
                DailyAllowanceFundingSourceVoteCode = "TRN-DAL-2026-A01",
                ClothingAllowanceFundingSourceVoteCode = "TRN-CLT-2026-A01",
                EmployeeNumbers = ["OMN-10245", "OMN-10246"]
            },
            new()
            {
                TrainingCourseId = Guid.Parse("70000000-0000-0000-0000-000000000002"),
                TenantId = Guid.Parse("90000000-0000-0000-0000-000000000002"),
                TenantName = "منظومة التدريب - قطاع المالية",
                CourseName = "ورشة تحليل البيانات المالية",
                StartDate = new DateTime(2026, 7, 12),
                EndDate = new DateTime(2026, 7, 16),
                DestinationCountry = "المملكة العربية السعودية",
                DestinationCity = "الرياض",
                TicketFundingSourceVoteCode = "FIN-TKT-7742-B",
                VisaFundingSourceVoteCode = "FIN-VSA-7742-B",
                HealthInsuranceFundingSourceVoteCode = "FIN-HIN-7742-B",
                DailyAllowanceFundingSourceVoteCode = "FIN-DAL-7742-B",
                ClothingAllowanceFundingSourceVoteCode = "FIN-CLT-7742-B",
                EmployeeNumbers = ["OMN-10247", "OMN-99999"]
            },
            new()
            {
                TrainingCourseId = Guid.Parse("70000000-0000-0000-0000-000000000003"),
                TenantId = Guid.Parse("90000000-0000-0000-0000-000000000003"),
                TenantName = "منظومة التدريب - قطاع الجودة",
                CourseName = "برنامج الجودة المؤسسية المتقدم",
                StartDate = new DateTime(2026, 8, 3),
                EndDate = new DateTime(2026, 8, 6),
                DestinationCountry = "سلطنة عمان",
                DestinationCity = "صلالة",
                TicketFundingSourceVoteCode = "QMS-TKT-26-009",
                VisaFundingSourceVoteCode = "QMS-VSA-26-009",
                HealthInsuranceFundingSourceVoteCode = "QMS-HIN-26-009",
                DailyAllowanceFundingSourceVoteCode = "QMS-DAL-26-009",
                ClothingAllowanceFundingSourceVoteCode = "QMS-CLT-26-009",
                EmployeeNumbers = ["OMN-10249"]
            }
        });
    }

    public virtual async Task<TrainingTravelResultDto> GetTrainingResultAsync(Guid trainingCourseId)
    {
        await CheckPolicyAsync(TravelManagementPermissions.TravelRequests.Default);

        var request = await FindExistingTrainingRequestAsync(trainingCourseId, tenantId: null);
        if (request == null)
        {
            return new TrainingTravelResultDto
            {
                TrainingCourseId = trainingCourseId,
                IsFound = false,
                IsCompleted = false
            };
        }

        await EnsureAllowanceSnapshotMatchesTravelDatesAsync(request);

        var result = new TrainingTravelResultDto
        {
            TenantId = request.SourceTenantId,
            TrainingCourseId = trainingCourseId,
            TravelRequestId = request.Id,
            CourseName = request.SourceTrainingCourseName,
            TravelRequestTitle = request.Title,
            FundingSourceVoteCode = request.FundingSourceVoteCode,
            TicketFundingSourceVoteCode = GetFundingSourceVoteCode(request.TicketFundingSourceVoteCode, request),
            VisaFundingSourceVoteCode = GetFundingSourceVoteCode(request.VisaFundingSourceVoteCode, request),
            HealthInsuranceFundingSourceVoteCode = GetFundingSourceVoteCode(request.HealthInsuranceFundingSourceVoteCode, request),
            DailyAllowanceFundingSourceVoteCode = GetFundingSourceVoteCode(request.DailyAllowanceFundingSourceVoteCode, request),
            ClothingAllowanceFundingSourceVoteCode = GetFundingSourceVoteCode(request.ClothingAllowanceFundingSourceVoteCode, request),
            CourseStartDate = request.StartDate,
            CourseEndDate = request.EndDate,
            DestinationCountry = request.DestinationCountry,
            DestinationCity = request.DestinationCity,
            Status = request.Status,
            IsFound = true,
            IsCompleted = request.Status == RequestStatus.Completed,
            CompletedAt = request.Status == RequestStatus.Completed
                ? request.LastModificationTime ?? request.CreationTime
                : null,
            TravelDepartureDate = request.GetAllowanceStartDate(),
            TravelReturnDate = request.GetAllowanceEndDate(),
            AllowanceStartDate = request.GetAllowanceStartDate(),
            AllowanceEndDate = request.GetAllowanceEndDate(),
            CalculatedDays = request.GetDurationInDays(),
            EmployeeCount = request.Employees.Count,
            Currency = request.Currency,
            WarningMessage = request.IntegrationWarnings,
            TicketsTotal = CalculateTicketsTotal(request),
            VisaTotal = request.RequiresVisa ? request.Employees.Count * request.VisaCostPerEmployee : 0m,
            HealthInsuranceTotal = request.RequiresTravelInsurance ? request.Employees.Count * request.TravelInsuranceCostPerEmployee : 0m,
            TravelAllowanceTotal = request.AllowanceDetails.Sum(x => x.OverseasTotal),
            ClothingAllowanceTotal = request.AllowanceDetails.Sum(x => x.ClothingAmount),
            DeductionTotal = request.AllowanceDetails.Sum(x => x.DeductionAmount),
            AllowancesTotal = request.AllowanceSnapshot?.NetTotal ?? request.AllowanceDetails.Sum(x => x.NetTotal)
        };
        result.GrandTotal = result.TicketsTotal + result.VisaTotal + result.HealthInsuranceTotal + result.AllowancesTotal;
        result.TotalAmount = result.GrandTotal;

        if (request.Status != RequestStatus.Completed)
        {
            return result;
        }

        result.Employees = request.AllowanceDetails
            .OrderBy(x => x.EmployeeNumber)
            .Select(detail => BuildTrainingEmployeeResult(request, detail))
            .ToList();
        result.TotalAmount = result.Employees.Sum(x => x.TotalAmount);
        result.GrandTotal = result.TotalAmount;

        return result;
    }

    [Authorize(TravelManagementPermissions.TravelRequests.ViewByType)]
    public virtual async Task<PagedResultDto<TravelRequestDto>> GetListByTypeAsync(
        Guid travelTypeDefinitionId,
        PagedAndSortedResultRequestDto input)
    {
        var query = await GetQueryWithDetailsAsync();

        query = query
            .Where(x => x.TravelTypeDefinitionId == travelTypeDefinitionId)
            .OrderByDescending(x => x.CreationTime);

        var totalCount = await AsyncExecuter.CountAsync(query);
        var items = await AsyncExecuter.ToListAsync(query.PageBy(input));

        return new PagedResultDto<TravelRequestDto>(
            totalCount,
            ObjectMapper.Map<List<TravelRequest>, List<TravelRequestDto>>(items));
    }

    public override async Task<TravelRequestDto> GetAsync(Guid id)
    {
        await CheckGetPolicyAsync();

        var request = await GetEntityByIdAsync(id);
        await EnsureAllowanceSnapshotMatchesTravelDatesAsync(request);

        return ObjectMapper.Map<TravelRequest, TravelRequestDto>(request);
    }

    protected override async Task<IQueryable<TravelRequest>> CreateFilteredQueryAsync(GetTravelRequestListInput input)
    {
        var query = await GetQueryWithDetailsAsync();

        query = query
            .WhereIf(input.TravelTypeDefinitionId.HasValue, x => x.TravelTypeDefinitionId == input.TravelTypeDefinitionId!.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(!input.Status.HasValue && input.Statuses.Count > 0, x => input.Statuses.Contains(x.Status))
            .WhereIf(!input.SearchText.IsNullOrWhiteSpace(),
                x => x.Title.Contains(input.SearchText!) ||
                     x.Description.Contains(input.SearchText!) ||
                     x.DestinationCountry.Contains(input.SearchText!) ||
                     x.DestinationCity.Contains(input.SearchText!))
            .WhereIf(input.StartDateFrom.HasValue, x => x.StartDate >= input.StartDateFrom!.Value)
            .WhereIf(input.StartDateTo.HasValue, x => x.StartDate <= input.StartDateTo!.Value)
            .WhereIf(!input.Department.IsNullOrWhiteSpace(), x => x.Department == input.Department);

        return query;
    }

    public virtual async Task<TravelRequestDto> ChangeStatusAsync(Guid id, ChangeStatusDto input)
    {
        await CheckPolicyAsync(TravelManagementPermissions.TravelRequests.Edit);

        var request = await GetEntityByIdAsync(id);

        switch (input.NewStatus)
        {
            // === الحالات الاستثنائية التي تتطلب سبباً ===
            case RequestStatus.Returned:
                if (input.Reason.IsNullOrWhiteSpace())
                    throw new UserFriendlyException(L["ReasonRequiredForReturn"]);
                await _travelRequestManager.ReturnAsync(request, input.Reason!);
                break;
            case RequestStatus.Rejected:
                if (input.Reason.IsNullOrWhiteSpace())
                    throw new UserFriendlyException(L["ReasonRequiredForRejection"]);
                await _travelRequestManager.RejectAsync(request, input.Reason!);
                break;

            // === المسار المختصر ===
            case RequestStatus.AtTravelOffice:
                await _travelRequestManager.SendToTravelOfficeAsync(request);
                break;
            case RequestStatus.CalculatingAllowances:
                await CalculateAndSetAllowanceSnapshotAsync(request, includeClothingAllowance: true);
                await _travelRequestManager.ChangeStatusAsync(request, RequestStatus.CalculatingAllowances, input.Reason);
                break;
            case RequestStatus.Completed:
                var calculation = await CalculateAndSetAllowanceSnapshotAsync(request, includeClothingAllowance: true);
                if (request.GetDurationInDays() > TravelRequestConsts.MaxTravelAllowanceDays)
                {
                    var payrollElements = await _payrollElementLookup.GetPayrollElementsAsync();
                    if (payrollElements.Count > 0)
                    {
                        if (!input.PayrollElementId.HasValue)
                        {
                            throw new UserFriendlyException(L["PayrollElementRequiredForLongTravel"]);
                        }

                        if (payrollElements.All(e => e.Id != input.PayrollElementId.Value))
                        {
                            throw new UserFriendlyException(L["PayrollElementInvalidForLongTravel"]);
                        }

                        await _payrollOverseasTaskGenerator.GenerateAsync(request, input.PayrollElementId.Value);
                    }
                }
                await _travelRequestManager.CompleteAsync(request, request.AllowanceSnapshot!);
                await RecordClothingAllowanceHistoryAsync(request, calculation);
                break;

            // === الحالات التي لم تعد مستخدمة كمراحل رئيسية (للتوافق) ===
            case RequestStatus.PendingApproval:
            case RequestStatus.Approved:
            case RequestStatus.TicketsBooked:
            case RequestStatus.Confirmed:
                throw new UserFriendlyException(L["UseSimplifiedWorkflow"]);

            default:
                await _travelRequestManager.ChangeStatusAsync(request, input.NewStatus, input.Reason);
                break;
        }

        await Repository.UpdateAsync(request);

        if (input.NewStatus == RequestStatus.Completed
            && request.SourceTrainingCourseId.HasValue)
        {
            var trainingResult = await GetTrainingResultAsync(request.SourceTrainingCourseId.Value);
            await _distributedEventBus.PublishAsync(new TrainingTravelCompletedEto
            {
                TenantId = request.SourceTenantId,
                Result = trainingResult
            });
        }

        return ObjectMapper.Map<TravelRequest, TravelRequestDto>(request);
    }

    [Authorize(TravelManagementPermissions.TravelRequests.Edit)]
    public virtual async Task<TravelRequestDto> SaveTravelOfficeDetailsAsync(Guid id, TravelOfficeDetailsDto input)
    {
        var request = await GetEntityByIdAsync(id);

        EnsureTravelOfficeCanSendOffers(request);

        request.SetTravelOfficeDetails(
            input.RequiresVisa,
            input.VisaCostPerEmployee,
            input.RequiresTravelInsurance,
            input.TravelInsuranceCostPerEmployee,
            string.Empty,
            string.Empty,
            null,
            null,
            0m,
            input.TravelOfficeNotes);

        var offers = input.FlightOffers
            .Where(offer => !offer.Airline.IsNullOrWhiteSpace() && !offer.FlightNumber.IsNullOrWhiteSpace())
            .Select(offer => new TravelFlightOffer(
                Guid.NewGuid(),
                request.Id,
                offer.Direction,
                offer.TicketClass,
                offer.Airline,
                offer.FlightNumber,
                offer.DepartureTime,
                offer.ArrivalTime,
                offer.Price,
                offer.Currency,
                offer.Duration))
            .ToList();

        EnsureFlightOffersAreValidForTravelDates(request, offers);

        var requiredTicketClasses = GetRequiredTicketClasses(request);
        if (requiredTicketClasses.Any(ticketClass =>
                !offers.Any(x => x.TicketClass == ticketClass && x.Direction == FlightDirection.Departure) ||
                !offers.Any(x => x.TicketClass == ticketClass && x.Direction == FlightDirection.Return)))
        {
            throw new UserFriendlyException(L["DepartureAndReturnOffersRequired"]);
        }

        request.ReplaceFlightOffers(offers);
        if (request.Status != RequestStatus.AtTravelOffice)
        {
            await _travelRequestManager.ChangeStatusAsync(request, RequestStatus.AtTravelOffice);
        }
        await Repository.UpdateAsync(request);

        return ObjectMapper.Map<TravelRequest, TravelRequestDto>(request);
    }

    [Authorize(TravelManagementPermissions.TravelRequests.Edit)]
    public virtual async Task<TravelRequestDto> SelectFlightOffersAsync(Guid id, SelectFlightOffersDto input)
    {
        var request = await GetEntityByIdAsync(id);

        var allowedStatuses = new[]
        {
            RequestStatus.AtTravelOffice,
            RequestStatus.FlightSelection,
            RequestStatus.VisaCheck,
            RequestStatus.InsuranceCheck,
            RequestStatus.PendingApproval,
            RequestStatus.Approved
        };

        if (!allowedStatuses.Contains(request.Status))
        {
            throw new BusinessException(TravelErrorCodes.InvalidStatusTransition)
                .WithData("Current", request.Status)
                .WithData("Target", RequestStatus.AtTravelOffice);
        }

        var selections = NormalizeFlightSelections(input, request);
        EnsureSelectedFlightDatesAreValid(request, selections);
        request.SelectFlightOffers(selections);
        if (request.Status != RequestStatus.AtTravelOffice)
        {
            await _travelRequestManager.ChangeStatusAsync(request, RequestStatus.AtTravelOffice);
        }
        await Repository.UpdateAsync(request);

        return ObjectMapper.Map<TravelRequest, TravelRequestDto>(request);
    }

    [Authorize(TravelManagementPermissions.TravelRequests.Edit)]
    public virtual async Task<TravelRequestDto> ConfirmTicketBookingAsync(Guid id, ConfirmTicketBookingDto input)
    {
        var request = await GetEntityByIdAsync(id);

        var allowedStatuses = new[]
        {
            RequestStatus.AtTravelOffice,
            RequestStatus.FlightSelection,
            RequestStatus.VisaCheck,
            RequestStatus.InsuranceCheck,
            RequestStatus.PendingApproval,
            RequestStatus.Approved
        };

        if (!allowedStatuses.Contains(request.Status))
        {
            throw new BusinessException(TravelErrorCodes.InvalidStatusTransition)
                .WithData("Current", request.Status)
                .WithData("Target", RequestStatus.TicketsBooked);
        }

        var requiredTicketClasses = GetRequiredTicketClasses(request);
        if (requiredTicketClasses.Any(ticketClass =>
                !request.FlightOffers.Any(x => x.IsSelected && x.TicketClass == ticketClass && x.Direction == FlightDirection.Departure) ||
                !request.FlightOffers.Any(x => x.IsSelected && x.TicketClass == ticketClass && x.Direction == FlightDirection.Return)))
        {
            throw new UserFriendlyException(L["FlightOfferSelectionRequired"]);
        }

        EnsureEmployeeBookingDocumentsAreComplete(request, input);

        var employeeDocuments = input.EmployeeDocuments
            .GroupBy(x => x.EmployeeId)
            .Select(group => group.Last())
            .Select(document => new TravelEmployeeDocument(
                Guid.NewGuid(),
                request.Id,
                document.EmployeeId,
                document.EmployeeName,
                document.EmployeeNumber,
                document.TicketNumber,
                document.Pnr,
                document.TicketFileName,
                document.TicketBlobName,
                request.RequiresVisa ? document.VisaFileName : string.Empty,
                request.RequiresVisa ? document.VisaBlobName : string.Empty,
                request.RequiresTravelInsurance ? document.InsuranceFileName : string.Empty,
                request.RequiresTravelInsurance ? document.InsuranceBlobName : string.Empty))
            .ToList();

        request.ReplaceEmployeeDocuments(employeeDocuments);

        await _travelRequestManager.BookTicketsAsync(request, new List<FlightOfferDto>());
        await Repository.UpdateAsync(request);

        return ObjectMapper.Map<TravelRequest, TravelRequestDto>(request);
    }

    public virtual async Task<AllowanceSummaryDto> CalculateAllowancesAsync(Guid id, bool includeClothingAllowance = false)
    {
        var request = await GetEntityByIdAsync(id);
        var employees = request.Employees.ToList();

        var calculation = await _allowanceCalculatorService.CalculateAsync(request, employees, includeClothingAllowance);

        request.SetAllowanceSnapshot(
            BuildAllowanceSnapshot(calculation),
            BuildAllowanceDetails(request, calculation));
        await Repository.UpdateAsync(request);

        return BuildAllowanceSummary(calculation);
    }

    private async Task<AllowanceCalculationResult> CalculateAndSetAllowanceSnapshotAsync(
        TravelRequest request,
        bool includeClothingAllowance)
    {
        EnsureTicketDatesAreValidForCalculation(request);

        var calculation = await _allowanceCalculatorService.CalculateAsync(
            request,
            request.Employees.ToList(),
            includeClothingAllowance);

        request.SetAllowanceSnapshot(
            BuildAllowanceSnapshot(calculation),
            BuildAllowanceDetails(request, calculation));

        return calculation;
    }

    private async Task EnsureAllowanceSnapshotMatchesTravelDatesAsync(TravelRequest request)
    {
        EnsureTicketDatesAreValidForCalculation(request);

        if (request.AllowanceSnapshot == null ||
            request.Status is not (RequestStatus.CalculatingAllowances or RequestStatus.Completed))
        {
            return;
        }

        var currentDuration = request.GetDurationInDays();
        var hasStaleDetails = request.AllowanceDetails.Count != request.Employees.Count ||
                              request.AllowanceDetails.Any(detail => detail.CalculatedDays != currentDuration);

        if (!hasStaleDetails)
        {
            return;
        }

        await CalculateAndSetAllowanceSnapshotAsync(request, includeClothingAllowance: true);
        await Repository.UpdateAsync(request);
    }

    public virtual async Task<AllowanceSummaryDto> PreviewAllowancesAsync(CreateUpdateTravelRequestDto input)
    {
        await CheckPolicyAsync(TravelManagementPermissions.TravelRequests.Default);

        var previewRequest = new TravelRequest(
            Guid.NewGuid(),
            input.Title,
            input.Description,
            input.TravelTypeDefinitionId,
            input.StartDate,
            ResolveEndDate(input.StartDate, input.EndDate),
            input.DestinationCountry,
            input.DestinationCity,
            input.Department,
            input.Category,
            input.DailyAllowanceRate,
            input.Currency,
            input.NeedsPermission,
            input.NeedsAwareness,
            input.HasTicketCompensation,
            input.NeedsTransportation,
            input.IncludesAccommodation,
            input.UseHighestAllowance,
            input.AllowanceTiers,
            CurrentUser.Id ?? Guid.Empty,
            CurrentUser.Name ?? "System");

        var employees = NormalizeEmployees(input);
        foreach (var employee in employees)
        {
            previewRequest.AddEmployee(
                employee.EmployeeId,
                employee.EmployeeName,
                employee.EmployeeNumber,
                employee.RankId,
                employee.RankName,
                input.Category,
                employee.DailyAllowanceRate,
                employee.TicketClass);
        }

        var calculation = await _allowanceCalculatorService.CalculateAsync(previewRequest, previewRequest.Employees.ToList());
        var summary = BuildAllowanceSummary(calculation);
        summary.Warnings = await GetAllowancePreviewWarningsAsync(input, employees);

        return summary;
    }

    public virtual async Task<TravelCostSummaryDto> GetCostSummaryAsync(Guid id)
    {
        await CheckPolicyAsync(TravelManagementPermissions.TravelRequests.Default);

        var request = await GetEntityByIdAsync(id);
        await EnsureAllowanceSnapshotMatchesTravelDatesAsync(request);
        var isAllowanceCalculated = request.AllowanceSnapshot != null;
        var allowancesTotal = request.AllowanceSnapshot?.NetTotal;

        if (!allowancesTotal.HasValue)
        {
            var estimate = await _allowanceCalculatorService.CalculateAsync(request, request.Employees.ToList());
            allowancesTotal = estimate.NetTotal;
        }

        return BuildTravelCostSummary(request, allowancesTotal.Value, isAllowanceCalculated);
    }

    [Authorize(TravelManagementPermissions.TravelRequests.Confirm)]
    public virtual async Task ConfirmByEmployeeAsync(Guid id, Guid employeeId)
    {
        var request = await GetEntityByIdAsync(id);

        if (!request.Employees.Any(x => x.EmployeeId == employeeId))
        {
            throw new BusinessException(TravelErrorCodes.EmployeeNotFound)
                .WithData("EmployeeId", employeeId);
        }

        await _travelRequestManager.ConfirmByEmployeeAsync(request, employeeId);
        await Repository.UpdateAsync(request);
    }

    [Authorize(TravelManagementPermissions.TravelRequests.Default)]
    public virtual async Task<List<PayrollElementLookupDto>> GetPayrollElementLookupAsync()
    {
        await CheckPolicyAsync(TravelManagementPermissions.TravelRequests.Default);

        return await _payrollElementLookup.GetPayrollElementsAsync();
    }

    [Authorize(TravelManagementPermissions.TravelRequests.Default)]
    public virtual async Task<OverseasPayrollPreviewDto> GetOverseasPayrollPreviewAsync(Guid id)
    {
        await CheckPolicyAsync(TravelManagementPermissions.TravelRequests.Default);

        var request = await GetEntityByIdAsync(id);
        await EnsureAllowanceSnapshotMatchesTravelDatesAsync(request);

        var preview = OverseasPayrollPreviewBuilder.Build(request);
        preview.RequiresPayrollElementSelection =
            preview.ExtraDays > 0 &&
            (await _payrollElementLookup.GetPayrollElementsAsync()).Count > 0;

        return preview;
    }

    public virtual async Task<TravelFileUploadResultDto> UploadFileAsync(byte[] file, string fileName)
    {
        var blobContainer = LazyServiceProvider.LazyGetRequiredService<IBlobContainer<TravelDocumentContainer>>();
        var extension = System.IO.Path.GetExtension(fileName);
        var name = Guid.NewGuid().ToString("N") + extension;
        await blobContainer.SaveAsync(name, file);
        return new TravelFileUploadResultDto
        {
            FileName = fileName,
            BlobName = name
        };
    }

    public virtual async Task<byte[]> DownloadFileAsync(string blobName)
    {
        var blobContainer = LazyServiceProvider.LazyGetRequiredService<IBlobContainer<TravelDocumentContainer>>();
        using var stream = await blobContainer.GetAsync(blobName);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private Task<IQueryable<TravelRequest>> GetQueryWithDetailsAsync()
        => Repository.WithDetailsAsync(
            x => x.Employees,
            x => x.Documents,
            x => x.FlightOffers,
            x => x.EmployeeDocuments,
            x => x.AllowanceDetails,
            x => x.AllowanceSnapshot!);

    protected override async Task<TravelRequest> GetEntityByIdAsync(Guid id)
    {
        var query = await GetQueryWithDetailsAsync();

        var entity = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id));
        if (entity == null)
        {
            throw new EntityNotFoundException(typeof(TravelRequest), id);
        }
        return entity;
    }

    protected override async Task<TravelRequest> MapToEntityAsync(CreateUpdateTravelRequestDto createInput)
    {
        var request = new TravelRequest(
            Guid.NewGuid(),
            createInput.Title,
            createInput.Description,
            createInput.TravelTypeDefinitionId,
            createInput.StartDate,
            ResolveEndDate(createInput.StartDate, createInput.EndDate),
            createInput.DestinationCountry,
            createInput.DestinationCity,
            createInput.Department,
            createInput.Category,
            createInput.DailyAllowanceRate,
            createInput.Currency,
            createInput.NeedsPermission,
            createInput.NeedsAwareness,
            createInput.HasTicketCompensation,
            createInput.NeedsTransportation,
            createInput.IncludesAccommodation,
            createInput.UseHighestAllowance,
            createInput.AllowanceTiers,
            CurrentUser.GetId(),
            CurrentUser.Name ?? "System"
        );

        var employees = NormalizeEmployees(createInput);
        foreach (var employee in employees)
        {
            request.AddEmployee(employee.EmployeeId, employee.EmployeeName, employee.EmployeeNumber, employee.RankId, employee.RankName, createInput.Category, employee.DailyAllowanceRate, employee.TicketClass);
        }

        var fundingSourceVoteCodes = await GetActiveFundingSourceVoteCodesAsync();
        request.SetFundingSourceVoteCodes(
            GetDefaultFundingSourceVoteCode(fundingSourceVoteCodes),
            GetFundingSourceVoteCode(fundingSourceVoteCodes, FundingSourceVotePaymentType.Ticket),
            GetFundingSourceVoteCode(fundingSourceVoteCodes, FundingSourceVotePaymentType.Visa),
            GetFundingSourceVoteCode(fundingSourceVoteCodes, FundingSourceVotePaymentType.HealthInsurance),
            GetFundingSourceVoteCode(fundingSourceVoteCodes, FundingSourceVotePaymentType.DailyAllowance),
            GetFundingSourceVoteCode(fundingSourceVoteCodes, FundingSourceVotePaymentType.ClothingAllowance));

        return request;
    }

    protected override async Task MapToEntityAsync(CreateUpdateTravelRequestDto updateInput, TravelRequest entity)
    {
        entity.UpdateDetails(
            updateInput.Title,
            updateInput.Description,
            updateInput.TravelTypeDefinitionId,
            updateInput.StartDate,
            ResolveEndDate(updateInput.StartDate, updateInput.EndDate),
            updateInput.DestinationCountry,
            updateInput.DestinationCity,
            updateInput.Department,
            updateInput.Category,
            updateInput.DailyAllowanceRate,
            updateInput.Currency,
            updateInput.NeedsPermission,
            updateInput.NeedsAwareness,
            updateInput.HasTicketCompensation,
            updateInput.NeedsTransportation,
            updateInput.IncludesAccommodation,
            updateInput.UseHighestAllowance,
            updateInput.AllowanceTiers
        );

        var currentEmployeeIds = entity.Employees.Select(e => e.EmployeeId).ToList();
        var employees = NormalizeEmployees(updateInput);
        var newEmployeeIds = employees.Select(e => e.EmployeeId).ToList();

        foreach (var toRemove in currentEmployeeIds.Except(newEmployeeIds))
        {
            entity.RemoveEmployee(toRemove);
        }

        foreach (var toAdd in newEmployeeIds.Except(currentEmployeeIds))
        {
            var employee = employees.First(e => e.EmployeeId == toAdd);
            entity.AddEmployee(employee.EmployeeId, employee.EmployeeName, employee.EmployeeNumber, employee.RankId, employee.RankName, updateInput.Category, employee.DailyAllowanceRate, employee.TicketClass);
        }

        foreach (var toUpdate in newEmployeeIds.Intersect(currentEmployeeIds))
        {
            var employee = employees.First(e => e.EmployeeId == toUpdate);
            entity.UpdateEmployee(employee.EmployeeId, employee.EmployeeName, employee.EmployeeNumber, employee.RankId, employee.RankName, updateInput.Category, employee.DailyAllowanceRate, employee.TicketClass);
        }
    }

    private async Task<TravelRequest?> FindExistingTrainingRequestAsync(Guid trainingCourseId, Guid? tenantId)
    {
        var query = await GetQueryWithDetailsAsync();

        return await AsyncExecuter.FirstOrDefaultAsync(query.Where(x =>
            x.SourceSystem == "Training" &&
            x.SourceTrainingCourseId == trainingCourseId &&
            (!tenantId.HasValue || x.SourceTenantId == tenantId.Value)));
    }

    private async Task<(decimal Amount, TicketClass TicketClass)> GetDailyAllowanceRateAsync(Guid? rankId, AllowanceCategory category)
    {
        if (!rankId.HasValue)
        {
            return (0m, TicketClass.Economy);
        }

        var rates = await _allowanceRateRepository.GetListAsync(rate =>
            rate.IsActive &&
            rate.AllowanceType == AllowanceType.Daily &&
            rate.RankId == rankId.Value &&
            rate.Category == category);
        var rate = rates.FirstOrDefault();

        return (rate?.Amount ?? 0m, rate?.TicketClass ?? TicketClass.Economy);
    }

    private static List<string> NormalizeEmployeeNumbers(IEnumerable<string> employeeNumbers)
    {
        return employeeNumbers
            .Where(number => !number.IsNullOrWhiteSpace())
            .Select(number => number.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildTrainingRequestTitle(string courseName)
    {
        var title = $"تعليمات سفر - {courseName}";
        return title.Length <= TravelRequestConsts.MaxTitleLength
            ? title
            : title[..TravelRequestConsts.MaxTitleLength];
    }

    private static string BuildMissingEmployeeWarning(IReadOnlyCollection<string> missingNumbers)
    {
        if (missingNumbers.Count == 0)
        {
            return string.Empty;
        }

        var warning = $"الأرقام الوظيفية التالية لم يتم العثور على بياناتها في النظام: {string.Join(", ", missingNumbers)}";
        return warning.Length <= TravelRequestConsts.MaxIntegrationWarningsLength
            ? warning
            : warning[..TravelRequestConsts.MaxIntegrationWarningsLength];
    }

    private static List<string> ParseMissingEmployeeNumbers(string integrationWarnings)
    {
        const string prefix = "الأرقام الوظيفية التالية لم يتم العثور على بياناتها في النظام:";
        if (integrationWarnings.IsNullOrWhiteSpace() || !integrationWarnings.StartsWith(prefix, StringComparison.Ordinal))
        {
            return [];
        }

        return integrationWarnings[prefix.Length..]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static TrainingTravelEmployeeResultDto BuildTrainingEmployeeResult(
        TravelRequest request,
        TravelRequestAllowanceDetail detail)
    {
        var employee = request.Employees.FirstOrDefault(x => x.EmployeeId == detail.EmployeeId);
        var ticketAmount = CalculateTicketAmountForEmployee(request, employee);
        var visaAmount = request.RequiresVisa ? request.VisaCostPerEmployee : 0m;
        var healthInsuranceAmount = request.RequiresTravelInsurance ? request.TravelInsuranceCostPerEmployee : 0m;
        var allowanceTotal = detail.NetTotal;

        var payments = new List<TrainingTravelPaymentDto>
        {
            new()
            {
                Type = "Ticket",
                Label = "تذكرة سفر",
                FundingSourceVoteCode = GetFundingSourceVoteCode(request.TicketFundingSourceVoteCode, request),
                Amount = ticketAmount
            },
            new()
            {
                Type = "Visa",
                Label = "تأشيرة",
                FundingSourceVoteCode = GetFundingSourceVoteCode(request.VisaFundingSourceVoteCode, request),
                Amount = visaAmount
            },
            new()
            {
                Type = "HealthInsurance",
                Label = "تأمين صحي",
                FundingSourceVoteCode = GetFundingSourceVoteCode(request.HealthInsuranceFundingSourceVoteCode, request),
                Amount = healthInsuranceAmount
            },
            new()
            {
                Type = "DailyAllowance",
                Label = "علاوة سفر",
                FundingSourceVoteCode = GetFundingSourceVoteCode(request.DailyAllowanceFundingSourceVoteCode, request),
                Amount = detail.OverseasTotal
            },
            new()
            {
                Type = "ClothingAllowance",
                Label = "علاوة ملابس",
                FundingSourceVoteCode = GetFundingSourceVoteCode(request.ClothingAllowanceFundingSourceVoteCode, request),
                Amount = detail.ClothingAmount
            }
        };

        if (detail.DeductionAmount > 0)
        {
            payments.Add(new TrainingTravelPaymentDto
            {
                Type = "Deduction",
                Label = "خصومات",
                FundingSourceVoteCode = GetFundingSourceVoteCode(request.DailyAllowanceFundingSourceVoteCode, request),
                Amount = -detail.DeductionAmount
            });
        }

        return new TrainingTravelEmployeeResultDto
        {
            PersonnelId = detail.EmployeeId,
            EmployeeNumber = detail.EmployeeNumber,
            EmployeeName = detail.EmployeeName,
            RankName = detail.RankName,
            TicketAmount = ticketAmount,
            VisaAmount = visaAmount,
            HealthInsuranceAmount = healthInsuranceAmount,
            TravelAllowanceAmount = detail.OverseasTotal,
            ClothingAllowanceAmount = detail.ClothingAmount,
            DeductionAmount = detail.DeductionAmount,
            AllowanceTotal = allowanceTotal,
            Payments = payments,
            TotalAmount = ticketAmount + visaAmount + healthInsuranceAmount + allowanceTotal
        };
    }

    private static decimal CalculateTicketAmountForEmployee(
        TravelRequest request,
        TravelRequestEmployee? employee)
    {
        var selectedOfferTotals = request.FlightOffers
            .Where(x => x.IsSelected)
            .GroupBy(x => x.TicketClass)
            .ToDictionary(x => x.Key, x => x.Sum(offer => offer.Price));

        if (selectedOfferTotals.Count == 0 || employee == null)
        {
            return request.TicketCostPerEmployee;
        }

        return selectedOfferTotals.TryGetValue(employee.TicketClass, out var amount)
            ? amount
            : 0m;
    }

    private static string GetFundingSourceVoteCode(string specificVoteCode, TravelRequest request)
    {
        return string.IsNullOrWhiteSpace(specificVoteCode)
            ? request.FundingSourceVoteCode
            : specificVoteCode;
    }

    private async Task<Dictionary<string, string>> GetActiveFundingSourceVoteCodesAsync()
    {
        var rules = await _fundingSourceVoteRuleRepository.GetListAsync(rule => rule.IsActive);

        return rules
            .GroupBy(rule => rule.PaymentType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Last().FundingSourceVoteCode,
                StringComparer.OrdinalIgnoreCase);
    }

    private static string GetFundingSourceVoteCode(
        IReadOnlyDictionary<string, string> fundingSourceVoteCodes,
        string paymentType)
    {
        return fundingSourceVoteCodes.TryGetValue(paymentType, out var code)
            ? code
            : string.Empty;
    }

    private static string GetDefaultFundingSourceVoteCode(IReadOnlyDictionary<string, string> fundingSourceVoteCodes)
    {
        var dailyAllowanceVoteCode = GetFundingSourceVoteCode(fundingSourceVoteCodes, FundingSourceVotePaymentType.DailyAllowance);

        return string.IsNullOrWhiteSpace(dailyAllowanceVoteCode)
            ? fundingSourceVoteCodes.Values.FirstOrDefault() ?? string.Empty
            : dailyAllowanceVoteCode;
    }

    private static List<TravelRequestEmployeeInputDto> NormalizeEmployees(CreateUpdateTravelRequestDto input)
    {
        if (input.Employees.Count > 0)
        {
            return input.Employees
                .GroupBy(e => e.EmployeeId)
                .Select(g => g.Last())
                .ToList();
        }

        return input.EmployeeIds
            .Distinct()
            .Select(id => new TravelRequestEmployeeInputDto
            {
                EmployeeId = id,
                EmployeeName = id.ToString(),
                EmployeeNumber = id.ToString(),
                Category = AllowanceCategory.A,
                DailyAllowanceRate = input.DailyAllowanceRate,
                TicketClass = TicketClass.Economy
            })
            .ToList();
    }

    private static DateTime ResolveEndDate(DateTime startDate, DateTime? endDate)
    {
        return endDate?.Date ?? startDate.Date.AddDays(59);
    }

    private static AllowanceSummaryDto BuildAllowanceSummary(AllowanceCalculationResult calculation)
    {
        return new AllowanceSummaryDto
        {
            TotalDays = calculation.TotalDays,
            DailyBaseRate = calculation.DailyBaseRate,
            AccommodationMultiplier = calculation.AccommodationMultiplier,
            OverseasTotal = calculation.OverseasTotal,
            OverseasSegments = calculation.OverseasSegments,
            EmployeeAllowances = calculation.EmployeeCalculations.Select(employee => new AllowanceEmployeeSummaryDto
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.EmployeeName,
                EmployeeNumber = employee.EmployeeNumber,
                RankName = employee.RankName,
                Category = employee.Category,
                DailyRate = employee.DailyRate,
                AccommodationPaymentPercentage = employee.AccommodationPaymentPercentage,
                OverseasTotal = employee.OverseasTotal,
                ClothingAmount = employee.ClothingAmount,
                ClothingEligible = employee.ClothingEligible,
                ClothingCalculationNote = employee.ClothingCalculationNote,
                DeductionAmount = employee.DeductionAmount,
                NetTotal = employee.NetTotal,
                HasMatchingRule = employee.HasMatchingRule,
                Segments = employee.Segments
            }).ToList(),
            ClothingAmount = calculation.ClothingAmount,
            ClothingEligible = calculation.ClothingEligible,
            ClothingCalculationNote = calculation.ClothingCalculationNote,
            DeductionAmount = calculation.DeductionAmount,
            NetTotal = calculation.NetTotal,
            CalculatedAt = calculation.CalculatedAt
        };
    }

    private AllowanceSnapshot BuildAllowanceSnapshot(AllowanceCalculationResult calculation)
    {
        return new AllowanceSnapshot(
            calculation.OverseasTotal,
            calculation.ClothingAmount,
            calculation.DeductionAmount,
            calculation.CalculatedAt);
    }

    private List<TravelRequestAllowanceDetail> BuildAllowanceDetails(
        TravelRequest request,
        AllowanceCalculationResult calculation)
    {
        return calculation.EmployeeCalculations.Select(employee => new TravelRequestAllowanceDetail(
            Guid.NewGuid(),
            request.Id,
            employee.EmployeeId,
            employee.EmployeeName,
            employee.EmployeeNumber,
            employee.RankName,
            request.Category,
            employee.DailyRate,
            calculation.TotalDays,
            employee.AccommodationPaymentPercentage,
            employee.OverseasTotal,
            employee.ClothingAmount,
            employee.DeductionAmount,
            employee.HasMatchingRule,
            JsonSerializer.Serialize(employee.Segments),
            employee.ClothingCalculationNote,
            calculation.CalculatedAt)).ToList();
    }

    private async Task RecordClothingAllowanceHistoryAsync(
        TravelRequest request,
        AllowanceCalculationResult calculation)
    {
        foreach (var employee in calculation.EmployeeCalculations.Where(x => x.ClothingAmount > 0))
        {
            var alreadyRecorded = await _clothingHistoryRepository.AnyAsync(x =>
                x.TravelRequestId == request.Id &&
                x.EmployeeId == employee.EmployeeId);

            if (alreadyRecorded)
            {
                continue;
            }

            await _clothingHistoryRepository.InsertAsync(new EmployeeClothingHistory(
                Guid.NewGuid(),
                employee.EmployeeId,
                request.Id,
                employee.ClothingAmount,
                calculation.CalculatedAt,
                employee.ClothingIsFullPayment,
                employee.ClothingAllowanceRuleId));
        }
    }

    private static TravelCostSummaryDto BuildTravelCostSummary(
        TravelRequest request,
        decimal allowancesTotal,
        bool isAllowanceCalculated)
    {
        var employeeCount = request.Employees.Count;
        var ticketsTotal = CalculateTicketsTotal(request);
        var ticketCostPerEmployee = employeeCount == 0 ? 0m : ticketsTotal / employeeCount;
        var visaTotal = request.RequiresVisa ? employeeCount * request.VisaCostPerEmployee : 0m;
        var insuranceTotal = request.RequiresTravelInsurance ? employeeCount * request.TravelInsuranceCostPerEmployee : 0m;

        return new TravelCostSummaryDto
        {
            EmployeeCount = employeeCount,
            TicketCostPerEmployee = ticketCostPerEmployee,
            TicketsTotal = ticketsTotal,
            VisaCostPerEmployee = request.RequiresVisa ? request.VisaCostPerEmployee : 0m,
            VisaTotal = visaTotal,
            TravelInsuranceCostPerEmployee = request.RequiresTravelInsurance ? request.TravelInsuranceCostPerEmployee : 0m,
            TravelInsuranceTotal = insuranceTotal,
            AllowancesTotal = allowancesTotal,
            GrandTotal = ticketsTotal + visaTotal + insuranceTotal + allowancesTotal,
            IsAllowanceCalculated = isAllowanceCalculated
        };
    }

    private static decimal CalculateTicketsTotal(TravelRequest request)
    {
        var selectedOfferTotals = request.FlightOffers
            .Where(x => x.IsSelected)
            .GroupBy(x => x.TicketClass)
            .ToDictionary(x => x.Key, x => x.Sum(offer => offer.Price));

        if (selectedOfferTotals.Count == 0)
        {
            return request.Employees.Count * request.TicketCostPerEmployee;
        }

        return request.Employees.Sum(employee =>
            selectedOfferTotals.TryGetValue(employee.TicketClass, out var amount)
                ? amount
                : 0m);
    }

    private static List<TicketClass> GetRequiredTicketClasses(TravelRequest request)
    {
        var required = request.Employees
            .Select(x => x.TicketClass)
            .Distinct()
            .ToList();

        return required.Count == 0
            ? [TicketClass.Economy]
            : required;
    }

    private static Dictionary<TicketClass, (Guid DepartureOfferId, Guid ReturnOfferId)> NormalizeFlightSelections(
        SelectFlightOffersDto input,
        TravelRequest request)
    {
        if (input.Selections.Count > 0)
        {
            return input.Selections
                .GroupBy(x => x.TicketClass)
                .ToDictionary(
                    x => x.Key,
                    x =>
                    {
                        var last = x.Last();
                        return (last.DepartureFlightOfferId, last.ReturnFlightOfferId);
                    });
        }

        return GetRequiredTicketClasses(request)
            .ToDictionary(
                ticketClass => ticketClass,
                _ => (input.DepartureFlightOfferId, input.ReturnFlightOfferId));
    }

    private void EnsureSelectedFlightDatesAreValid(
        TravelRequest request,
        IReadOnlyDictionary<TicketClass, (Guid DepartureOfferId, Guid ReturnOfferId)> selections)
    {
        foreach (var selection in selections)
        {
            var departureOffer = request.FlightOffers.FirstOrDefault(x =>
                x.Id == selection.Value.DepartureOfferId &&
                x.Direction == FlightDirection.Departure &&
                x.TicketClass == selection.Key);
            var returnOffer = request.FlightOffers.FirstOrDefault(x =>
                x.Id == selection.Value.ReturnOfferId &&
                x.Direction == FlightDirection.Return &&
                x.TicketClass == selection.Key);

            if (departureOffer == null || returnOffer == null)
            {
                throw new BusinessException(TravelErrorCodes.FlightOfferNotFound);
            }

            if (returnOffer.ArrivalTime.Date < departureOffer.DepartureTime.Date)
            {
                throw new UserFriendlyException(L["InvalidTicketTravelDates"]);
            }
        }
    }

    private void EnsureFlightOffersAreValidForTravelDates(
        TravelRequest request,
        IEnumerable<TravelFlightOffer> offers)
    {
        foreach (var offer in offers)
        {
            if (offer.ArrivalTime < offer.DepartureTime)
            {
                throw new UserFriendlyException(L["InvalidTicketTravelDates"]);
            }

            if (offer.Direction == FlightDirection.Departure &&
                offer.DepartureTime.Date > request.StartDate.Date)
            {
                throw new UserFriendlyException(L["InvalidTicketTravelDates"]);
            }

            if (offer.Direction == FlightDirection.Return &&
                offer.DepartureTime.Date < request.EndDate.Date)
            {
                throw new UserFriendlyException(L["InvalidTicketTravelDates"]);
            }
        }
    }

    private void EnsureTicketDatesAreValidForCalculation(TravelRequest request)
    {
        if (request.TicketDepartureTime.HasValue &&
            request.TicketArrivalTime.HasValue &&
            request.TicketArrivalTime.Value.Date < request.TicketDepartureTime.Value.Date)
        {
            throw new UserFriendlyException(L["InvalidTicketTravelDates"]);
        }
    }

    private async Task<List<string>> GetAllowancePreviewWarningsAsync(
        CreateUpdateTravelRequestDto input,
        List<TravelRequestEmployeeInputDto> employees)
    {
        var warnings = new List<string>();
        var hasRule = await _allowanceRuleRepository.AnyAsync(rule =>
            rule.IsActive &&
            rule.AllowanceType == AllowanceType.Daily &&
            (rule.TravelTypeDefinitionId == null || rule.TravelTypeDefinitionId == input.TravelTypeDefinitionId) &&
            (rule.Category == null || rule.Category == input.Category));

        if (!hasRule)
        {
            warnings.Add(L["NoMatchingDailyAllowanceRule"]);
        }

        foreach (var employee in employees.Where(employee => employee.DailyAllowanceRate <= 0))
        {
            warnings.Add(L["MissingDailyAllowanceRateForEmployee", employee.EmployeeName]);
        }

        if (employees.Count == 0)
        {
            warnings.Add(L["NoEmployeesSelected"]);
        }

        return warnings.Distinct().ToList();
    }

    private static void EnsureTravelOfficeCanSendOffers(TravelRequest request)
    {
        var canSendOffers =
            request.Status == RequestStatus.AtTravelOffice ||
            request.Status == RequestStatus.FlightSelection ||
            request.Status == RequestStatus.VisaCheck ||
            request.Status == RequestStatus.InsuranceCheck ||
            request.Status == RequestStatus.PendingApproval ||
            request.Status == RequestStatus.Approved;

        if (!canSendOffers ||
            request.SelectedDepartureFlightOfferId.HasValue ||
            request.SelectedReturnFlightOfferId.HasValue)
        {
            throw new BusinessException(TravelErrorCodes.InvalidStatusTransition)
                .WithData("Current", request.Status)
                .WithData("Target", RequestStatus.AtTravelOffice);
        }
    }

    private void EnsureEmployeeBookingDocumentsAreComplete(TravelRequest request, ConfirmTicketBookingDto input)
    {
        var requestedEmployeeIds = request.Employees.Select(x => x.EmployeeId).Distinct().ToList();
        var inputByEmployee = input.EmployeeDocuments
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(x => x.Key, x => x.Last());

        if (requestedEmployeeIds.Count == 0 ||
            requestedEmployeeIds.Any(employeeId => !inputByEmployee.ContainsKey(employeeId)))
        {
            throw new UserFriendlyException(L["BookingDocumentsRequiredForAllEmployees"]);
        }

        foreach (var employeeId in requestedEmployeeIds)
        {
            var document = inputByEmployee[employeeId];

            if (document.TicketNumber.IsNullOrWhiteSpace() ||
                document.Pnr.IsNullOrWhiteSpace() ||
                document.TicketFileName.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException(L["TicketBookingDetailsRequiredForAllEmployees"]);
            }

            if (request.RequiresVisa && document.VisaFileName.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException(L["VisaDocumentRequiredForAllEmployees"]);
            }

            if (request.RequiresTravelInsurance && document.InsuranceFileName.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException(L["InsuranceDocumentRequiredForAllEmployees"]);
            }
        }
    }
}
