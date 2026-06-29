using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class FloorMappings
{
    public static Floor ToEntity(this CreateFloorRequest request) => new()
    {
        Name = request.Name,
        LocationId = request.LocationId,
        LevelIndex = request.LevelIndex,
        ElevationInches = request.ElevationInches,
        CeilingHeightInches = request.CeilingHeightInches,
    };

    public static void ApplyTo(this UpdateFloorRequest request, Floor floor)
    {
        floor.Name = request.Name;
        floor.LocationId = request.LocationId;
        floor.LevelIndex = request.LevelIndex;
        floor.ElevationInches = request.ElevationInches;
        floor.CeilingHeightInches = request.CeilingHeightInches;
        floor.RowVersion = request.RowVersion;
    }

    public static FloorResponse ToResponse(this Floor floor) => new(
        floor.Id,
        floor.Name,
        floor.LocationId,
        floor.LevelIndex,
        floor.ElevationInches,
        floor.CeilingHeightInches,
        floor.RowVersion);
}
