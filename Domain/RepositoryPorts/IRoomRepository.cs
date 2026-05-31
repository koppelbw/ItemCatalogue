using Domain.Entities;

namespace Domain.RepositoryPorts;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(int id);

    Task<IReadOnlyList<Room>> GetAllAsync();

    Task<int> InsertAsync(Room room);

    Task UpdateAsync(Room room);

    Task<int> DeleteAsync(int id);
}
