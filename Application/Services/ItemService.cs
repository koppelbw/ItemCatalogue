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

    public async Task DeleteItemAsync(int id, DeletedReason reason)
    {
        await itemRepository.SoftDeleteItemByIdAsync(id, reason);
    }
}
