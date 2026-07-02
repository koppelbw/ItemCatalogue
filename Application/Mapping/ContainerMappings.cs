using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class ContainerMappings
{
    public static Container ToEntity(this CreateContainerRequest request) => new()
    {
        Name = request.Name,
        Description = request.Description,
        RoomId = request.RoomId,
        ParentContainerId = request.ParentContainerId,
        ContainerType = request.ContainerType,
        PositionXInches = request.PositionXInches,
        PositionYInches = request.PositionYInches,
        PositionZInches = request.PositionZInches,
        Rotation = request.Rotation,
        WidthInches = request.WidthInches,
        DepthInches = request.DepthInches,
        HeightInches = request.HeightInches,
        Color = request.Color,
        IsShownInUI = request.IsShownInUI,
    };

    public static void ApplyTo(this UpdateContainerRequest request, Container container)
    {
        container.Name = request.Name;
        container.Description = request.Description;
        container.RoomId = request.RoomId;
        container.ParentContainerId = request.ParentContainerId;
        container.ContainerType = request.ContainerType;
        container.PositionXInches = request.PositionXInches;
        container.PositionYInches = request.PositionYInches;
        container.PositionZInches = request.PositionZInches;
        container.Rotation = request.Rotation;
        container.WidthInches = request.WidthInches;
        container.DepthInches = request.DepthInches;
        container.HeightInches = request.HeightInches;
        container.Color = request.Color;
        container.IsShownInUI = request.IsShownInUI;
        container.RowVersion = request.RowVersion;
    }

    public static ContainerResponse ToResponse(this Container container) => new(
        container.Id,
        container.Name,
        container.Description,
        container.RoomId,
        container.ParentContainerId,
        container.ContainerType,
        container.PositionXInches,
        container.PositionYInches,
        container.PositionZInches,
        container.Rotation,
        container.WidthInches,
        container.DepthInches,
        container.HeightInches,
        container.Color,
        container.IsShownInUI,
        container.RowVersion);
}
