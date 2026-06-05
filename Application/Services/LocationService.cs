using Application.DTOs;
using Application.Mapping;
using Application.ServicePorts;
using Domain.Pagination;
using Domain.RepositoryPorts;

namespace Application.Services;

public sealed class LocationService(ILocationRepository locationRepository) : ILocationService
{
    public async Task<LocationResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var location = await locationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Location with id {id} not found.");

        return location.ToResponse();
    }

    public async Task<PagedResponse<LocationResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var page = await locationRepository.GetAllAsync(PageRequest.Create(pagination.Page, pagination.PageSize), cancellationToken);
        return page.ToResponse(l => l.ToResponse());
    }

    public async Task<LocationResponse> CreateAsync(CreateLocationRequest request, CancellationToken cancellationToken = default)
    {
        var location = request.ToEntity();
        await locationRepository.InsertAsync(location, cancellationToken);
        return location.ToResponse();
    }

    public async Task<LocationResponse> UpdateAsync(UpdateLocationRequest request, CancellationToken cancellationToken = default)
    {
        var location = await locationRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Location with id {request.Id} not found.");

        request.ApplyTo(location);
        await locationRepository.UpdateAsync(location, cancellationToken);
        return location.ToResponse();
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await locationRepository.DeleteAsync(id, cancellationToken);
    }
}
