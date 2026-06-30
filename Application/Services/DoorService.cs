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

public sealed class DoorService(
    IDoorRepository doorRepository,
    IValidator<CreateDoorRequest> createValidator,
    IValidator<UpdateDoorRequest> updateValidator,
    ILogger<DoorService> logger) : IDoorService
{
    public async Task<DoorResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var door = await doorRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Door", id);

        return door.ToResponse();
    }

    public async Task<PagedResponse<DoorResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var page = await doorRepository.GetAllAsync(PageRequest.Create(pagination.Page, pagination.PageSize), cancellationToken);
        return page.ToResponse(d => d.ToResponse());
    }

    public async Task<DoorResponse> CreateAsync(CreateDoorRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var door = request.ToEntity();
        await doorRepository.InsertAsync(door, cancellationToken);
        logger.EntityCreated("Door", door.Id);
        return door.ToResponse();
    }

    public async Task<DoorResponse> UpdateAsync(UpdateDoorRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var door = await doorRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Door", request.Id);

        request.ApplyTo(door);
        await doorRepository.UpdateAsync(door, cancellationToken);
        logger.EntityUpdated("Door", door.Id);
        return door.ToResponse();
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var rowsAffected = await doorRepository.DeleteAsync(id, cancellationToken);
        logger.EntityDeleted("Door", id, rowsAffected);
        return rowsAffected;
    }
}
