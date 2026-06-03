using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;

namespace Persistence.RepositoryAdapters;

public sealed class LocationRepository(ItemCatalogueDbContext dbContext)
    : GenericRepository<Location>(dbContext), ILocationRepository
{
    // Read paths include the parent Room for display; the update path intentionally does not.
    protected override IQueryable<Location> ReadQuery()
        => EntitySet.Include(l => l.Room).AsNoTracking();
}
