using Domain.Enums;

namespace Application.DTOs;

public sealed record ItemSearchQuery : PaginationQuery
{
    // Substring match against Name and Description (case-insensitive on a default SQL Server collation).
    public string? Query { get; init; }
    public int? RoomId { get; init; }
    public int? ContainerId { get; init; }
    public int? TagId { get; init; }
    public int? OwnerId { get; init; }

    // Range applied to CurrentValue ?? PurchasePrice. Items with neither value set are excluded.
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public Condition? Condition { get; init; }
    public bool? IsStored { get; init; }

    // When false (the default), soft-deleted items are hidden. Pass true to include them.
    public bool IncludeDeleted { get; init; }
}

public sealed record ContainerPathStep(int Id, string Name);

public sealed record ItemLocationPathResponse(
    int ItemId,
    string ItemName,
    int LocationId,
    string LocationName,
    int FloorId,
    string FloorName,
    int RoomId,
    string RoomName,
    IReadOnlyList<ContainerPathStep> ContainerPath);

public sealed record CreateItemRequest(
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

public sealed record UpdateItemRequest(
    int Id,
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
    string? AcquisitionReference,
    byte[] RowVersion);

public sealed record ItemResponse(
    int Id,
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
    bool IsDeleted,
    DeletedReason? ReasonForDeletion,
    int? RoomId,
    int? ContainerId,
    int? OwnerId,
    DateTime? ReleaseDate,
    DateTime? ValuationDate,
    string? AcquisitionReference,
    DateTime CreatedDate,
    DateTime? LastModifiedDate,
    byte[] RowVersion);
