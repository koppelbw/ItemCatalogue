using Application.DTOs;
using Application.Mapping;
using Application.ServicePorts;
using Domain.RepositoryPorts;

namespace Application.Services;

public sealed class PersonService(IPersonRepository personRepository) : IPersonService
{
    public async Task<PersonResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var person = await personRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Person with id {id} not found.");

        return person.ToResponse();
    }

    public async Task<IReadOnlyList<PersonResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var people = await personRepository.GetAllAsync(cancellationToken);
        return people.Select(p => p.ToResponse()).ToList();
    }

    public async Task<PersonResponse> CreateAsync(CreatePersonRequest request, CancellationToken cancellationToken = default)
    {
        var person = request.ToEntity();
        await personRepository.InsertAsync(person, cancellationToken);
        return person.ToResponse();
    }

    public async Task<PersonResponse> UpdateAsync(UpdatePersonRequest request, CancellationToken cancellationToken = default)
    {
        var person = await personRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Person with id {request.Id} not found.");

        request.ApplyTo(person);
        await personRepository.UpdateAsync(person, cancellationToken);
        return person.ToResponse();
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await personRepository.DeleteAsync(id, cancellationToken);
    }
}
