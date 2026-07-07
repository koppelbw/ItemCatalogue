using Application.DTOs;

namespace Application.StoragePorts;

// Claim-check store for a job's normalized payload (implemented by
// Infrastructure/Storage/BlobImportPayloadStore). The full row list is written once at intake;
// each chunk processor reads only its slice. Queue messages carry just {jobId, startRow, count}.
public interface IImportPayloadStore
{
    Task WriteAsync(int jobId, IReadOnlyList<CreateItemRequest> rows, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CreateItemRequest>> ReadChunkAsync(int jobId, int startRow, int count, CancellationToken cancellationToken = default);
}
