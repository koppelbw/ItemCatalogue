using Domain.Enums;

namespace Application.DTOs;

public sealed record CreateItemRequest(
    string Name,
    string? Description,
    List<ItemType> ItemTypes,
    decimal? Price,
    bool IsStored,
    int? LocationId,
    int? OwnerId);

public sealed record UpdateItemRequest(
    int Id,
    string Name,
    string? Description,
    List<ItemType> ItemTypes,
    decimal? Price,
    bool IsStored,
    int? LocationId,
    int? OwnerId,
    byte[] RowVersion);

public sealed record ItemResponse(
    int Id,
    string Name,
    string? Description,
    List<ItemType> ItemTypes,
    decimal? Price,
    bool IsStored,
    bool IsDeleted,
    DeletedReason? ReasonForDeletion,
    int? LocationId,
    int? OwnerId,
    DateTime CreatedDate,
    DateTime? LastModifiedDate,
    byte[] RowVersion);
