using Application.DTOs;

namespace Application.ServicePorts;

public interface ILocationService
{
    Task<LocationResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // Returns the full spatial graph (floors, rooms, container trees, doors) for one location,
    // for a consumer to reconstruct the whole building. Throws NotFoundException when missing.
    Task<LocationMapResponse> GetMapAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResponse<LocationResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);

    Task<LocationResponse> CreateAsync(CreateLocationRequest request, CancellationToken cancellationToken = default);

    Task<LocationResponse> UpdateAsync(UpdateLocationRequest request, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
