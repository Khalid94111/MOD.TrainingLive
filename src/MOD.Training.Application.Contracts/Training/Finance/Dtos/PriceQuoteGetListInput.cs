using MOD.Training.Training.Enums;
using System;
using Volo.Abp.Application.Dtos;
 
namespace MOD.Training.Training.Finance.Dtos;

public class PriceQuoteGetListInput : PagedAndSortedResultRequestDto
{
    public Guid? SessionId { get; set; }
    public Guid? ProviderId { get; set; }
    public ApprovalStatus? Status { get; set; }
}
