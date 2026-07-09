using Domain.Entities;
using Domain.Pagination;

namespace Domain.RepositoryPorts;

// Persistence port for bulk-import jobs. Extends the generic surface with the chunk-processing
// write path; ImportChunk rows are only ever written through RecordChunkAsync so the
// marker+items transaction (the idempotency guarantee) cannot be bypassed.
public interface IImportJobRepository : IGenericRepository<ImportJob>
{
    Task<ImportJob?> GetWithChunksAsync(int jobId, CancellationToken cancellationToken = default);

    // A page of jobs, newest first, each with its chunk markers loaded — status/progress are
    // derived from the markers (see ImportMappings.ToResponse), so a job-history list needs them.
    Task<PagedResult<ImportJob>> GetRecentWithChunksAsync(PageRequest page, CancellationToken cancellationToken = default);

    Task<bool> RecordChunkAsync(ImportChunk chunk, IReadOnlyCollection<Item> items, CancellationToken cancellationToken = default);
}
