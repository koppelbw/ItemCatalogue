using Domain.Entities;
using Domain.Enums;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;

namespace Persistence.RepositoryAdapters;

public sealed class ItemRepository(ItemCatalogueDbContext dbContext) : IItemRepository
{
    public async Task<Item?> GetItemByIdAsync(int id)
    {
        return await dbContext.Items.FindAsync(id);
    }

    public async Task<int> InsertItemAsync(Item item)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }
}
