using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class PersonMappings
{
    public static Person ToEntity(this CreatePersonRequest request) => new()
    {
        Name = request.Name,
    };

    public static void ApplyTo(this UpdatePersonRequest request, Person person)
    {
        person.Name = request.Name;
        person.RowVersion = request.RowVersion;
    }

    public static PersonResponse ToResponse(this Person person) => new(
        person.Id,
        person.Name,
        person.RowVersion);
}
