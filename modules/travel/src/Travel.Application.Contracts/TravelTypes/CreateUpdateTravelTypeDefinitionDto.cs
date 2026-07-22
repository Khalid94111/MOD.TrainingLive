using System.ComponentModel.DataAnnotations;

namespace Travel.TravelTypes;

public class CreateUpdateTravelTypeDefinitionDto
{
    [Range(1, int.MaxValue)]
    public int Code { get; set; }

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
