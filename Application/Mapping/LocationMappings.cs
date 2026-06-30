using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class LocationMappings
{
    public static Location ToEntity(this CreateLocationRequest request) => new()
    {
        Name = request.Name,
        Description = request.Description,
    };

    public static void ApplyTo(this UpdateLocationRequest request, Location location)
    {
        location.Name = request.Name;
        location.Description = request.Description;
        location.RowVersion = request.RowVersion;
    }

    public static LocationResponse ToResponse(this Location location) => new(
        location.Id,
        location.Name,
        location.Description,
        location.Floors?.Select(f => f.ToResponse()).ToList() ?? [],
        location.RowVersion);
}
