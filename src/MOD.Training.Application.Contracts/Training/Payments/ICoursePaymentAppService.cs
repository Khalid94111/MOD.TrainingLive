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

    /// <summary>Confirms the course-fee invoice after validating its parent and provider.</summary>
    Task<CoursePaymentDto> ConfirmAsync(Guid id);
}
