using Domain.Enums;

namespace Application.DTOs;

public sealed record CreateItemRequest(
    string Name,
    string? Description,
    List<ItemType> ItemTypes,
    decimal? Price,
    bool IsStored,
    int? RoomId,
    int? ContainerId,
    int? OwnerId);

public sealed record UpdateItemRequest(
    int Id,
    string Name,
    string? Description,
    List<ItemType> ItemTypes,
    decimal? Price,
    bool IsStored,
    int? RoomId,
    int? ContainerId,
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
    int? RoomId,
    int? ContainerId,
    int? OwnerId,
    DateTime CreatedDate,
    DateTime? LastModifiedDate,
    byte[] RowVersion);
