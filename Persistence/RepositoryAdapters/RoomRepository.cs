using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;

namespace Persistence.RepositoryAdapters;

public sealed class RoomRepository(ItemCatalogueDbContext dbContext) : IRoomRepository
{
    public async Task<Room?> GetByIdAsync(int id)
    {
        return await dbContext.Rooms.FindAsync(id);
    }

    public async Task<IReadOnlyList<Room>> GetAllAsync()
    {
        return await dbContext.Rooms
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> InsertAsync(Room room)
    {
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();
        return room.Id;
    }

    public async Task UpdateAsync(Room room)
    {
        dbContext.Rooms.Update(room);
        await dbContext.SaveChangesAsync();
    }

    public async Task<int> DeleteAsync(int id)
    {
        var rowsAffected = await dbContext.Rooms
            .Where(r => r.Id == id)
            .ExecuteDeleteAsync();

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Room with id {id} not found.");
        }

        return rowsAffected;
    }
}
