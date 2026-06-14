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
    // Read paths eager-load the Room, Container, and Owner graph for display.
    protected override IQueryable<Item> ReadQuery()
        => EntitySet.Include(i => i.Room).Include(i => i.Container).Include(i => i.Owner).AsNoTracking();


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

    public async Task<IReadOnlyList<Tag>> GetTagsAsync(int itemId, CancellationToken cancellationToken = default)
    {
        var item = await EntitySet
            .AsNoTracking()
            .Include(i => i.Tags)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken)
            ?? throw NotFoundException.For("Item", itemId);

        return item.Tags;
    }

    public async Task<IReadOnlyList<Tag>> SetTagsAsync(int itemId, IReadOnlyCollection<int> tagIds, CancellationToken cancellationToken = default)
    {
        // Tracked load (with the current tags) so EF reconciles the ItemTag join rows on save:
        // assigning a fresh list inserts the added pairings and deletes the removed ones.
        var item = await EntitySet
            .Include(i => i.Tags)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken)
            ?? throw NotFoundException.For("Item", itemId);

        var distinctIds = tagIds.Distinct().ToList();

        var tags = await DbContext.Set<Tag>()
            .Where(t => distinctIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        // Every requested tag must exist; otherwise the caller referenced an unknown tag id.
        if (tags.Count != distinctIds.Count)
        {
            var missingId = distinctIds.Except(tags.Select(t => t.Id)).First();
            throw NotFoundException.For("Tag", missingId);
        }

        item.Tags.Clear();
        item.Tags.AddRange(tags);
        await DbContext.SaveChangesAsync(cancellationToken);

        return tags;
    }
}
