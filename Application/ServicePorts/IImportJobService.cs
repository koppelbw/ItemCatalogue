using Application.DTOs;

namespace Application.ServicePorts;

// Orchestrates the async bulk-import pipeline. Transport-agnostic on both sides: the API calls
// StartImportAsync/GetStatusAsync over HTTP, and whatever background trigger is in play (Storage
// Queue Function today; Durable/Service Bus adapters later) calls ProcessChunkAsync.
public interface IImportJobService
{
    // Synchronous intake: parse the CSV, resolve room/container/owner names to ids, create the
    // job row, persist the normalized payload (claim-check), and dispatch one message per chunk.
    // Rows that fail parsing or name resolution are rejected here and recorded on the job;
    // business validation (FluentValidation, FK re-check) runs later, per chunk.
    // Throws FluentValidation.ValidationException when the file as a whole is unusable
    // (no data rows, or more than ImportOptions.MaxRows).
    Task<ImportJobResponse> StartImportAsync(Stream csvContent, string fileName, CancellationToken cancellationToken = default);

    // Poll endpoint projection: status/progress derived from the chunk marker rows.
    Task<ImportJobResponse> GetStatusAsync(int jobId, CancellationToken cancellationToken = default);

    // Processes one chunk message: read the payload slice, validate/map, and record the outcome
    // atomically. Idempotent — a redelivered message is a no-op (see RecordChunkAsync).
    Task ProcessChunkAsync(ImportChunkMessage message, CancellationToken cancellationToken = default);

    // Terminal failure path (the poison-queue trigger): records the chunk's rows as failed so the
    // job still reaches Completed instead of hanging one marker short. Idempotent like
    // ProcessChunkAsync — if the chunk somehow succeeded earlier, the marker already exists and
    // this is a no-op.
    Task MarkChunkFailedAsync(ImportChunkMessage message, string reason, CancellationToken cancellationToken = default);
}
