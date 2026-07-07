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
    IRoomRepository roomRepository,
    IContainerRepository containerRepository,
    IPersonRepository personRepository,
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

        // Whole-file problems surface as the same 400 problem-details shape as any other
        // validation failure (see ValidationExceptionHandler).
        if (totalRows == 0)
        {
            throw new ValidationException([new ValidationFailure("File", "The file contains no data rows.")]);
        }
        if (totalRows > _options.MaxRows)
        {
            throw new ValidationException(
                [new ValidationFailure("File", $"The file has {totalRows} data rows; the maximum per import is {_options.MaxRows}.")]);
        }

        var intakeErrors = new List<ImportRowError>(parsed.Errors);
        var payloadRows = new List<ImportPayloadRow>();

        // Users reference rooms/containers/owners by name ("Garage", "Will"), not database id.
        // Resolution happens here at intake — fast feedback, and the queued payload carries only
        // ids so chunk processing needs no name knowledge. A numeric cell is taken as a direct id.
        var rooms = await NameMap.LoadAsync(parsed.Rows.Any(r => r.Room is not null), roomRepository, r => r.Name, cancellationToken);
        var containers = await NameMap.LoadAsync(parsed.Rows.Any(r => r.Container is not null), containerRepository, c => c.Name, cancellationToken);
        var owners = await NameMap.LoadAsync(parsed.Rows.Any(r => r.Owner is not null), personRepository, p => p.Name, cancellationToken);

        foreach (var row in parsed.Rows)
        {
            var rowErrors = new List<string>();
            var roomId = rooms.Resolve("Room", row.Room, rowErrors);
            var containerId = containers.Resolve("Container", row.Container, rowErrors);
            var ownerId = owners.Resolve("Owner", row.Owner, rowErrors);

            if (rowErrors.Count > 0)
            {
                intakeErrors.Add(new ImportRowError(row.RowNumber, rowErrors));
            }
            else
            {
                payloadRows.Add(new ImportPayloadRow(row.RowNumber, row.ToCreateRequest(roomId, containerId, ownerId)));
            }
        }

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

    // Some browsers send a full client path as the upload's file name; keep only the leaf and fit
    // the FileName column (255).
    private static string SanitizeFileName(string? fileName)
    {
        var raw = fileName ?? string.Empty;
        var leaf = raw[(raw.LastIndexOfAny(['/', '\\']) + 1)..].Trim();
        if (leaf.Length == 0) return "upload.csv";
        return leaf.Length <= 255 ? leaf : leaf[..255];
    }

    // Case-insensitive name -> id lookup for one reference table, built only when the file
    // actually uses that column. Names shared by two rows (e.g. two rooms both named "Closet")
    // are ambiguous — resolvable only by id.
    private sealed class NameMap
    {
        private readonly Dictionary<string, int>? _idsByName;
        private readonly HashSet<string>? _ambiguous;

        private NameMap(Dictionary<string, int>? idsByName, HashSet<string>? ambiguous)
        {
            _idsByName = idsByName;
            _ambiguous = ambiguous;
        }

        public static async Task<NameMap> LoadAsync<TEntity>(
            bool needed,
            IGenericRepository<TEntity> repository,
            Func<TEntity, string> name,
            CancellationToken cancellationToken)
            where TEntity : class, IEntity
        {
            if (!needed)
            {
                return new NameMap(null, null);
            }

            var idsByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entity in await repository.GetAllUnpagedAsync(cancellationToken))
            {
                if (!idsByName.TryAdd(name(entity), entity.Id))
                {
                    ambiguous.Add(name(entity));
                }
            }

            return new NameMap(idsByName, ambiguous);
        }

        public int? Resolve(string kind, string? cell, List<string> rowErrors)
        {
            if (string.IsNullOrWhiteSpace(cell))
            {
                return null;
            }

            var value = cell.Trim();
            if (int.TryParse(value, out var directId))
            {
                // Existence of a direct id is re-checked per chunk by ItemBulkPreparer's FK query.
                return directId;
            }

            if (_ambiguous is not null && _ambiguous.Contains(value))
            {
                rowErrors.Add($"{kind} name '{value}' matches more than one record; use its numeric id instead.");
                return null;
            }
            if (_idsByName is not null && _idsByName.TryGetValue(value, out var id))
            {
                return id;
            }

            rowErrors.Add($"Unknown {kind} '{value}'.");
            return null;
        }
    }
}
