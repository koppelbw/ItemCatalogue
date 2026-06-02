using Domain.Entities;

namespace Domain.RepositoryPorts;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Room?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<int> InsertAsync(Room room, CancellationToken cancellationToken = default);

    Task UpdateAsync(Room room, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
