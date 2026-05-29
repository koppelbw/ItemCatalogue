using Domain.Entities;
using Domain.Enums;
using Domain.RepositoryPorts;

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
        var item = await dbContext.Items.FindAsync(id)
                    ?? throw new InvalidOperationException($"Item with id {id} not found.");

        item.IsDeleted = true;
        item.ReasonForDeletion = reason;
        return await dbContext.SaveChangesAsync();
    }

    public async Task UpdateItemAsync(Item item)
    {
        throw new NotImplementedException();
    }
}
