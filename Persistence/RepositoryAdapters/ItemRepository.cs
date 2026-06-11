using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Persistence.RepositoryAdapters;

// Reuses GenericRepository<Item> for the standard read/insert/update plumbing, but exposes a
// soft delete instead of the generic hard delete (hence IItemRepository, not IGenericRepository<Item>).
public sealed class ItemRepository(ItemCatalogueDbContext dbContext, TimeProvider timeProvider, ILoggerFactory loggerFactory)
    : GenericRepository<Item>(dbContext, loggerFactory), IItemRepository
{
    // Read paths eager-load the Location and Owner graph for display.
    protected override IQueryable<Item> ReadQuery()
        => EntitySet.Include(i => i.Location).Include(i => i.Owner).AsNoTracking();


    public async Task<int> SoftDeleteItemByIdAsync(int id, DeletedReason reason, CancellationToken cancellationToken = default)
    {
        // ExecuteUpdate bypasses the change tracker (and therefore the auditing interceptor),
        // so stamp LastModifiedDate explicitly here using the same TimeProvider the interceptor
        // uses, keeping every app-driven write on a single clock.
        var rowsAffected = await EntitySet
            .Where(i => i.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.IsDeleted, true)
                .SetProperty(i => i.ReasonForDeletion, reason)
                .SetProperty(i => i.LastModifiedDate, timeProvider.GetUtcNow().UtcDateTime), cancellationToken);

        if (rowsAffected == 0)
        {
            // Domain-level not-found so the API maps it to 404 (see GenericRepository.DeleteAsync).
            throw NotFoundException.For("Item", id);
        }

        return rowsAffected;
    }
}
