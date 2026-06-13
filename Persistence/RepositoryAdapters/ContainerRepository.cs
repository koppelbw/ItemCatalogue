using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Persistence.RepositoryAdapters;

public sealed class ContainerRepository(ItemCatalogueDbContext dbContext, ILoggerFactory loggerFactory)
    : GenericRepository<Container>(dbContext, loggerFactory), IContainerRepository
{
    // Read paths include the owning Room and parent Container for display; the update path does not.
    protected override IQueryable<Container> ReadQuery()
        => EntitySet.Include(c => c.Room).Include(c => c.ParentContainer).AsNoTracking();
}
