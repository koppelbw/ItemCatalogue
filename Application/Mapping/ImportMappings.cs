using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using System.Text.Json;

namespace Application.Mapping;

public static class ImportMappings
{
    // A parsed CSV row plus its resolved reference ids -> the same request shape the single-item
    // POST endpoint takes, so everything downstream of intake is shared with the normal path.
    public static CreateItemRequest ToCreateRequest(this CsvItemRow row, int? roomId, int? containerId, int? ownerId) => new(
        Name: row.Name,
        Description: row.Description,
        ItemTypes: row.ItemTypes,
        PurchasePrice: row.PurchasePrice,
        CurrentValue: row.CurrentValue,
        Brand: row.Brand,
        Model: row.Model,
        SerialNumber: row.SerialNumber,
        PurchasedFrom: row.PurchasedFrom,
        Quantity: row.Quantity,
        Condition: row.Condition,
        AcquisitionType: row.AcquisitionType,
        PurchaseDate: row.PurchaseDate,
        WarrantyExpiryDate: row.WarrantyExpiryDate,
        IsStored: row.IsStored,
        IsShownInUI: row.IsShownInUI,
        RoomId: roomId,
        ContainerId: containerId,
        OwnerId: ownerId,
        ReleaseDate: row.ReleaseDate,
        ValuationDate: row.ValuationDate,
        AcquisitionReference: row.AcquisitionReference);

    // Derives the user-facing view from the job row + its chunk markers. Status and progress are
    // projections of the markers (never a stored counter), so this is the single place the
    // "how done is it?" question is answered.
    public static ImportJobResponse ToResponse(this ImportJob job)
    {
        var processedChunks = job.Chunks.Count;
        var succeeded = job.Chunks.Sum(c => c.Succeeded);
        var failedInChunks = job.Chunks.Sum(c => c.Failed);

        var status = job.TotalChunks == 0 || processedChunks >= job.TotalChunks
            ? ImportJobStatus.Completed
            : processedChunks > 0 ? ImportJobStatus.Processing : ImportJobStatus.Queued;

        var errors = DeserializeErrors(job.IntakeErrorsJson)
            .Concat(job.Chunks.SelectMany(c => DeserializeErrors(c.ErrorsJson)))
            .OrderBy(e => e.RowNumber)
            .ToList();

        return new ImportJobResponse(
            Id: job.Id,
            Status: status,
            FileName: job.FileName,
            TotalRows: job.TotalRows,
            RejectedAtIntake: job.RejectedAtIntake,
            EnqueuedRows: job.EnqueuedRows,
            TotalChunks: job.TotalChunks,
            ProcessedChunks: processedChunks,
            Succeeded: succeeded,
            // Failed counts every unsuccessful row: rejected at intake plus failed during chunk
            // processing. When the job completes, Succeeded + Failed == TotalRows.
            Failed: job.RejectedAtIntake + failedInChunks,
            Errors: errors,
            CreatedDate: job.CreatedDate,
            LastModifiedDate: job.LastModifiedDate);
    }

    // Row errors are persisted as plain JSON (ImportJob.IntakeErrorsJson / ImportChunk.ErrorsJson);
    // writer and reader live here so the format has exactly one definition.
    public static string? SerializeErrors(IReadOnlyList<ImportRowError> errors)
        => errors.Count == 0 ? null : JsonSerializer.Serialize(errors);

    public static IReadOnlyList<ImportRowError> DeserializeErrors(string? json)
        => string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<ImportRowError>>(json) ?? [];
}
