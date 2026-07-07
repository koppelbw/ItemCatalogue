using Domain.Enums;

namespace Application.DTOs;

// A contiguous slice of a job's normalized payload, processed as one unit (one queue message,
// one transaction). StartRow is a 0-based index into the payload array, not a CSV line number.
public sealed record ChunkRef(int ChunkIndex, int StartRow, int Count);

// The queue message body for one chunk, serialized as JSON. Deliberately tiny (claim-check
// pattern): the payload itself lives in blob storage keyed by JobId, so a message never
// approaches the 64 KB Storage Queue limit regardless of row content.
public sealed record ImportChunkMessage(int JobId, int ChunkIndex, int StartRow, int Count);

// Per-row failure detail surfaced to the user. RowNumber is the source CSV line number
// (header = line 1, first data row = 2) so rows can be found in the original spreadsheet.
public sealed record ImportRowError(int RowNumber, IReadOnlyList<string> Messages);

// Per-row failure keyed by index into the request list passed to CreateManyAsync. The bulk core
// deliberately knows nothing about CSV line numbers; callers translate Index using their own
// offset (see ImportJobService.ProcessChunkAsync).
public sealed record BulkRowError(int Index, IReadOnlyList<string> Messages);

// Outcome of a partial-success bulk insert: ids of the rows that were inserted plus the errors
// for the rows that were not. CreatedIds.Count + Errors.Count == requests.Count.
public sealed record BulkCreateResult(IReadOnlyList<int> CreatedIds, IReadOnlyList<BulkRowError> Errors);

// One parsed CSV data row, normalized to typed values but with placement/owner still by NAME
// ("Garage", "Will") — users don't know database ids. The import service resolves names to ids
// at intake; a cell that parses as an integer is treated as a direct id.
public sealed record CsvItemRow(
    int RowNumber,
    string Name,
    string? Description,
    List<ItemType> ItemTypes,
    decimal? PurchasePrice,
    decimal? CurrentValue,
    string? Brand,
    string? Model,
    string? SerialNumber,
    string? PurchasedFrom,
    int Quantity,
    Condition? Condition,
    AcquisitionType? AcquisitionType,
    DateTime? PurchaseDate,
    DateTime? WarrantyExpiryDate,
    bool IsStored,
    bool IsShownInUI,
    string? Room,
    string? Container,
    string? Owner,
    DateTime? ReleaseDate,
    DateTime? ValuationDate,
    string? AcquisitionReference);

public sealed record CsvParseResult(IReadOnlyList<CsvItemRow> Rows, IReadOnlyList<ImportRowError> Errors);

// Poll response for GET /api/imports/{id}. Status, ProcessedChunks, Succeeded and Failed are
// derived from the per-chunk marker rows at read time (never from a mutable counter), so
// concurrent chunk processors cannot contend on a hot job row.
public sealed record ImportJobResponse(
    int Id,
    ImportJobStatus Status,
    string FileName,
    int TotalRows,
    int RejectedAtIntake,
    int EnqueuedRows,
    int TotalChunks,
    int ProcessedChunks,
    int Succeeded,
    int Failed,
    IReadOnlyList<ImportRowError> Errors,
    DateTime CreatedDate,
    DateTime? LastModifiedDate);
