using Domain.Entities;

namespace Domain.RepositoryPorts;

public interface ILocationRepository
{
    Task<Location?> GetByIdAsync(int id);

    Task<IReadOnlyList<Location>> GetAllAsync();

    Task<int> InsertAsync(Location location);

    Task UpdateAsync(Location location);

    Task<int> DeleteAsync(int id);
}
