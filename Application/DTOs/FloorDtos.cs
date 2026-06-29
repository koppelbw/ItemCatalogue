namespace Application.DTOs;

public sealed record CreateFloorRequest(
    string Name,
    int LocationId,
    int LevelIndex,
    decimal? ElevationInches,
    decimal? CeilingHeightInches);

public sealed record UpdateFloorRequest(
    int Id,
    string Name,
    int LocationId,
    int LevelIndex,
    decimal? ElevationInches,
    decimal? CeilingHeightInches,
    byte[] RowVersion);

public sealed record FloorResponse(
    int Id,
    string Name,
    int LocationId,
    int LevelIndex,
    decimal? ElevationInches,
    decimal? CeilingHeightInches,
    byte[] RowVersion);
