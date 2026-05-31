using Application.DTOs;
using Application.Mapping;
using Application.ServicePorts;
using Domain.RepositoryPorts;

namespace Application.Services;

public sealed class PersonService(IPersonRepository personRepository) : IPersonService
{
    public async Task<PersonResponse> GetByIdAsync(int id)
    {
        var person = await personRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Person with id {id} not found.");

        return person.ToResponse();
    }

    public async Task<IReadOnlyList<PersonResponse>> GetAllAsync()
    {
        var people = await personRepository.GetAllAsync();
        return people.Select(p => p.ToResponse()).ToList();
    }

    public async Task<PersonResponse> CreateAsync(CreatePersonRequest request)
    {
        var person = request.ToEntity();
        await personRepository.InsertAsync(person);
        return person.ToResponse();
    }

    public async Task<PersonResponse> UpdateAsync(UpdatePersonRequest request)
    {
        var person = await personRepository.GetByIdAsync(request.Id)
            ?? throw new InvalidOperationException($"Person with id {request.Id} not found.");

        request.ApplyTo(person);
        await personRepository.UpdateAsync(person);
        return person.ToResponse();
    }

    public async Task<int> DeleteAsync(int id)
    {
        return await personRepository.DeleteAsync(id);
    }
}
