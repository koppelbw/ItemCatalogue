using Domain.Enums;

namespace Application.DTOs;

public sealed record LocationMapResponse(
    int Id,
    string Name,
    string? Description,
    IReadOnlyList<FloorMap> Floors);

public sealed record FloorMap(
    int Id,
    string Name,
    int LevelIndex,
    decimal? ElevationInches,
    decimal? CeilingHeightInches,
    IReadOnlyList<RoomMap> Rooms);

public sealed record RoomMap(
    int Id,
    string Name,
    string? Description,
    RoomType? RoomType,
    decimal? OriginXInches,
    decimal? OriginYInches,
    decimal? WidthInches,
    decimal? DepthInches,
    decimal? HeightInches,
    decimal? Rotation,
    string? WallColor,
    string? FloorColor,
    string? CeilingColor,
    IReadOnlyList<ContainerNode> Containers,
    IReadOnlyList<DoorMap> Doors,
    IReadOnlyList<StairMap> Stairs);

// Recursive node: a container plus its nested children.
public sealed record ContainerNode(
    int Id,
    string Name,
    string? Description,
    ContainerType? ContainerType,
    decimal? PositionXInches,
    decimal? PositionYInches,
    decimal? PositionZInches,
    decimal? Rotation,
    decimal? WidthInches,
    decimal? DepthInches,
    decimal? HeightInches,
    string? Color,
    IReadOnlyList<ContainerNode> Children);

public sealed record DoorMap(
    int Id,
    string? Name,
    DoorKind Kind,
    int FromRoomId,
    int? ToRoomId,
    Wall Wall,
    decimal OffsetInches,
    decimal WidthInches,
    decimal HeightInches,
    HingeSide? HingeSide,
    Swing? Swing);

public sealed record StairMap(
    int Id,
    string? Name,
    int FromRoomId,
    int? ToRoomId,
    StairShape Shape,
    decimal? PositionXInches,
    decimal? PositionYInches,
    decimal? Rotation,
    decimal? RunInches,
    decimal? WidthInches,
    decimal? RiseInches,
    int? StepCount);
