using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Logging;

// Source-generated, allocation-free log methods for the Application services (see the [LoggerMessage]
// source generator). Logging only the state-changing operations keeps the signal high: reads and
// not-found outcomes are already covered by request telemetry, so they are deliberately not logged
// here. The {Named} placeholders are captured as structured properties by the OpenTelemetry pipeline.
internal static partial class ServiceLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Created {EntityType} {EntityId}")]
    public static partial void EntityCreated(this ILogger logger, string entityType, int entityId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated {EntityType} {EntityId}")]
    public static partial void EntityUpdated(this ILogger logger, string entityType, int entityId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {EntityType} {EntityId} ({RowsAffected} row(s))")]
    public static partial void EntityDeleted(this ILogger logger, string entityType, int entityId, int rowsAffected);

    [LoggerMessage(Level = LogLevel.Information, Message = "Soft-deleted Item {EntityId} (reason {Reason}, {RowsAffected} row(s))")]
    public static partial void ItemSoftDeleted(this ILogger logger, int entityId, DeletedReason reason, int rowsAffected);

    [LoggerMessage(Level = LogLevel.Information, Message = "Bulk-created {Succeeded} Item(s), rejected {Failed} row(s)")]
    public static partial void ItemsBulkCreated(this ILogger logger, int succeeded, int failed);

    [LoggerMessage(Level = LogLevel.Information, Message = "Started import job {JobId}: {TotalRows} row(s), {EnqueuedRows} enqueued in {TotalChunks} chunk(s)")]
    public static partial void ImportJobStarted(this ILogger logger, int jobId, int totalRows, int enqueuedRows, int totalChunks);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processed import chunk {ChunkIndex} of job {JobId}: {Succeeded} succeeded, {Failed} failed")]
    public static partial void ImportChunkProcessed(this ILogger logger, int jobId, int chunkIndex, int succeeded, int failed);
}
