using Application.DTOs;

namespace Application.ServicePorts;

public interface ILocationService
{
    Task<LocationResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocationResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<LocationResponse> CreateAsync(CreateLocationRequest request, CancellationToken cancellationToken = default);

    Task<LocationResponse> UpdateAsync(UpdateLocationRequest request, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
