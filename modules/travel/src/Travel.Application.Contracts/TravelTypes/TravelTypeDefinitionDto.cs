using System;
using Volo.Abp.Application.Dtos;

namespace Travel.TravelTypes;

public class TravelTypeDefinitionDto : FullAuditedEntityDto<Guid>
{
    public int Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
