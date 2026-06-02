using Domain.Entities;
using Domain.Exceptions;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;

namespace Persistence.RepositoryAdapters;

public sealed class RoomRepository(ItemCatalogueDbContext dbContext) : IRoomRepository
{
    public async Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Read-only path: not tracked, since the result is only mapped to a response.
        return await dbContext.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Room?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default)
    {
        // Tracked (no AsNoTracking) so SaveChangesAsync emits a minimal, diff-based UPDATE.
        return await dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
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
        // room is already tracked (loaded via GetForUpdateAsync), so no Update() call is needed.
        // Drive the concurrency check off the client's token carried on the entity.
        dbContext.Entry(room).Property(r => r.RowVersion).OriginalValue = room.RowVersion;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                $"Room with id {room.Id} was modified by another process. Reload and try again.", ex);
        }
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
