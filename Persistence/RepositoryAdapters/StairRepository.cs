using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Persistence.RepositoryAdapters;

public sealed class StairRepository(ItemCatalogueDbContext dbContext, ILoggerFactory loggerFactory)
    : GenericRepository<Stair>(dbContext, loggerFactory), IStairRepository
{
    protected override IQueryable<Stair> ReadQuery()
        => EntitySet.Include(s => s.FromRoom).Include(s => s.ToRoom).AsNoTracking();
}
