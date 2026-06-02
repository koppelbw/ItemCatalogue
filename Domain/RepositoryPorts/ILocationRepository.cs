using Domain.Entities;

namespace Domain.RepositoryPorts;

public interface ILocationRepository
{
    Task<Location?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Location?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Location>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<int> InsertAsync(Location location, CancellationToken cancellationToken = default);

    Task UpdateAsync(Location location, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
