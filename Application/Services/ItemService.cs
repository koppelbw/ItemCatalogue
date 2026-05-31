using Domain.Entities;
using Domain.Enums;
using Domain.RepositoryPorts;

namespace Application.Services;

public sealed class ItemService(IItemRepository itemRepository)
{
    public async Task<Item> GetItemByIdAsync(int id)
    {
        return await itemRepository.GetItemByIdAsync(id) ?? throw new InvalidOperationException($"Item with id {id} not found.");
    }

    public async Task<int> DeleteItemAsync(int id, DeletedReason reason)
    {
        var numberOfEffectedRows = await itemRepository.SoftDeleteItemByIdAsync(id, reason);
        return numberOfEffectedRows;
    }
}
