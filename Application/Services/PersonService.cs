using Application.DTOs;
using Application.Mapping;
using Application.ServicePorts;
using Domain.Exceptions;
using Domain.Pagination;
using Domain.RepositoryPorts;
using FluentValidation;

namespace Application.Services;

public sealed class PersonService(
    IPersonRepository personRepository,
    IValidator<CreatePersonRequest> createValidator,
    IValidator<UpdatePersonRequest> updateValidator) : IPersonService
{
    public async Task<PersonResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var person = await personRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Person", id);

        return person.ToResponse();
    }

    public async Task<PagedResponse<PersonResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var page = await personRepository.GetAllAsync(PageRequest.Create(pagination.Page, pagination.PageSize), cancellationToken);
        return page.ToResponse(p => p.ToResponse());
    }

    public async Task<PersonResponse> CreateAsync(CreatePersonRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var person = request.ToEntity();
        await personRepository.InsertAsync(person, cancellationToken);
        return person.ToResponse();
    }

    public async Task<PersonResponse> UpdateAsync(UpdatePersonRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var person = await personRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Person", request.Id);

        request.ApplyTo(person);
        await personRepository.UpdateAsync(person, cancellationToken);
        return person.ToResponse();
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await personRepository.DeleteAsync(id, cancellationToken);
    }
}
