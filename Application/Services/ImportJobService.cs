using Application.DTOs;
using Application.Logging;
using Application.Mapping;
using Application.Options;
using Application.ServicePorts;
using Application.StoragePorts;
using Domain.Entities;
using Domain.Exceptions;
using Domain.RepositoryPorts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

public sealed class ImportJobService(
    ICsvItemParser csvParser,
    IImportPayloadStore payloadStore,
    IImportDispatcher dispatcher,
    IImportJobRepository importJobRepository,
    ItemBulkPreparer bulkPreparer,
    TimeProvider timeProvider,
    IOptions<ImportOptions> importOptions,
    ILogger<ImportJobService> logger) : IImportJobService
{
    private readonly ImportOptions _options = importOptions.Value;

    public async Task<ImportJobResponse> StartImportAsync(Stream csvContent, string fileName, CancellationToken cancellationToken = default)
    {
        var parsed = await csvParser.ParseAsync(csvContent, cancellationToken);
        var totalRows = parsed.Rows.Count + parsed.Errors.Count;


        if (totalRows == 0)
        {
            throw new ValidationException([new ValidationFailure("File", "The file contains no data rows.")]);
        }
        if (totalRows > _options.MaxRows)
        {
            throw new ValidationException(
                [new ValidationFailure("File", $"The file has {totalRows} data rows; the maximum per import is {_options.MaxRows}.")]);
        }

        // The CSV carries reference ids (RoomId/ContainerId/OwnerId) directly, so intake is a
        // straight projection: parse errors are the only intake rejections, and every well-formed
        // row is enqueued. Whether a referenced id actually exists is validated per chunk by
        // ItemBulkPreparer's batched FK check.
        var intakeErrors = parsed.Errors;
        var payloadRows = parsed.Rows
            .Select(row => new ImportPayloadRow(row.RowNumber, row.ToCreateRequest()))
            .ToList();

        var chunkSize = Math.Max(1, _options.ChunkSize);
        var totalChunks = (payloadRows.Count + chunkSize - 1) / chunkSize;

        var job = new ImportJob
        {
            FileName = SanitizeFileName(fileName),
            TotalRows = totalRows,
            RejectedAtIntake = intakeErrors.Count,
            EnqueuedRows = payloadRows.Count,
            TotalChunks = totalChunks,
            IntakeErrorsJson = ImportMappings.SerializeErrors(intakeErrors),
        };
        await importJobRepository.InsertAsync(job, cancellationToken);

        // Order matters: the payload must be readable before any queue message can be delivered.
        if (payloadRows.Count > 0)
        {
            await payloadStore.WriteAsync(job.Id, payloadRows, cancellationToken);

            var chunks = Enumerable.Range(0, totalChunks)
                .Select(i => new ChunkRef(
                    ChunkIndex: i,
                    StartRow: i * chunkSize,
                    Count: Math.Min(chunkSize, payloadRows.Count - i * chunkSize)))
                .ToList();
            await dispatcher.DispatchAsync(job.Id, chunks, cancellationToken);
        }

        logger.ImportJobStarted(job.Id, totalRows, payloadRows.Count, totalChunks);
        return job.ToResponse();
    }

    public async Task<ImportJobResponse> GetStatusAsync(int jobId, CancellationToken cancellationToken = default)
    {
        var job = await importJobRepository.GetWithChunksAsync(jobId, cancellationToken)
            ?? throw NotFoundException.For("ImportJob", jobId);

        return job.ToResponse();
    }

    public async Task ProcessChunkAsync(ImportChunkMessage message, CancellationToken cancellationToken = default)
    {
        var rows = await payloadStore.ReadChunkAsync(message.JobId, message.StartRow, message.Count, cancellationToken);
        var prepared = await bulkPreparer.PrepareAsync(rows.Select(r => r.Item).ToList(), cancellationToken);

        // Chunk-local error indexes -> the CSV row numbers carried in the payload.
        var errors = prepared.Errors
            .Select(e => new ImportRowError(rows[e.Index].RowNumber, e.Messages))
            .ToList();

        var chunk = new ImportChunk
        {
            JobId = message.JobId,
            ChunkIndex = message.ChunkIndex,
            Succeeded = prepared.Entities.Count,
            Failed = errors.Count,
            ProcessedAt = timeProvider.GetUtcNow().UtcDateTime,
            ErrorsJson = ImportMappings.SerializeErrors(errors),
        };

        var recorded = await importJobRepository.RecordChunkAsync(chunk, prepared.Entities, cancellationToken);
        if (recorded)
        {
            logger.ImportChunkProcessed(message.JobId, message.ChunkIndex, chunk.Succeeded, chunk.Failed);
        }
    }

    public async Task MarkChunkFailedAsync(ImportChunkMessage message, string reason, CancellationToken cancellationToken = default)
    {
        // Best effort: pull the real CSV row numbers from the payload so the report points at
        // spreadsheet rows. If the payload itself is the problem (the likely reason the chunk
        // poisoned), fall back to one aggregate error under row 0 (= file-level).
        List<ImportRowError> errors;
        try
        {
            var rows = await payloadStore.ReadChunkAsync(message.JobId, message.StartRow, message.Count, cancellationToken);
            errors = rows.Select(r => new ImportRowError(r.RowNumber, [reason])).ToList();
        }
        catch
        {
            errors = [new ImportRowError(0, [$"{message.Count} row(s) in chunk {message.ChunkIndex}: {reason}"])];
        }

        var chunk = new ImportChunk
        {
            JobId = message.JobId,
            ChunkIndex = message.ChunkIndex,
            Succeeded = 0,
            Failed = message.Count,
            ProcessedAt = timeProvider.GetUtcNow().UtcDateTime,
            ErrorsJson = ImportMappings.SerializeErrors(errors),
        };

        var recorded = await importJobRepository.RecordChunkAsync(chunk, [], cancellationToken);
        if (recorded)
        {
            logger.ImportChunkPoisoned(message.JobId, message.ChunkIndex, message.Count);
        }
    }

    // Some browsers send a full client path as the upload's file name; keep only the leaf and fit
    // the FileName column (255).
    private static string SanitizeFileName(string? fileName)
    {
        var raw = fileName ?? string.Empty;
        var leaf = raw[(raw.LastIndexOfAny(['/', '\\']) + 1)..].Trim();
        if (leaf.Length == 0) return "upload.csv";
        return leaf.Length <= 255 ? leaf : leaf[..255];
    }
}
