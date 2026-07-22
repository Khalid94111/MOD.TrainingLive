using System.Linq;
using System.Threading.Tasks;
using Travel.TravelRequests;
using Volo.Abp.DependencyInjection;

namespace MOD.Training.Training.Travel.Integration;

/// <summary>
/// Maps the public Travel module contract to the Training-owned integration boundary.
/// Keeping this mapping here prevents Travel types from leaking into the Training workflow.
/// </summary>
[Dependency(ReplaceServices = true)]
public class TravelTrainingGateway(ITravelRequestAppService travelRequests)
    : ITrainingTravelGateway, ITransientDependency
{
    public async Task<TrainingTravelGatewayResult> GetByTrainingCourseAsync(global::System.Guid trainingCourseId)
    {
        var result = await travelRequests.GetTrainingResultAsync(trainingCourseId);
        return MapResult(result);
    }

    public async Task<TrainingTravelGatewayResult> CreateFromTrainingAsync(
        TrainingTravelGatewayRequest input)
    {
        var created = await travelRequests.CreateFromTrainingAsync(new CreateTravelRequestFromTrainingDto
        {
            TrainingCourseId = input.TrainingCourseId,
            TenantId = input.TenantId,
            CourseName = input.CourseName,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            DestinationCountry = input.DestinationCountry,
            DestinationCity = input.DestinationCity,
            TicketFundingSourceVoteCode = input.TicketFundingSourceVoteCode,
            VisaFundingSourceVoteCode = input.VisaFundingSourceVoteCode,
            HealthInsuranceFundingSourceVoteCode = input.HealthInsuranceFundingSourceVoteCode,
            DailyAllowanceFundingSourceVoteCode = input.DailyAllowanceFundingSourceVoteCode,
            ClothingAllowanceFundingSourceVoteCode = input.ClothingAllowanceFundingSourceVoteCode,
            EmployeeNumbers = input.EmployeeNumbers
        });

        var result = await travelRequests.GetTrainingResultAsync(input.TrainingCourseId);
        if (!result.IsFound)
        {
            return new TrainingTravelGatewayResult
            {
                IsAvailable = true,
                IsFound = true,
                IsCompleted = false,
                TravelRequestId = created.TravelRequestId,
                StatusCode = created.Status.ToString(),
                WarningMessage = created.WarningMessage
            };
        }

        return MapResult(result);
    }

    public static TrainingTravelGatewayResult MapResult(TrainingTravelResultDto result)
        => new()
        {
            IsAvailable = true,
            IsFound = result.IsFound,
            IsCompleted = result.IsCompleted,
            TravelRequestId = result.TravelRequestId,
            StatusCode = result.Status?.ToString() ?? string.Empty,
            WarningMessage = result.WarningMessage,
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
            GrandTotal = result.GrandTotal,
            Employees = result.Employees.Select(employee => new TrainingTravelEmployeeResult
            {
                PersonnelId = employee.PersonnelId,
                EmployeeNumber = employee.EmployeeNumber,
                EmployeeName = employee.EmployeeName,
                TicketAmount = employee.TicketAmount,
                VisaAmount = employee.VisaAmount,
                HealthInsuranceAmount = employee.HealthInsuranceAmount,
                TravelAllowanceAmount = employee.TravelAllowanceAmount,
                ClothingAllowanceAmount = employee.ClothingAllowanceAmount,
                DeductionAmount = employee.DeductionAmount,
                Payments = employee.Payments.Select(payment => new TrainingTravelPaymentResult
                {
                    TypeCode = payment.Type,
                    FundingSourceVoteCode = payment.FundingSourceVoteCode,
                    Amount = payment.Amount
                }).ToList()
            }).ToList()
        };
}
