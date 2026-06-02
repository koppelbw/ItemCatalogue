using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;

namespace Persistence.RepositoryAdapters;

public sealed class RoomRepository(ItemCatalogueDbContext dbContext) : IRoomRepository
{
    public async Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Rooms.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Rooms
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> InsertAsync(Room room, CancellationToken cancellationToken = default)
    {
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync(cancellationToken);
        return room.Id;
    }

    public async Task UpdateAsync(Room room, CancellationToken cancellationToken = default)
    {
        dbContext.Rooms.Update(room);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var rowsAffected = await dbContext.Rooms
            .Where(r => r.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Room with id {id} not found.");
        }

        return rowsAffected;
    }
}
