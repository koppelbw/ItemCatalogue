using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class DoorMappings
{
    public static Door ToEntity(this CreateDoorRequest request) => new()
    {
        Name = request.Name,
        Kind = request.Kind,
        FromRoomId = request.FromRoomId,
        ToRoomId = request.ToRoomId,
        Wall = request.Wall,
        OffsetInches = request.OffsetInches,
        WidthInches = request.WidthInches,
        HeightInches = request.HeightInches,
        HingeSide = request.HingeSide,
        Swing = request.Swing,
    };

    public static void ApplyTo(this UpdateDoorRequest request, Door door)
    {
        door.Name = request.Name;
        door.Kind = request.Kind;
        door.FromRoomId = request.FromRoomId;
        door.ToRoomId = request.ToRoomId;
        door.Wall = request.Wall;
        door.OffsetInches = request.OffsetInches;
        door.WidthInches = request.WidthInches;
        door.HeightInches = request.HeightInches;
        door.HingeSide = request.HingeSide;
        door.Swing = request.Swing;
        door.RowVersion = request.RowVersion;
    }

    public static DoorResponse ToResponse(this Door door) => new(
        door.Id,
        door.Name,
        door.Kind,
        door.FromRoomId,
        door.ToRoomId,
        door.Wall,
        door.OffsetInches,
        door.WidthInches,
        door.HeightInches,
        door.HingeSide,
        door.Swing,
        door.RowVersion);
}
