using Domain.Enums;

namespace Application.DTOs;

public sealed record CreateRoomRequest(
    string Name,
    string? Description,
    int FloorId,
    RoomType? RoomType = null,
    decimal? OriginXInches = null,
    decimal? OriginYInches = null,
    decimal? WidthInches = null,
    decimal? DepthInches = null,
    decimal? HeightInches = null,
    decimal? Rotation = null,
    string? WallColor = null,
    string? FloorColor = null,
    string? CeilingColor = null);

public sealed record UpdateRoomRequest(
    int Id,
    string Name,
    string? Description,
    int FloorId,
    byte[] RowVersion,
    RoomType? RoomType = null,
    decimal? OriginXInches = null,
    decimal? OriginYInches = null,
    decimal? WidthInches = null,
    decimal? DepthInches = null,
    decimal? HeightInches = null,
    decimal? Rotation = null,
    string? WallColor = null,
    string? FloorColor = null,
    string? CeilingColor = null);

public sealed record RoomResponse(
    int Id,
    string Name,
    string? Description,
    int FloorId,
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
    byte[] RowVersion);
