using Application.DTOs;

namespace Application.ServicePorts;

public interface IDoorService
{
    Task<DoorResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResponse<DoorResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);

    Task<DoorResponse> CreateAsync(CreateDoorRequest request, CancellationToken cancellationToken = default);

    Task<DoorResponse> UpdateAsync(UpdateDoorRequest request, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
