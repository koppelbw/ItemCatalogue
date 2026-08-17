using BlazorUI.Contracts;

namespace BlazorUI.Services.Location;

public interface ILocationApi
{
    Task<PagedResponse<LocationResponse>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken);
}
