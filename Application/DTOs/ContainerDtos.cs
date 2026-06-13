namespace Application.DTOs;

// A Container is owned by exactly one parent: set RoomId for a top-level container (sits directly in
// a room) or ParentContainerId for a nested container (sits inside another container) — never both.
public sealed record CreateContainerRequest(
    string Name,
    string? Description,
    int? RoomId,
    int? ParentContainerId);

public sealed record UpdateContainerRequest(
    int Id,
    string Name,
    string? Description,
    int? RoomId,
    int? ParentContainerId,
    byte[] RowVersion);

public sealed record ContainerResponse(
    int Id,
    string Name,
    string? Description,
    int? RoomId,
    int? ParentContainerId,
    byte[] RowVersion);
