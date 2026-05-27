using Domain.Entities;
using Domain.Enums;
using Domain.RepositoryPorts;
using Persistence.Database;

namespace Persistence.RepositoryAdapters;

public sealed class ItemRepository(ItemCatalogueDbContext dbContext) : IItemRepository
{
    public Task<Item?> GetItemByIdAsync(int id)
    {
        return dbContext.Items.FindAsync(id).AsTask();
    }

    public Task<int> InsertItemAsync(Item item)
    {
        throw new NotImplementedException();
    }

    public Task<int> SoftDeleteItemByIdAsync(int id, DeletedReason reason)
    {
        throw new NotImplementedException();
    }

    public Task UpdateItemAsync(Item item)
    {
        throw new NotImplementedException();
    }
}
