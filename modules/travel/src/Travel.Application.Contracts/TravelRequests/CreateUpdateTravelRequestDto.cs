using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Travel.Allowances;
using Travel.TravelTypes;

namespace Travel.TravelRequests;

public class CreateUpdateTravelRequestDto
{
    [Required]
    [StringLength(TravelRequestConsts.MaxTitleLength)]
    public string Title { get; set; } = string.Empty;

    [StringLength(TravelRequestConsts.MaxDescriptionLength)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public Guid TravelTypeDefinitionId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    [StringLength(TravelRequestConsts.MaxCountryLength)]
    public string DestinationCountry { get; set; } = string.Empty;

    [Required]
    [StringLength(TravelRequestConsts.MaxCityLength)]
    public string DestinationCity { get; set; } = string.Empty;

    [StringLength(TravelRequestConsts.MaxDepartmentLength)]
    public string Department { get; set; } = string.Empty;

    public AllowanceCategory Category { get; set; } = AllowanceCategory.A;

    public decimal DailyAllowanceRate { get; set; }

    [StringLength(TravelRequestConsts.MaxCurrencyLength)]
    public string Currency { get; set; } = string.Empty;

    public bool NeedsPermission { get; set; }

    public bool NeedsAwareness { get; set; }

    public bool HasTicketCompensation { get; set; }

    public bool NeedsTransportation { get; set; }

    public bool IncludesAccommodation { get; set; }

    public bool UseHighestAllowance { get; set; }

    [StringLength(TravelRequestConsts.MaxAllowanceTiersLength)]
    public string AllowanceTiers { get; set; } = string.Empty;

    public List<Guid> EmployeeIds { get; set; } = new();

    public List<TravelRequestEmployeeInputDto> Employees { get; set; } = new();
}
