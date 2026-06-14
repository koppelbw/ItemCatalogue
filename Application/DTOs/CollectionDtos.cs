namespace Application.DTOs;

public sealed record CreateCollectionRequest(
    string Name,
    string? Description);

public sealed record UpdateCollectionRequest(
    int Id,
    string Name,
    string? Description,
    byte[] RowVersion);

public sealed record CollectionResponse(
    int Id,
    string Name,
    string? Description,
    IReadOnlyList<CollectionItemResponse> Items,
    byte[] RowVersion);

// One membership row: the item plus the rich-join payload describing its place in the collection.
public sealed record CollectionItemResponse(
    int ItemId,
    string ItemName,
    int Quantity,
    int SortOrder,
    string? Role);

// Adds an item to a collection. SortOrder is optional (defaults to 0 — appended).
public sealed record AddCollectionItemRequest(
    int ItemId,
    int Quantity,
    int? SortOrder,
    string? Role);

// Updates an existing membership's payload. The item is identified by the route, not the body; a
// null SortOrder leaves the current ordering unchanged.
public sealed record UpdateCollectionItemRequest(
    int Quantity,
    int? SortOrder,
    string? Role);
