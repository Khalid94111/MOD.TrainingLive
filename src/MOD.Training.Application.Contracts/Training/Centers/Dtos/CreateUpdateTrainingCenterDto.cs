using System;
using System.ComponentModel.DataAnnotations;

namespace MOD.Training.Training.Centers.Dtos;

public class CreateUpdateTrainingCenterDto
{
    [Required]
    public Guid OrgUnitId { get; set; }

    [Required]
    [StringLength(256)]
    public string CenterNameAr { get; set; } = null!;

    [Required]
    [StringLength(256)]
    public string CenterNameEn { get; set; } = null!;

    [StringLength(500)]
    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;
}
