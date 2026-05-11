using Volo.Abp.BlobStoring;

namespace MOD.Training.Training.Payments.Storage;

/// <summary>
/// Phase 4B-β — typed BlobContainer for CoursePayment invoice PDFs. The container name pins
/// the on-disk subfolder under each tenant: <c>{Gtms.Files.Path}/{tenantId}/training-course-invoices</c>.
/// File-system provider configuration lives in <c>TrainingDomainModule.ConfigureServicesAsync</c>;
/// using a typed marker class lets us swap provider per-container without affecting ABP's default
/// blob containers (file management, etc.).
/// </summary>
[BlobContainerName("training-course-invoices")]
public class CourseInvoiceContainer { }
