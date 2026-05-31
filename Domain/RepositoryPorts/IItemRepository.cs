using Domain.Entities;
using Domain.Enums;

namespace Domain.RepositoryPorts;

public interface IItemRepository
{
    Task<Item?> GetItemByIdAsync(int id);

    Task<IReadOnlyList<Item>> GetAllItemsAsync();

    Task<int> InsertItemAsync(Item item);

    Task UpdateItemAsync (Item item);

    Task<int> SoftDeleteItemByIdAsync (int id, DeletedReason reason);
}
