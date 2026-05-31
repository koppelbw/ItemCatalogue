using Application.DTOs;

namespace Application.ServicePorts;

public interface ILocationService
{
    Task<LocationResponse> GetByIdAsync(int id);

    Task<IReadOnlyList<LocationResponse>> GetAllAsync();

    Task<LocationResponse> CreateAsync(CreateLocationRequest request);

    Task<LocationResponse> UpdateAsync(UpdateLocationRequest request);

    Task<int> DeleteAsync(int id);
}
