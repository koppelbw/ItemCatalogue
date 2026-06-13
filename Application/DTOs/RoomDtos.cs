namespace Application.DTOs;

public sealed record CreateRoomRequest(
    string Name,
    string? Description,
    int LocationId);

public sealed record UpdateRoomRequest(
    int Id,
    string Name,
    string? Description,
    int LocationId,
    byte[] RowVersion);

public sealed record RoomResponse(
    int Id,
    string Name,
    string? Description,
    int LocationId,
    byte[] RowVersion);
