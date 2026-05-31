namespace Application.DTOs;

public sealed record CreateLocationRequest(
    string Name,
    string? Description,
    int RoomId);

public sealed record UpdateLocationRequest(
    int Id,
    string Name,
    string? Description,
    int RoomId);

public sealed record LocationResponse(
    int Id,
    string Name,
    string? Description,
    int RoomId);
