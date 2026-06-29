using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Persistence.RepositoryAdapters;

public sealed class FloorRepository(ItemCatalogueDbContext dbContext, ILoggerFactory loggerFactory)
    : GenericRepository<Floor>(dbContext, loggerFactory), IFloorRepository
{
    protected override IQueryable<Floor> ReadQuery()
        => EntitySet.Include(f => f.Location).AsNoTracking();
}
