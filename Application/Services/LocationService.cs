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

public sealed class LocationService(
    ILocationRepository locationRepository,
    IValidator<CreateLocationRequest> createValidator,
    IValidator<UpdateLocationRequest> updateValidator,
    ILogger<LocationService> logger) : ILocationService
{
    public async Task<LocationResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var location = await locationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Location", id);

        return location.ToResponse();
    }

    public async Task<LocationMapResponse> GetMapAsync(int id, CancellationToken cancellationToken = default)
    {
        var location = await locationRepository.GetMapAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Location", id);

        return location.ToMapResponse();
    }

    public async Task<PagedResponse<LocationResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var page = await locationRepository.GetAllAsync(PageRequest.Create(pagination.Page, pagination.PageSize), cancellationToken);
        return page.ToResponse(l => l.ToResponse());
    }

    public async Task<LocationResponse> CreateAsync(CreateLocationRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var location = request.ToEntity();
        await locationRepository.InsertAsync(location, cancellationToken);
        logger.EntityCreated("Location", location.Id);
        return location.ToResponse();
    }

    public async Task<LocationResponse> UpdateAsync(UpdateLocationRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var location = await locationRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Location", request.Id);

        request.ApplyTo(location);
        await locationRepository.UpdateAsync(location, cancellationToken);
        logger.EntityUpdated("Location", location.Id);
        return location.ToResponse();
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var rowsAffected = await locationRepository.DeleteAsync(id, cancellationToken);
        logger.EntityDeleted("Location", id, rowsAffected);
        return rowsAffected;
    }
}
