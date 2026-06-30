using Domain.Enums;

namespace Application.DTOs;

public sealed record CreateDoorRequest(
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

public sealed record UpdateDoorRequest(
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
    Swing? Swing,
    byte[] RowVersion);

public sealed record DoorResponse(
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
    Swing? Swing,
    byte[] RowVersion);
