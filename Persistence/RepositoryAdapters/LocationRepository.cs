using Domain.Entities;
using Domain.Exceptions;
using Domain.RepositoryPorts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Persistence.RepositoryAdapters;

public sealed class LocationRepository(ItemCatalogueDbContext dbContext) : ILocationRepository
{
    public async Task<Location?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Read-only path: not tracked, includes the Room for display.
        return await dbContext.Locations
            .Include(l => l.Room)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<Location?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default)
    {
        // Tracked (no AsNoTracking) so SaveChangesAsync emits a minimal, diff-based UPDATE.
        // No Includes: updates only touch the Location's own columns.
        return await dbContext.Locations
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Location>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Locations
            .Include(l => l.Room)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> InsertAsync(Location location, CancellationToken cancellationToken = default)
    {
        dbContext.Locations.Add(location);
        await dbContext.SaveChangesAsync(cancellationToken);
        return location.Id;
    }

    public async Task UpdateAsync(Location location, CancellationToken cancellationToken = default)
    {
        // location is already tracked (loaded via GetForUpdateAsync), so no Update() call is needed.
        // Drive the concurrency check off the client's token carried on the entity.
        dbContext.Entry(location).Property(l => l.RowVersion).OriginalValue = location.RowVersion;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                $"Location with id {location.Id} was modified by another process. Reload and try again.", ex);
        }
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var rowsAffected = await dbContext.Locations
                .Where(l => l.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Location with id {id} not found.");
            }

            return rowsAffected;
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            // 547 = FK reference constraint violation. Defensive: today nothing restricts a
            // Location delete (Item.LocationId is SetNull), but translate it consistently so a
            // future restricted FK surfaces as a domain exception rather than a raw SQL error.
            throw new EntityInUseException(
                $"Location with id {id} cannot be deleted because it is still referenced by another record.", ex);
        }
    }
}
