using Domain.Entities;

namespace Domain.RepositoryPorts;

// Persistence port for bulk-import jobs. Extends the generic surface with the chunk-processing
// write path; ImportChunk rows are only ever written through RecordChunkAsync so the
// marker+items transaction (the idempotency guarantee) cannot be bypassed.
public interface IImportJobRepository : IGenericRepository<ImportJob>
{
    // The job with its chunk markers eager-loaded; progress/status are derived from them.
    Task<ImportJob?> GetWithChunksAsync(int jobId, CancellationToken cancellationToken = default);

    // Atomically records a processed chunk: the marker row and the chunk's surviving items in ONE
    // SaveChanges. Returns false when the (JobId, ChunkIndex) marker already exists — the message
    // was redelivered, the transaction rolled back, and nothing (marker or items) was inserted.
    Task<bool> RecordChunkAsync(ImportChunk chunk, IReadOnlyCollection<Item> items, CancellationToken cancellationToken = default);
}
