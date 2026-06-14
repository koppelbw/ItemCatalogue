using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class TagMappings
{
    public static Tag ToEntity(this CreateTagRequest request) => new()
    {
        Name = request.Name,
        Description = request.Description,
    };

    public static void ApplyTo(this UpdateTagRequest request, Tag tag)
    {
        tag.Name = request.Name;
        tag.Description = request.Description;
        tag.RowVersion = request.RowVersion;
    }

    public static TagResponse ToResponse(this Tag tag) => new(
        tag.Id,
        tag.Name,
        tag.Description,
        tag.RowVersion);
}
