using Domain.Entities;

namespace Domain.RepositoryPorts;

// Persistence port for bulk-import jobs. Extends the generic surface with the chunk-processing
// write path; ImportChunk rows are only ever written through RecordChunkAsync so the
// marker+items transaction (the idempotency guarantee) cannot be bypassed.
public interface IImportJobRepository : IGenericRepository<ImportJob>
{
    Task<ImportJob?> GetWithChunksAsync(int jobId, CancellationToken cancellationToken = default);

    Task<bool> RecordChunkAsync(ImportChunk chunk, IReadOnlyCollection<Item> items, CancellationToken cancellationToken = default);
}
