namespace Application.DTOs;

public sealed record CreateTagRequest(
    string Name,
    string? Description);

public sealed record UpdateTagRequest(
    int Id,
    string Name,
    string? Description,
    byte[] RowVersion);

public sealed record TagResponse(
    int Id,
    string Name,
    string? Description,
    byte[] RowVersion);

// Replaces an item's full tag set with the tags identified by TagIds (send an empty list to clear).
public sealed record SetItemTagsRequest(
    IReadOnlyList<int> TagIds);

// The tags currently assigned to an item, returned by the item-tags endpoints.
public sealed record ItemTagsResponse(
    int ItemId,
    IReadOnlyList<TagResponse> Tags);
