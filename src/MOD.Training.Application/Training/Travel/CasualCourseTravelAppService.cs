using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MOD.Training.Training.CasualCourses;
using MOD.Training.Training.Enums;
using MOD.Training.Training.Finance;
using MOD.Training.Training.Hr;
using MOD.Training.Training.Managers;
using MOD.Training.Training.Permissions;
using MOD.Training.Training.Travel.Dtos;
using MOD.Training.Training.Travel.Integration;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MOD.Training.Training.Travel;

/// <summary>
/// Hands an approved external-international ad-hoc course to the Travel module.
/// The browser supplies only the course id; all integration data is resolved server-side.
/// </summary>
[Authorize(TrainingExecutionPermissions.TravelRequests.Default)]
[RemoteService(IsEnabled = false)]
public class CasualCourseTravelAppService(
    IRepository<CasualCourse, Guid> courseRepo,
    IRepository<CasualCourseNomination, Guid> nominationRepo,
    IRepository<Employee, Guid> employeeRepo,
    IRepository<PriceQuote, Guid> priceQuoteRepo,
    IRepository<GeographicalLocation, Guid> locationRepo,
    IRepository<FinancialItem, Guid> financialItemRepo,
    CourseNameResolver courseNameResolver,
    ITrainingTravelGateway travelGateway,
    SessionTravelPaymentSynchronizer paymentSynchronizer)
    : ApplicationService, ICasualCourseTravelAppService
{
    private static readonly FinancialItemType[] RequiredFinancialItemTypes =
    [
        FinancialItemType.Ticket,
        FinancialItemType.Visa,
        FinancialItemType.Insurance,
        FinancialItemType.Allowance,
        FinancialItemType.Clothing
    ];

    public async Task<CasualCourseTravelDto> GetAsync(Guid casualCourseId)
    {
        var context = await BuildIntegrationContextAsync(casualCourseId);
        var gatewayResult = await travelGateway.GetByTrainingCourseAsync(casualCourseId);
        return MapResult(context, gatewayResult);
    }

    public async Task<CasualCourseTravelDto> RefreshAsync(Guid casualCourseId)
    {
        var context = await BuildIntegrationContextAsync(casualCourseId);
        var gatewayResult = await travelGateway.GetByTrainingCourseAsync(casualCourseId);
        await paymentSynchronizer.SyncCasualCourseAsync(casualCourseId, gatewayResult);
        return MapResult(context, gatewayResult);
    }

    [Authorize(TrainingExecutionPermissions.TravelRequests.Send)]
    public async Task<CasualCourseTravelDto> SendAsync(Guid casualCourseId)
    {
        var context = await BuildIntegrationContextAsync(casualCourseId);
        if (context.BlockingReasons.Count != 0)
        {
            throw new UserFriendlyException(string.Join(Environment.NewLine, context.BlockingReasons));
        }

        var result = await travelGateway.CreateFromTrainingAsync(context.Request);
        if (!result.IsAvailable)
        {
            throw new BusinessException("Training:SessionTravel:IntegrationUnavailable");
        }

        await paymentSynchronizer.SyncCasualCourseAsync(casualCourseId, result);
        return MapResult(context, result);
    }

    private async Task<IntegrationContext> BuildIntegrationContextAsync(Guid casualCourseId)
    {
        var course = await courseRepo.GetAsync(casualCourseId);
        if (course.CourseType != CourseType.ExternalInternational)
        {
            throw new BusinessException("Training:CasualCourseTravel:InternationalOnly");
        }

        var blockers = new List<string>();
        if (course.Status != CasualCourseStatus.THApproved)
        {
            blockers.Add(L["Training:CasualCourseTravel:ApprovalRequired"]);
        }
        if (!course.SelectedPriceQuoteId.HasValue)
        {
            blockers.Add(L["Training:SessionTravel:WinningQuoteRequired"]);
        }
        if (!course.ActualStartDate.HasValue || !course.ActualEndDate.HasValue)
        {
            blockers.Add(L["Training:SessionTravel:ConfirmedDatesRequired"]);
        }

        PriceQuote? quote = null;
        GeographicalLocation? country = null;
        GeographicalLocation? city = null;
        if (course.SelectedPriceQuoteId.HasValue)
        {
            quote = await priceQuoteRepo.FindAsync(course.SelectedPriceQuoteId.Value);
            if (quote?.CountryId.HasValue == true)
            {
                country = await locationRepo.FindAsync(quote.CountryId.Value);
            }
            if (quote?.CityId.HasValue == true)
            {
                city = await locationRepo.FindAsync(quote.CityId.Value);
            }
        }
        if (country == null || city == null)
        {
            blockers.Add(L["Training:SessionTravel:DestinationRequired"]);
        }

        var nominationQuery = await nominationRepo.GetQueryableAsync();
        var nominations = await AsyncExecuter.ToListAsync(
            nominationQuery.Where(x => x.CasualCourseId == casualCourseId));
        if (nominations.Count == 0)
        {
            blockers.Add(L["Training:SessionTravel:NomineesRequired"]);
        }

        var employeeIds = nominations.Select(x => x.EmployeeId).Distinct().ToList();
        var employeeQuery = await employeeRepo.GetQueryableAsync();
        var employees = employeeIds.Count == 0
            ? []
            : await AsyncExecuter.ToListAsync(employeeQuery.Where(x => employeeIds.Contains(x.Id)));
        var employeeNumbers = employees
            .Select(x => x.ServiceNumber?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (employeeNumbers.Count != nominations.Count)
        {
            blockers.Add(L["Training:SessionTravel:EmployeeNumbersRequired"]);
        }

        var financialQuery = await financialItemRepo.GetQueryableAsync();
        var financialItems = await AsyncExecuter.ToListAsync(financialQuery.Where(x =>
            x.IsActive && x.ItemType.HasValue && RequiredFinancialItemTypes.Contains(x.ItemType.Value)));
        var voteCodes = financialItems
            .Where(x => !string.IsNullOrWhiteSpace(x.VoteCode))
            .GroupBy(x => x.ItemType!.Value)
            .ToDictionary(x => x.Key, x => x.Last().VoteCode.Trim());
        foreach (var type in RequiredFinancialItemTypes.Where(type => !voteCodes.ContainsKey(type)))
        {
            blockers.Add(L["Training:SessionTravel:VoteCodeRequired", type.ToString()]);
        }

        var courseName = await courseNameResolver.ResolveAsync(course.TenantCourseId);
        if (courseName == null || string.IsNullOrWhiteSpace(courseName.NameAr))
        {
            blockers.Add(L["Training:SessionTravel:CourseNameRequired"]);
        }

        var request = new TrainingTravelGatewayRequest
        {
            TrainingCourseId = course.Id,
            TenantId = CurrentTenant.Id,
            TenantName = CurrentTenant.Name ?? string.Empty,
            CourseName = courseName?.NameAr ?? string.Empty,
            StartDate = course.ActualStartDate ?? default,
            EndDate = course.ActualEndDate ?? default,
            DestinationCountry = country?.ArabicName ?? string.Empty,
            DestinationCity = city?.ArabicName ?? string.Empty,
            TicketFundingSourceVoteCode = voteCodes.GetValueOrDefault(FinancialItemType.Ticket) ?? string.Empty,
            VisaFundingSourceVoteCode = voteCodes.GetValueOrDefault(FinancialItemType.Visa) ?? string.Empty,
            HealthInsuranceFundingSourceVoteCode = voteCodes.GetValueOrDefault(FinancialItemType.Insurance) ?? string.Empty,
            DailyAllowanceFundingSourceVoteCode = voteCodes.GetValueOrDefault(FinancialItemType.Allowance) ?? string.Empty,
            ClothingAllowanceFundingSourceVoteCode = voteCodes.GetValueOrDefault(FinancialItemType.Clothing) ?? string.Empty,
            EmployeeNumbers = employeeNumbers
        };

        return new IntegrationContext(request, blockers);
    }

    private static CasualCourseTravelDto MapResult(
        IntegrationContext context,
        TrainingTravelGatewayResult result)
    {
        return new CasualCourseTravelDto
        {
            CasualCourseId = context.Request.TrainingCourseId,
            TravelRequestId = result.TravelRequestId,
            IntegrationAvailable = result.IsAvailable,
            IsFound = result.IsFound,
            IsCompleted = result.IsCompleted,
            CanSend = result.IsAvailable && !result.IsFound && context.BlockingReasons.Count == 0,
            StatusCode = result.StatusCode,
            WarningMessage = result.WarningMessage,
            BlockingReasons = context.BlockingReasons,
            CompletedAt = result.CompletedAt,
            CalculatedDays = result.CalculatedDays,
            EmployeeCount = result.EmployeeCount,
            Currency = result.Currency,
            TicketsTotal = result.TicketsTotal,
            VisaTotal = result.VisaTotal,
            HealthInsuranceTotal = result.HealthInsuranceTotal,
            TravelAllowanceTotal = result.TravelAllowanceTotal,
            ClothingAllowanceTotal = result.ClothingAllowanceTotal,
            DeductionTotal = result.DeductionTotal,
            GrandTotal = result.GrandTotal
        };
    }

    private sealed record IntegrationContext(
        TrainingTravelGatewayRequest Request,
        List<string> BlockingReasons);
}
