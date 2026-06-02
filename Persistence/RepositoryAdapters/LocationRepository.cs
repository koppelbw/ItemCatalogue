using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;

namespace Persistence.RepositoryAdapters;

public sealed class LocationRepository(ItemCatalogueDbContext dbContext) : ILocationRepository
{
    public async Task<Location?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Locations
            .Include(l => l.Room)
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
        dbContext.Locations.Update(location);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
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
}
