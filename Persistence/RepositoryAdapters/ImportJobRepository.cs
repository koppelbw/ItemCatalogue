using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.Logging;

namespace Persistence.RepositoryAdapters;

public sealed class ImportJobRepository(ItemCatalogueDbContext dbContext, ILoggerFactory loggerFactory)
    : GenericRepository<ImportJob>(dbContext, loggerFactory), IImportJobRepository
{
    public async Task<ImportJob?> GetWithChunksAsync(int jobId, CancellationToken cancellationToken = default)
    {
        return await EntitySet
            .AsNoTracking()
            .Include(j => j.Chunks)
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
    }

    public async Task<bool> RecordChunkAsync(ImportChunk chunk, IReadOnlyCollection<Item> items, CancellationToken cancellationToken = default)
    {
        DbContext.Set<ImportChunk>().Add(chunk);
        DbContext.Set<Item>().AddRange(items);

        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Redelivered queue message: the (JobId, ChunkIndex) marker already exists, so SQL
            // rejected the SaveChanges and marker AND items rolled back together — exactly the
            // no-duplicates guarantee. Detach what this call staged so the scoped context stays
            // clean, and report "already done" rather than throwing.
            DbContext.Entry(chunk).State = EntityState.Detached;
            foreach (var item in items)
            {
                DbContext.Entry(item).State = EntityState.Detached;
            }

            Logger.ImportChunkAlreadyRecorded(chunk.JobId, chunk.ChunkIndex);
            return false;
        }
    }
}
