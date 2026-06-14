using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Persistence.RepositoryAdapters;

public sealed class CollectionRepository(ItemCatalogueDbContext dbContext, ILoggerFactory loggerFactory)
    : GenericRepository<Collection>(dbContext, loggerFactory), ICollectionRepository
{
    // Read paths eager-load the membership rows (ordered for stable display) and each member's Item,
    // so a collection GET returns the full, ordered set in one shape.
    protected override IQueryable<Collection> ReadQuery()
        => EntitySet
            .Include(c => c.Items.OrderBy(ci => ci.SortOrder))
                .ThenInclude(ci => ci.Item)
            .AsNoTracking();

    // Tracked load with members included so the service can add/update/remove CollectionItem entries
    // and have EF reconcile the join rows on SaveAsync. No Item include: membership writes only need
    // the join rows, not the full item graph.
    public Task<Collection?> GetForUpdateWithItemsAsync(int id, CancellationToken cancellationToken = default)
        => EntitySet.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task SaveAsync(CancellationToken cancellationToken = default)
        => DbContext.SaveChangesAsync(cancellationToken);
}
