using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Persistence.RepositoryAdapters;

public sealed class DoorRepository(ItemCatalogueDbContext dbContext, ILoggerFactory loggerFactory)
    : GenericRepository<Door>(dbContext, loggerFactory), IDoorRepository
{
    protected override IQueryable<Door> ReadQuery()
        => EntitySet.Include(d => d.FromRoom).Include(d => d.ToRoom).AsNoTracking();
}
