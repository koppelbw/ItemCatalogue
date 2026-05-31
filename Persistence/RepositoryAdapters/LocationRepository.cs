using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;

namespace Persistence.RepositoryAdapters;

public sealed class LocationRepository(ItemCatalogueDbContext dbContext) : ILocationRepository
{
    public async Task<Location?> GetByIdAsync(int id)
    {
        return await dbContext.Locations
            .Include(l => l.Room)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<IReadOnlyList<Location>> GetAllAsync()
    {
        return await dbContext.Locations
            .Include(l => l.Room)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> InsertAsync(Location location)
    {
        dbContext.Locations.Add(location);
        await dbContext.SaveChangesAsync();
        return location.Id;
    }

    public async Task UpdateAsync(Location location)
    {
        dbContext.Locations.Update(location);
        await dbContext.SaveChangesAsync();
    }

    public async Task<int> DeleteAsync(int id)
    {
        var rowsAffected = await dbContext.Locations
            .Where(l => l.Id == id)
            .ExecuteDeleteAsync();

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Location with id {id} not found.");
        }

        return rowsAffected;
    }
}
