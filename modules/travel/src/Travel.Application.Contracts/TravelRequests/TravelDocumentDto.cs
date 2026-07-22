using System;
using Volo.Abp.Application.Dtos;

namespace Travel.TravelRequests;

public class TravelDocumentDto : EntityDto<Guid>
{
    public Guid TravelRequestId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string BlobName { get; set; } = string.Empty;
}
