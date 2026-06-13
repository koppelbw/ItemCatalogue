using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;

namespace Persistence.RepositoryAdapters;

public sealed class ItemEventRepository(ItemCatalogueDbContext context) : IItemEventRepository
{
    public async Task InsertAsync(ItemEvent itemEvent, CancellationToken cancellationToken = default)
    {
        context.ItemEvents.Add(itemEvent);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ItemEvent>> GetByItemIdAsync(int itemId, CancellationToken cancellationToken = default) =>
        await context.ItemEvents
            .Where(e => e.ItemId == itemId)
            .OrderByDescending(e => e.OccurredAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
}
