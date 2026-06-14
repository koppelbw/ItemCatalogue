using Application.DTOs;

namespace Application.ServicePorts;

public interface ITagService
{
    Task<TagResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResponse<TagResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);

    Task<TagResponse> CreateAsync(CreateTagRequest request, CancellationToken cancellationToken = default);

    Task<TagResponse> UpdateAsync(UpdateTagRequest request, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
