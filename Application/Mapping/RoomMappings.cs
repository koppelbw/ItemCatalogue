using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class RoomMappings
{
    public static Room ToEntity(this CreateRoomRequest request) => new()
    {
        Name = request.Name,
        Description = request.Description,
        FloorId = request.FloorId,
        RoomType = request.RoomType,
        OriginXInches = request.OriginXInches,
        OriginYInches = request.OriginYInches,
        WidthInches = request.WidthInches,
        DepthInches = request.DepthInches,
        HeightInches = request.HeightInches,
        Rotation = request.Rotation,
        WallColor = request.WallColor,
        FloorColor = request.FloorColor,
        CeilingColor = request.CeilingColor,
    };

    public static void ApplyTo(this UpdateRoomRequest request, Room room)
    {
        room.Name = request.Name;
        room.Description = request.Description;
        room.FloorId = request.FloorId;
        room.RoomType = request.RoomType;
        room.OriginXInches = request.OriginXInches;
        room.OriginYInches = request.OriginYInches;
        room.WidthInches = request.WidthInches;
        room.DepthInches = request.DepthInches;
        room.HeightInches = request.HeightInches;
        room.Rotation = request.Rotation;
        room.WallColor = request.WallColor;
        room.FloorColor = request.FloorColor;
        room.CeilingColor = request.CeilingColor;
        room.RowVersion = request.RowVersion;
    }

    public static RoomResponse ToResponse(this Room room) => new(
        room.Id,
        room.Name,
        room.Description,
        room.FloorId,
        room.RoomType,
        room.OriginXInches,
        room.OriginYInches,
        room.WidthInches,
        room.DepthInches,
        room.HeightInches,
        room.Rotation,
        room.WallColor,
        room.FloorColor,
        room.CeilingColor,
        room.RowVersion);
}
