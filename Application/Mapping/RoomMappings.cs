using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class RoomMappings
{
    public static Room ToEntity(this CreateRoomRequest request) => new()
    {
        Name = request.Name,
        Description = request.Description,
    };

    public static void ApplyTo(this UpdateRoomRequest request, Room room)
    {
        room.Name = request.Name;
        room.Description = request.Description;
        room.RowVersion = request.RowVersion;
    }

    public static RoomResponse ToResponse(this Room room) => new(
        room.Id,
        room.Name,
        room.Description,
        room.RowVersion);
}
