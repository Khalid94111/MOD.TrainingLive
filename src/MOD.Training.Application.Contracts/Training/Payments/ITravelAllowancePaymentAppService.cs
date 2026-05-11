using MOD.Training.Training.Payments.Dtos;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MOD.Training.Training.Payments;

public interface ITravelAllowancePaymentAppService :
    ICrudAppService<
        TravelAllowancePaymentDto,
        Guid,
        TravelAllowancePaymentGetListInput,
        CreateUpdateTravelAllowancePaymentDto>
{
    Task<TravelAllowancePaymentDto> ConfirmAsync(Guid id);

    /// <summary>
    /// Returns the default per-nominee amounts that would be applied if a Draft payment were
    /// created right now for the given (course, nomination). Does not create anything.
    /// Used by the New-Payment dialog to preview the breakdown before Save.
    /// </summary>
    Task<TravelAllowancePaymentDefaultsDto> GetDefaultsAsync(Guid casualCourseId, Guid nominationId);
}
