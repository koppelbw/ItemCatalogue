using Domain.Entities;
using Domain.Enums;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;

namespace Persistence.RepositoryAdapters;

public sealed class ItemRepository(ItemCatalogueDbContext dbContext) : IItemRepository
{
    public async Task<Item?> GetItemByIdAsync(int id)
    {
        return await dbContext.Items
            .Include(i => i.Location)
            .Include(i => i.Owner)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Item?> GetItemForUpdateAsync(int id)
    {
        // Tracked (no AsNoTracking) so SaveChangesAsync emits a minimal, diff-based UPDATE.
        // No Includes: updates only touch the Item's own columns, so the Location/Owner
        // graph is intentionally left out to avoid loading or over-posting related rows.
        return await dbContext.Items
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IReadOnlyList<Item>> GetAllItemsAsync()
    {
        return await dbContext.Items
            //.Where(i => !i.IsDeleted) // TODO: Deleted here means more like, no longer tracked but I don't want to lose the data, so maybe we should rename it to IsArchived or something like that. For now, we will just return all items and let the caller decide what to do with deleted items.
            .Include(i => i.Location)
            .Include(i => i.Owner)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> InsertItemAsync(Item item)
    {
        dbContext.Items.Add(item);
        await dbContext.SaveChangesAsync();
        return item.Id;
    }

    public async Task<int> SoftDeleteItemByIdAsync(int id, DeletedReason reason)
    {
        var rowsAffected = await dbContext.Items
        .Where(i => i.Id == id)
        .ExecuteUpdateAsync(s => s
            .SetProperty(i => i.IsDeleted, true)
            .SetProperty(i => i.ReasonForDeletion, reason)
            .SetProperty(i => i.LastModifiedDate, DateTime.UtcNow));

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Item with id {id} not found.");
        }

        return rowsAffected;
    }

    public async Task UpdateItemAsync(Item item)
    {
        item.LastModifiedDate = DateTime.UtcNow;
        //dbContext.Items.Update(item); // Not needed because the item is already being tracked by the dbContext, so we just need to call SaveChangesAsync to persist the changes to the database.
        await dbContext.SaveChangesAsync();
    }
}
