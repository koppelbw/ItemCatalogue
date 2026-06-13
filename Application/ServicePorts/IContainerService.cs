using Application.DTOs;

namespace Application.ServicePorts;

public interface IContainerService
{
    Task<ContainerResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResponse<ContainerResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);

    Task<ContainerResponse> CreateAsync(CreateContainerRequest request, CancellationToken cancellationToken = default);

    Task<ContainerResponse> UpdateAsync(UpdateContainerRequest request, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
