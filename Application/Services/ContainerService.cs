using Application.DTOs;
using Application.Logging;
using Application.Mapping;
using Application.ServicePorts;
using Domain.Exceptions;
using Domain.Pagination;
using Domain.RepositoryPorts;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public sealed class ContainerService(
    IContainerRepository containerRepository,
    IValidator<CreateContainerRequest> createValidator,
    IValidator<UpdateContainerRequest> updateValidator,
    ILogger<ContainerService> logger) : IContainerService
{
    public async Task<ContainerResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var container = await containerRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Container", id);

        return container.ToResponse();
    }

    public async Task<PagedResponse<ContainerResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var page = await containerRepository.GetAllAsync(PageRequest.Create(pagination.Page, pagination.PageSize), cancellationToken);
        return page.ToResponse(c => c.ToResponse());
    }

    public async Task<ContainerResponse> CreateAsync(CreateContainerRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var container = request.ToEntity();
        await containerRepository.InsertAsync(container, cancellationToken);
        logger.EntityCreated("Container", container.Id);
        return container.ToResponse();
    }

    public async Task<ContainerResponse> UpdateAsync(UpdateContainerRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var container = await containerRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Container", request.Id);

        request.ApplyTo(container);
        await containerRepository.UpdateAsync(container, cancellationToken);
        logger.EntityUpdated("Container", container.Id);
        return container.ToResponse();
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var rowsAffected = await containerRepository.DeleteAsync(id, cancellationToken);
        logger.EntityDeleted("Container", id, rowsAffected);
        return rowsAffected;
    }
}
