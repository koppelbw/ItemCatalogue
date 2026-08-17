using BlazorUI.Contracts;

namespace BlazorUI.Services.Location;

public interface ILocationApi
{
    Task<PagedResponse<LocationResponse>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<LocationResponse> CreateAsync(CreateLocationRequest request, CancellationToken cancellationToken);
    Task<LocationResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<LocationResponse> UpdateAsync(UpdateLocationRequest request, CancellationToken cancellationToken);
}
