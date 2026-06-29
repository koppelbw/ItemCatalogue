using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class StairMappings
{
    public static Stair ToEntity(this CreateStairRequest request) => new()
    {
        Name = request.Name,
        FromRoomId = request.FromRoomId,
        ToRoomId = request.ToRoomId,
        Shape = request.Shape,
        PositionXInches = request.PositionXInches,
        PositionYInches = request.PositionYInches,
        Rotation = request.Rotation,
        RunInches = request.RunInches,
        WidthInches = request.WidthInches,
        RiseInches = request.RiseInches,
        StepCount = request.StepCount,
    };

    public static void ApplyTo(this UpdateStairRequest request, Stair stair)
    {
        stair.Name = request.Name;
        stair.FromRoomId = request.FromRoomId;
        stair.ToRoomId = request.ToRoomId;
        stair.Shape = request.Shape;
        stair.PositionXInches = request.PositionXInches;
        stair.PositionYInches = request.PositionYInches;
        stair.Rotation = request.Rotation;
        stair.RunInches = request.RunInches;
        stair.WidthInches = request.WidthInches;
        stair.RiseInches = request.RiseInches;
        stair.StepCount = request.StepCount;
        stair.RowVersion = request.RowVersion;
    }

    public static StairResponse ToResponse(this Stair stair) => new(
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
        stair.StepCount,
        stair.RowVersion);
}
