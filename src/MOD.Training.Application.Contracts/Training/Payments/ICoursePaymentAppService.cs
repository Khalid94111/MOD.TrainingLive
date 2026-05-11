using MOD.Training.Training.Payments.Dtos;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;

namespace MOD.Training.Training.Payments;

public interface ICoursePaymentAppService :
    ICrudAppService<
        CoursePaymentDto,
        Guid,
        CoursePaymentGetListInput,
        CreateUpdateCoursePaymentDto>
{
    /// <summary>Uploads the invoice PDF — replaces any prior file. PDF only, 25 MB max.</summary>
    Task<CoursePaymentDto> UploadInvoiceAsync(Guid id, IRemoteStreamContent file);

    /// <summary>Streams the invoice PDF back. Returns null if no invoice has been uploaded.</summary>
    Task<IRemoteStreamContent?> DownloadInvoiceAsync(Guid id);

    /// <summary>
    /// Confirms the payment. For casual courses, atomically fires the BudgetReallocationGenerator
    /// — if generation fails, the confirmation is rolled back. Returns the final payment + the
    /// number of reallocation rows produced (0 for session arm or scenario 1).
    /// </summary>
    Task<CoursePaymentConfirmResultDto> ConfirmAsync(Guid id);
}
