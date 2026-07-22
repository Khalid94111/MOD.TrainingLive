using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MOD.Training.Training.Travel.Integration;

/// <summary>
/// Anti-corruption boundary between Training and the Travel module. The Travel adapter maps
/// its public contracts to these Training-owned models once the module is installed.
/// </summary>
public interface ITrainingTravelGateway
{
    Task<TrainingTravelGatewayResult> GetByTrainingCourseAsync(Guid trainingCourseId);
    Task<TrainingTravelGatewayResult> CreateFromTrainingAsync(TrainingTravelGatewayRequest input);
}

public class TrainingTravelGatewayRequest
{
    public Guid TrainingCourseId { get; set; }
    public Guid? TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string DestinationCountry { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public string TicketFundingSourceVoteCode { get; set; } = string.Empty;
    public string VisaFundingSourceVoteCode { get; set; } = string.Empty;
    public string HealthInsuranceFundingSourceVoteCode { get; set; } = string.Empty;
    public string DailyAllowanceFundingSourceVoteCode { get; set; } = string.Empty;
    public string ClothingAllowanceFundingSourceVoteCode { get; set; } = string.Empty;
    public List<string> EmployeeNumbers { get; set; } = [];
}

public class TrainingTravelGatewayResult
{
    public bool IsAvailable { get; set; }
    public bool IsFound { get; set; }
    public bool IsCompleted { get; set; }
    public Guid? TravelRequestId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string WarningMessage { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public int CalculatedDays { get; set; }
    public int EmployeeCount { get; set; }
    public string Currency { get; set; } = "OMR";
    public decimal TicketsTotal { get; set; }
    public decimal VisaTotal { get; set; }
    public decimal HealthInsuranceTotal { get; set; }
    public decimal TravelAllowanceTotal { get; set; }
    public decimal ClothingAllowanceTotal { get; set; }
    public decimal DeductionTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public List<TrainingTravelEmployeeResult> Employees { get; set; } = [];
}

public class TrainingTravelEmployeeResult
{
    public Guid PersonnelId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public decimal TicketAmount { get; set; }
    public decimal VisaAmount { get; set; }
    public decimal HealthInsuranceAmount { get; set; }
    public decimal TravelAllowanceAmount { get; set; }
    public decimal ClothingAllowanceAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public List<TrainingTravelPaymentResult> Payments { get; set; } = [];
}

public class TrainingTravelPaymentResult
{
    public string TypeCode { get; set; } = string.Empty;
    public string FundingSourceVoteCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
