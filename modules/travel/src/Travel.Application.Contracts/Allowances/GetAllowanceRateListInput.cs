using System;
using Volo.Abp.Application.Dtos;

namespace Travel.Allowances;

public class GetAllowanceRateListInput : PagedAndSortedResultRequestDto
{
    public Guid? RankId { get; set; }
    public AllowanceCategory? Category { get; set; }
    public AllowanceType? AllowanceType { get; set; }
}
