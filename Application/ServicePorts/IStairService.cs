using Application.DTOs;

namespace Application.ServicePorts;

public interface IStairService
{
    Task<StairResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResponse<StairResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);

    Task<StairResponse> CreateAsync(CreateStairRequest request, CancellationToken cancellationToken = default);

    Task<StairResponse> UpdateAsync(UpdateStairRequest request, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
