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

public sealed class FloorService(
    IFloorRepository floorRepository,
    IValidator<CreateFloorRequest> createValidator,
    IValidator<UpdateFloorRequest> updateValidator,
    ILogger<FloorService> logger) : IFloorService
{
    public async Task<FloorResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var floor = await floorRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Floor", id);

        return floor.ToResponse();
    }

    public async Task<PagedResponse<FloorResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var page = await floorRepository.GetAllAsync(PageRequest.Create(pagination.Page, pagination.PageSize), cancellationToken);
        return page.ToResponse(f => f.ToResponse());
    }

    public async Task<FloorResponse> CreateAsync(CreateFloorRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var floor = request.ToEntity();
        await floorRepository.InsertAsync(floor, cancellationToken);
        logger.EntityCreated("Floor", floor.Id);
        return floor.ToResponse();
    }

    public async Task<FloorResponse> UpdateAsync(UpdateFloorRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var floor = await floorRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Floor", request.Id);

        request.ApplyTo(floor);
        await floorRepository.UpdateAsync(floor, cancellationToken);
        logger.EntityUpdated("Floor", floor.Id);
        return floor.ToResponse();
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var rowsAffected = await floorRepository.DeleteAsync(id, cancellationToken);
        logger.EntityDeleted("Floor", id, rowsAffected);
        return rowsAffected;
    }
}
