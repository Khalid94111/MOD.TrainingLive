using System;
using System.Collections.Generic;
using System.Linq;
using Travel.Allowances;
using Travel.TravelRequests.Events;
using Travel.TravelTypes;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Travel.TravelRequests;

public class TravelRequest : FullAuditedAggregateRoot<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid TravelTypeDefinitionId { get; private set; }
    public RequestStatus Status { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public string DestinationCountry { get; private set; } = string.Empty;
    public string DestinationCity { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public AllowanceCategory Category { get; private set; }
    public decimal DailyAllowanceRate { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public bool NeedsPermission { get; private set; }
    public bool NeedsAwareness { get; private set; }
    public bool HasTicketCompensation { get; private set; }
    public bool NeedsTransportation { get; private set; }
    public bool IncludesAccommodation { get; private set; }
    public bool UseHighestAllowance { get; private set; }
    public string AllowanceTiers { get; private set; } = string.Empty;
    public Guid RequesterId { get; private set; }
    public string RequesterName { get; private set; } = string.Empty;
    public bool RequiresVisa { get; private set; }
    public decimal VisaCostPerEmployee { get; private set; }
    public bool RequiresTravelInsurance { get; private set; }
    public decimal TravelInsuranceCostPerEmployee { get; private set; }
    public string TicketAirline { get; private set; } = string.Empty;
    public string TicketFlightNumber { get; private set; } = string.Empty;
    public DateTime? TicketDepartureTime { get; private set; }
    public DateTime? TicketArrivalTime { get; private set; }
    public decimal TicketCostPerEmployee { get; private set; }
    public string TravelOfficeNotes { get; private set; } = string.Empty;
    public Guid? SelectedDepartureFlightOfferId { get; private set; }
    public Guid? SelectedReturnFlightOfferId { get; private set; }
    public string SourceSystem { get; private set; } = string.Empty;
    public Guid? SourceTenantId { get; private set; }
    public Guid? SourceTrainingCourseId { get; private set; }
    public string SourceTrainingCourseName { get; private set; } = string.Empty;
    public string FundingSourceVoteCode { get; private set; } = string.Empty;
    public string TicketFundingSourceVoteCode { get; private set; } = string.Empty;
    public string VisaFundingSourceVoteCode { get; private set; } = string.Empty;
    public string HealthInsuranceFundingSourceVoteCode { get; private set; } = string.Empty;
    public string DailyAllowanceFundingSourceVoteCode { get; private set; } = string.Empty;
    public string ClothingAllowanceFundingSourceVoteCode { get; private set; } = string.Empty;
    public string IntegrationWarnings { get; private set; } = string.Empty;

    public ICollection<TravelRequestEmployee> Employees { get; private set; } = new List<TravelRequestEmployee>();
    public ICollection<TravelDocument> Documents { get; private set; } = new List<TravelDocument>();
    public ICollection<TravelFlightOffer> FlightOffers { get; private set; } = new List<TravelFlightOffer>();
    public ICollection<TravelEmployeeDocument> EmployeeDocuments { get; private set; } = new List<TravelEmployeeDocument>();
    public ICollection<TravelRequestAllowanceDetail> AllowanceDetails { get; private set; } = new List<TravelRequestAllowanceDetail>();
    public AllowanceSnapshot? AllowanceSnapshot { get; private set; }

    protected TravelRequest()
    {
    }

    public TravelRequest(
        Guid id,
        string title,
        string description,
        Guid travelTypeDefinitionId,
        DateTime startDate,
        DateTime endDate,
        string destinationCountry,
        string destinationCity,
        string department,
        AllowanceCategory category,
        decimal dailyAllowanceRate,
        string currency,
        bool needsPermission,
        bool needsAwareness,
        bool hasTicketCompensation,
        bool needsTransportation,
        bool includesAccommodation,
        bool useHighestAllowance,
        string allowanceTiers,
        Guid requesterId,
        string requesterName) : base(id)
    {
        Title = title;
        Description = description;
        TravelTypeDefinitionId = travelTypeDefinitionId;
        Status = RequestStatus.Draft;
        StartDate = startDate;
        EndDate = endDate;
        DestinationCountry = destinationCountry;
        DestinationCity = destinationCity;
        Department = department;
        Category = category;
        DailyAllowanceRate = dailyAllowanceRate;
        Currency = currency;
        NeedsPermission = needsPermission;
        NeedsAwareness = needsAwareness;
        HasTicketCompensation = hasTicketCompensation;
        NeedsTransportation = needsTransportation;
        IncludesAccommodation = includesAccommodation;
        UseHighestAllowance = useHighestAllowance;
        AllowanceTiers = allowanceTiers;
        RequesterId = requesterId;
        RequesterName = requesterName;

        AddLocalEvent(new TravelRequestCreatedEvent(this));
    }

    public void UpdateDetails(
        string title,
        string description,
        Guid travelTypeDefinitionId,
        DateTime startDate,
        DateTime endDate,
        string destinationCountry,
        string destinationCity,
        string department,
        AllowanceCategory category,
        decimal dailyAllowanceRate,
        string currency,
        bool needsPermission,
        bool needsAwareness,
        bool hasTicketCompensation,
        bool needsTransportation,
        bool includesAccommodation,
        bool useHighestAllowance,
        string allowanceTiers)
    {
        if (Status != RequestStatus.Draft && Status != RequestStatus.Returned)
        {
            throw new BusinessException(TravelErrorCodes.CanOnlyEditDraftOrReturned)
                .WithData("Status", Status);
        }

        Title = title;
        Description = description;
        TravelTypeDefinitionId = travelTypeDefinitionId;
        StartDate = startDate;
        EndDate = endDate;
        DestinationCountry = destinationCountry;
        DestinationCity = destinationCity;
        Department = department;
        Category = category;
        DailyAllowanceRate = dailyAllowanceRate;
        Currency = currency;
        NeedsPermission = needsPermission;
        NeedsAwareness = needsAwareness;
        HasTicketCompensation = hasTicketCompensation;
        NeedsTransportation = needsTransportation;
        IncludesAccommodation = includesAccommodation;
        UseHighestAllowance = useHighestAllowance;
        AllowanceTiers = allowanceTiers;
        ClearAllowanceSnapshot();
    }

    public void ChangeStatus(RequestStatus newStatus, string? reason = null)
    {
        Status = newStatus;
    }

    public void AddEmployee(Guid employeeId, string employeeName, string employeeNumber, Guid? rankId, string rankName, AllowanceCategory category, decimal dailyAllowanceRate, TicketClass ticketClass = TicketClass.Economy)
    {
        if (!Employees.Any(e => e.EmployeeId == employeeId))
        {
            Employees.Add(new TravelRequestEmployee(Guid.NewGuid(), Id, employeeId, employeeName, employeeNumber, rankId, rankName, category, dailyAllowanceRate, ticketClass));
            ClearAllowanceSnapshot();
        }
    }

    public void RemoveEmployee(Guid employeeId)
    {
        var employee = Employees.FirstOrDefault(e => e.EmployeeId == employeeId);
        if (employee != null)
        {
            Employees.Remove(employee);
            ClearAllowanceSnapshot();
        }
    }

    public void UpdateEmployee(Guid employeeId, string employeeName, string employeeNumber, Guid? rankId, string rankName, AllowanceCategory category, decimal dailyAllowanceRate, TicketClass ticketClass = TicketClass.Economy)
    {
        var employee = Employees.FirstOrDefault(e => e.EmployeeId == employeeId);
        employee?.Update(employeeName, employeeNumber, rankId, rankName, category, dailyAllowanceRate, ticketClass);
        ClearAllowanceSnapshot();
    }

    public void AddDocument(string name, string fileExtension, long fileSize, string blobName)
    {
        Documents.Add(new TravelDocument(Guid.NewGuid(), Id, name, fileExtension, fileSize, blobName));
    }

    public void SetTrainingSource(
        Guid trainingCourseId,
        string courseName,
        Guid? tenantId,
        string fundingSourceVoteCode,
        string ticketFundingSourceVoteCode,
        string visaFundingSourceVoteCode,
        string healthInsuranceFundingSourceVoteCode,
        string dailyAllowanceFundingSourceVoteCode,
        string clothingAllowanceFundingSourceVoteCode,
        string sourceSystem = "Training")
    {
        SourceSystem = sourceSystem ?? string.Empty;
        SourceTenantId = tenantId;
        SourceTrainingCourseId = trainingCourseId;
        SourceTrainingCourseName = courseName ?? string.Empty;
        SetFundingSourceVoteCodes(
            fundingSourceVoteCode,
            ticketFundingSourceVoteCode,
            visaFundingSourceVoteCode,
            healthInsuranceFundingSourceVoteCode,
            dailyAllowanceFundingSourceVoteCode,
            clothingAllowanceFundingSourceVoteCode);
    }

    public void SetFundingSourceVoteCodes(
        string fundingSourceVoteCode,
        string ticketFundingSourceVoteCode,
        string visaFundingSourceVoteCode,
        string healthInsuranceFundingSourceVoteCode,
        string dailyAllowanceFundingSourceVoteCode,
        string clothingAllowanceFundingSourceVoteCode)
    {
        FundingSourceVoteCode = fundingSourceVoteCode ?? string.Empty;
        TicketFundingSourceVoteCode = ticketFundingSourceVoteCode ?? string.Empty;
        VisaFundingSourceVoteCode = visaFundingSourceVoteCode ?? string.Empty;
        HealthInsuranceFundingSourceVoteCode = healthInsuranceFundingSourceVoteCode ?? string.Empty;
        DailyAllowanceFundingSourceVoteCode = dailyAllowanceFundingSourceVoteCode ?? string.Empty;
        ClothingAllowanceFundingSourceVoteCode = clothingAllowanceFundingSourceVoteCode ?? string.Empty;
    }

    public void SetIntegrationWarnings(string warnings)
    {
        IntegrationWarnings = warnings ?? string.Empty;
    }

    public void SetAllowanceSnapshot(AllowanceSnapshot snapshot)
    {
        AllowanceSnapshot = snapshot;
    }

    public void SetAllowanceSnapshot(AllowanceSnapshot snapshot, IEnumerable<TravelRequestAllowanceDetail> details)
    {
        AllowanceSnapshot = snapshot;
        AllowanceDetails.Clear();

        foreach (var detail in details)
        {
            AllowanceDetails.Add(detail);
        }
    }

    public void ClearAllowanceSnapshot()
    {
        AllowanceSnapshot = null;
        AllowanceDetails.Clear();
    }

    public void SetTravelOfficeDetails(
        bool requiresVisa,
        decimal visaCostPerEmployee,
        bool requiresTravelInsurance,
        decimal travelInsuranceCostPerEmployee,
        string ticketAirline,
        string ticketFlightNumber,
        DateTime? ticketDepartureTime,
        DateTime? ticketArrivalTime,
        decimal ticketCostPerEmployee,
        string travelOfficeNotes)
    {
        if (visaCostPerEmployee < 0 || travelInsuranceCostPerEmployee < 0 || ticketCostPerEmployee < 0)
        {
            throw new BusinessException(TravelErrorCodes.NegativeAmount);
        }

        RequiresVisa = requiresVisa;
        VisaCostPerEmployee = requiresVisa ? visaCostPerEmployee : 0m;
        RequiresTravelInsurance = requiresTravelInsurance;
        TravelInsuranceCostPerEmployee = requiresTravelInsurance ? travelInsuranceCostPerEmployee : 0m;
        TicketAirline = ticketAirline ?? string.Empty;
        TicketFlightNumber = ticketFlightNumber ?? string.Empty;
        TicketDepartureTime = ticketDepartureTime;
        TicketArrivalTime = ticketArrivalTime;
        TicketCostPerEmployee = ticketCostPerEmployee;
        TravelOfficeNotes = travelOfficeNotes ?? string.Empty;
        ClearAllowanceSnapshot();
    }

    public void ReplaceFlightOffers(IEnumerable<TravelFlightOffer> offers)
    {
        FlightOffers.Clear();
        SelectedDepartureFlightOfferId = null;
        SelectedReturnFlightOfferId = null;

        foreach (var offer in offers)
        {
            FlightOffers.Add(offer);
        }
    }

    public void SelectFlightOffers(Guid departureOfferId, Guid returnOfferId)
    {
        var departureOffer = FlightOffers.FirstOrDefault(x => x.Id == departureOfferId && x.Direction == FlightDirection.Departure);
        var returnOffer = FlightOffers.FirstOrDefault(x => x.Id == returnOfferId && x.Direction == FlightDirection.Return);

        if (departureOffer == null || returnOffer == null)
        {
            throw new BusinessException(TravelErrorCodes.FlightOfferNotFound);
        }

        foreach (var offer in FlightOffers)
        {
            offer.Unselect();
        }

        departureOffer.Select();
        returnOffer.Select();
        SelectedDepartureFlightOfferId = departureOfferId;
        SelectedReturnFlightOfferId = returnOfferId;
        TicketAirline = $"{departureOffer.Airline} / {returnOffer.Airline}";
        TicketFlightNumber = $"{departureOffer.FlightNumber} / {returnOffer.FlightNumber}";
        TicketDepartureTime = departureOffer.DepartureTime;
        TicketArrivalTime = returnOffer.ArrivalTime;
        TicketCostPerEmployee = departureOffer.Price + returnOffer.Price;
        ClearAllowanceSnapshot();
    }

    public void SelectFlightOffers(IReadOnlyDictionary<TicketClass, (Guid DepartureOfferId, Guid ReturnOfferId)> selections)
    {
        if (selections.Count == 0)
        {
            throw new BusinessException(TravelErrorCodes.FlightOfferNotFound);
        }

        foreach (var offer in FlightOffers)
        {
            offer.Unselect();
        }

        var selectedPairs = new List<(TicketClass TicketClass, TravelFlightOffer Departure, TravelFlightOffer Return)>();

        foreach (var selection in selections)
        {
            var ticketClass = selection.Key;
            var departureOffer = FlightOffers.FirstOrDefault(x =>
                x.Id == selection.Value.DepartureOfferId &&
                x.Direction == FlightDirection.Departure &&
                x.TicketClass == ticketClass);
            var returnOffer = FlightOffers.FirstOrDefault(x =>
                x.Id == selection.Value.ReturnOfferId &&
                x.Direction == FlightDirection.Return &&
                x.TicketClass == ticketClass);

            if (departureOffer == null || returnOffer == null)
            {
                throw new BusinessException(TravelErrorCodes.FlightOfferNotFound);
            }

            departureOffer.Select();
            returnOffer.Select();
            selectedPairs.Add((ticketClass, departureOffer, returnOffer));
        }

        var firstPair = selectedPairs.First();
        SelectedDepartureFlightOfferId = firstPair.Departure.Id;
        SelectedReturnFlightOfferId = firstPair.Return.Id;
        TicketAirline = string.Join(" | ", selectedPairs.Select(x => $"{x.TicketClass}: {x.Departure.Airline}/{x.Return.Airline}"));
        TicketFlightNumber = string.Join(" | ", selectedPairs.Select(x => $"{x.TicketClass}: {x.Departure.FlightNumber}/{x.Return.FlightNumber}"));
        TicketDepartureTime = selectedPairs.Min(x => x.Departure.DepartureTime);
        TicketArrivalTime = selectedPairs.Max(x => x.Return.ArrivalTime);

        var employeeCount = Employees.Count;
        if (employeeCount == 0)
        {
            TicketCostPerEmployee = selectedPairs.Sum(x => x.Departure.Price + x.Return.Price);
            ClearAllowanceSnapshot();
            return;
        }

        TicketCostPerEmployee = Employees.Sum(employee =>
        {
            var pair = selectedPairs.FirstOrDefault(x => x.TicketClass == employee.TicketClass);
            return pair.Departure == null ? 0m : pair.Departure.Price + pair.Return.Price;
        }) / employeeCount;
        ClearAllowanceSnapshot();
    }

    public void ReplaceEmployeeDocuments(IEnumerable<TravelEmployeeDocument> documents)
    {
        EmployeeDocuments.Clear();

        foreach (var document in documents)
        {
            EmployeeDocuments.Add(document);
        }
    }

    public int GetDurationInDays()
    {
        return (GetAllowanceEndDate() - GetAllowanceStartDate()).Days + 1;
    }

    public DateTime GetAllowanceStartDate()
    {
        return HasValidTicketAllowanceDates()
            ? TicketDepartureTime!.Value.Date
            : StartDate.Date;
    }

    public DateTime GetAllowanceEndDate()
    {
        return HasValidTicketAllowanceDates()
            ? TicketArrivalTime!.Value.Date
            : EndDate.Date;
    }

    private bool HasValidTicketAllowanceDates()
    {
        return TicketDepartureTime.HasValue &&
               TicketArrivalTime.HasValue &&
               TicketArrivalTime.Value.Date >= TicketDepartureTime.Value.Date;
    }
}
