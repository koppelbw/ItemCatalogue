using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Pagination;
using Domain.Querying;
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

    public async Task<PagedResult<Item>> SearchAsync(ItemFilter filter, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = ReadQuery();

        // Soft-deleted items are hidden by default; callers opt in with IncludeDeleted.
        if (!filter.IncludeDeleted)
            query = query.Where(i => !i.IsDeleted);

        if (filter.Query is { Length: > 0 } q)
            query = query.Where(i => i.Name.Contains(q) || (i.Description != null && i.Description.Contains(q)));

        if (filter.RoomId.HasValue)
            query = query.Where(i => i.RoomId == filter.RoomId.Value);

        if (filter.ContainerId.HasValue)
            query = query.Where(i => i.ContainerId == filter.ContainerId.Value);

        if (filter.TagId.HasValue)
            query = query.Where(i => i.Tags.Any(t => t.Id == filter.TagId.Value));

        if (filter.OwnerId.HasValue)
            query = query.Where(i => i.OwnerId == filter.OwnerId.Value);

        if (filter.MinValue.HasValue)
            query = query.Where(i => (i.CurrentValue ?? i.PurchasePrice) >= filter.MinValue.Value);

        if (filter.MaxValue.HasValue)
            query = query.Where(i => (i.CurrentValue ?? i.PurchasePrice) <= filter.MaxValue.Value);

        if (filter.Condition.HasValue)
            query = query.Where(i => i.Condition == filter.Condition.Value);

        if (filter.IsStored.HasValue)
            query = query.Where(i => i.IsStored == filter.IsStored.Value);

        return await PaginateAsync(query.OrderBy(i => i.Id), page, cancellationToken);
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
