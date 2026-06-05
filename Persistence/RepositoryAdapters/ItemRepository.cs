using Domain.Entities;
using Domain.Enums;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;

namespace Persistence.RepositoryAdapters;

// Reuses GenericRepository<Item> for the standard read/insert/update plumbing, but exposes a
// soft delete instead of the generic hard delete (hence IItemRepository, not IGenericRepository<Item>).
public sealed class ItemRepository(ItemCatalogueDbContext dbContext, TimeProvider timeProvider)
    : GenericRepository<Item>(dbContext), IItemRepository
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
            throw new InvalidOperationException($"Item with id {id} not found.");
        }

        return rowsAffected;
    }
}
