using Application.DTOs;

namespace Application.ServicePorts;

public interface IFloorService
{
    Task<FloorResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResponse<FloorResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);

    Task<FloorResponse> CreateAsync(CreateFloorRequest request, CancellationToken cancellationToken = default);

    Task<FloorResponse> UpdateAsync(UpdateFloorRequest request, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
