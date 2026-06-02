namespace Application.DTOs;

public sealed record CreatePersonRequest(
    string Name);

public sealed record UpdatePersonRequest(
    int Id,
    string Name,
    byte[] RowVersion);

public sealed record PersonResponse(
    int Id,
    string Name,
    byte[] RowVersion);
