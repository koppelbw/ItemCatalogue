using Domain.Enums;

namespace Application.DTOs;

public sealed record ChunkRef(int ChunkIndex, int StartRow, int Count);

public sealed record ImportChunkMessage(int JobId, int ChunkIndex, int StartRow, int Count);

public sealed record ImportRowError(int RowNumber, IReadOnlyList<string> Messages);

public sealed record ImportPayloadRow(int RowNumber, CreateItemRequest Item);

public sealed record BulkRowError(int Index, IReadOnlyList<string> Messages);

public sealed record BulkCreateResult(IReadOnlyList<int> CreatedIds, IReadOnlyList<BulkRowError> Errors);

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
    int? RoomId,
    int? ContainerId,
    int? OwnerId,
    DateTime? ReleaseDate,
    DateTime? ValuationDate,
    string? AcquisitionReference);

public sealed record CsvParseResult(IReadOnlyList<CsvItemRow> Rows, IReadOnlyList<ImportRowError> Errors);

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
