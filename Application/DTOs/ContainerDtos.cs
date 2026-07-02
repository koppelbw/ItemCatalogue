using Domain.Enums;

namespace Application.DTOs;

public sealed record CreateContainerRequest(
    string Name,
    string? Description,
    int? RoomId,
    int? ParentContainerId,
    ContainerType? ContainerType = null,
    decimal? PositionXInches = null,
    decimal? PositionYInches = null,
    decimal? PositionZInches = null,
    decimal? Rotation = null,
    decimal? WidthInches = null,
    decimal? DepthInches = null,
    decimal? HeightInches = null,
    string? Color = null,
    bool IsShownInUI = true);

public sealed record UpdateContainerRequest(
    int Id,
    string Name,
    string? Description,
    int? RoomId,
    int? ParentContainerId,
    byte[] RowVersion,
    ContainerType? ContainerType = null,
    decimal? PositionXInches = null,
    decimal? PositionYInches = null,
    decimal? PositionZInches = null,
    decimal? Rotation = null,
    decimal? WidthInches = null,
    decimal? DepthInches = null,
    decimal? HeightInches = null,
    string? Color = null,
    bool IsShownInUI = true);

public sealed record ContainerResponse(
    int Id,
    string Name,
    string? Description,
    int? RoomId,
    int? ParentContainerId,
    ContainerType? ContainerType,
    decimal? PositionXInches,
    decimal? PositionYInches,
    decimal? PositionZInches,
    decimal? Rotation,
    decimal? WidthInches,
    decimal? DepthInches,
    decimal? HeightInches,
    string? Color,
    bool IsShownInUI,
    byte[] RowVersion);
