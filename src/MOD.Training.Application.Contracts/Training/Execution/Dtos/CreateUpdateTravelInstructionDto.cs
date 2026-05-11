using System;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Execution.Dtos;

/// <summary>
/// Upsert input — exactly one of <c>CasualCourseId</c> / <c>SessionId</c> set
/// (validated server-side; rejected via <c>Training:TravelInstruction:OnePolymorphicParentRequired</c>).
/// </summary>
public class CreateUpdateTravelInstructionDto
{
    public Guid? CasualCourseId { get; set; }
    public Guid? SessionId { get; set; }

    [Required]
    public DateTime DepartureDate { get; set; }

    [Required]
    public DateTime ArrivalDate { get; set; }

    [Required]
    public DateTime ReturnDate { get; set; }

    [Required]
    public DateTime ArrivalBackDate { get; set; }

    public bool VisaRequired { get; set; }

    [MaxLength(500)]
    public string? VisaNotes { get; set; }

    public bool InsuranceArranged { get; set; }

    [MaxLength(200)]
    public string? InsuranceProvider { get; set; }

    public bool TicketsBooked { get; set; }

    [MaxLength(100)]
    public string? TicketReference { get; set; }

    public int? OverrideTravelDays { get; set; }
}
