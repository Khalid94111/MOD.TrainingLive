using System;
using Volo.Abp.Application.Dtos;

namespace MOD.Training.Training.HrIntegration.Dtos;

public class GeographicalLocationDto : EntityDto<Guid>
{
    public string ArabicName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public Guid? LocationParentId { get; set; }
}
