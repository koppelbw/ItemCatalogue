namespace Application.DTOs;

public sealed record CreateRoomRequest(
    string Name,
    string? Description);

public sealed record UpdateRoomRequest(
    int Id,
    string Name,
    string? Description,
    byte[] RowVersion);

public sealed record RoomResponse(
    int Id,
    string Name,
    string? Description,
    byte[] RowVersion);
