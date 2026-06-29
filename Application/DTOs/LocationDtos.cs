namespace Application.DTOs;

public sealed record CreateLocationRequest(
    string Name,
    string? Description);

public sealed record UpdateLocationRequest(
    int Id,
    string Name,
    string? Description,
    byte[] RowVersion);

public sealed record LocationResponse(
    int Id,
    string Name,
    string? Description,
    IReadOnlyList<FloorResponse> Floors,
    byte[] RowVersion);
