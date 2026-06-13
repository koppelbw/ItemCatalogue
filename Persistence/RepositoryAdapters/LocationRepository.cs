using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Persistence.RepositoryAdapters;

public sealed class LocationRepository(ItemCatalogueDbContext dbContext, ILoggerFactory loggerFactory)
    : GenericRepository<Location>(dbContext, loggerFactory), ILocationRepository
{
    // Read paths include the child Rooms for display; the update path intentionally does not.
    protected override IQueryable<Location> ReadQuery()
        => EntitySet.Include(l => l.Rooms).AsNoTracking();
}
