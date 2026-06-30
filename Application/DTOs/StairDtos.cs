using Domain.Enums;

namespace Application.DTOs;

public sealed record CreateStairRequest(
    string? Name,
    int FromRoomId,
    int? ToRoomId,
    StairShape Shape,
    decimal? PositionXInches = null,
    decimal? PositionYInches = null,
    decimal? Rotation = null,
    decimal? RunInches = null,
    decimal? WidthInches = null,
    decimal? RiseInches = null,
    int? StepCount = null);

public sealed record UpdateStairRequest(
    int Id,
    string? Name,
    int FromRoomId,
    int? ToRoomId,
    StairShape Shape,
    byte[] RowVersion,
    decimal? PositionXInches = null,
    decimal? PositionYInches = null,
    decimal? Rotation = null,
    decimal? RunInches = null,
    decimal? WidthInches = null,
    decimal? RiseInches = null,
    int? StepCount = null);

public sealed record StairResponse(
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
    int? StepCount,
    byte[] RowVersion);
