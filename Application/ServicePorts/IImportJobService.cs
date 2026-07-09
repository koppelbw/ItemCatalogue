using Application.DTOs;

namespace Application.ServicePorts;

public interface IImportJobService
{
    Task<ImportJobResponse> StartImportAsync(Stream csvContent, string fileName, CancellationToken cancellationToken = default);

    Task<ImportJobResponse> GetStatusAsync(int jobId, CancellationToken cancellationToken = default);

    // Recent jobs, newest first, for the import history list.
    Task<PagedResponse<ImportJobResponse>> GetRecentAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);

    Task ProcessChunkAsync(ImportChunkMessage message, CancellationToken cancellationToken = default);

    Task MarkChunkFailedAsync(ImportChunkMessage message, string reason, CancellationToken cancellationToken = default);
}
