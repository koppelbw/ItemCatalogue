using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

// Projects a fully-loaded Location aggregate (Floors -> Rooms -> Containers tree + Doors, as loaded
// by ILocationRepository.GetMapAsync) into the nested LocationMapResponse the map endpoint returns.
public static class LocationMapMappings
{
    public static LocationMapResponse ToMapResponse(this Location location) => new(
        location.Id,
        location.Name,
        location.Description,
        location.Floors
            .OrderBy(f => f.LevelIndex)
            .Select(ToFloorMap)
            .ToList());

    private static FloorMap ToFloorMap(Floor floor) => new(
        floor.Id,
        floor.Name,
        floor.LevelIndex,
        floor.ElevationInches,
        floor.CeilingHeightInches,
        floor.Rooms.Select(ToRoomMap).ToList());

    private static RoomMap ToRoomMap(Room room) => new(
        room.Id,
        room.Name,
        room.Description,
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
        // room.Containers holds the top-level containers (RoomId set); nested ones hang off Children.
        room.Containers.Select(ToContainerNode).ToList(),
        room.Doors.Select(ToDoorMap).ToList(),
        room.Stairs.Select(ToStairMap).ToList());

    private static ContainerNode ToContainerNode(Container container) => new(
        container.Id,
        container.Name,
        container.Description,
        container.ContainerType,
        container.PositionXInches,
        container.PositionYInches,
        container.PositionZInches,
        container.Rotation,
        container.WidthInches,
        container.DepthInches,
        container.HeightInches,
        container.Color,
        container.Children.Select(ToContainerNode).ToList());

    private static DoorMap ToDoorMap(Door door) => new(
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
        door.Swing);

    private static StairMap ToStairMap(Stair stair) => new(
        stair.Id,
        stair.Name,
        stair.FromRoomId,
        stair.ToRoomId,
        stair.Shape,
        stair.PositionXInches,
        stair.PositionYInches,
        stair.Rotation,
        stair.RunInches,
        stair.WidthInches,
        stair.RiseInches,
        stair.StepCount);
}
