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
    };

    public static void ApplyTo(this UpdateContainerRequest request, Container container)
    {
        container.Name = request.Name;
        container.Description = request.Description;
        container.RoomId = request.RoomId;
        container.ParentContainerId = request.ParentContainerId;
        container.RowVersion = request.RowVersion;
    }

    public static ContainerResponse ToResponse(this Container container) => new(
        container.Id,
        container.Name,
        container.Description,
        container.RoomId,
        container.ParentContainerId,
        container.RowVersion);
}
