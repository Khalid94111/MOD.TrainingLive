using System;
using Volo.Abp.Domain.Entities;

namespace Travel.TravelRequests;

public class TravelDocument : Entity<Guid>
{
    public Guid TravelRequestId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string FileExtension { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string BlobName { get; private set; } = string.Empty;

    protected TravelDocument()
    {
    }

    public TravelDocument(Guid id, Guid travelRequestId, string name, string fileExtension, long fileSize, string blobName)
    {
        Id = id;
        TravelRequestId = travelRequestId;
        Name = name;
        FileExtension = fileExtension;
        FileSize = fileSize;
        BlobName = blobName;
    }
}
