using Microsoft.Extensions.Logging;

namespace Persistence.Logging;

// Source-generated log methods for the persistence adapters. These fire at the points where a
// provider-level failure is translated into a domain exception, so the conflict is recorded once
// (with the entity it concerns) before it propagates — the ConflictExceptionHandler that maps these
// to HTTP 409 does not log, so without this the conflicts would be invisible in telemetry.
internal static partial class RepositoryLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Concurrency conflict updating {EntityType} {EntityId}")]
    public static partial void ConcurrencyConflict(this ILogger logger, string entityType, int entityId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{EntityType} {EntityId} cannot be deleted; still referenced by another record (FK restrict)")]
    public static partial void EntityInUse(this ILogger logger, string entityType, int entityId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{EntityType} write rejected; a record with the same unique value already exists")]
    public static partial void DuplicateValue(this ILogger logger, string entityType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Import chunk {ChunkIndex} of job {JobId} already recorded; skipping redelivered message")]
    public static partial void ImportChunkAlreadyRecorded(this ILogger logger, int jobId, int chunkIndex);
}
