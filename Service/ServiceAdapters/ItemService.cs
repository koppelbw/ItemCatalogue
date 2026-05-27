using Application.ServicePorts;
using Domain.Entities;
using Domain.Enums;
using Domain.RepositoryPorts;

namespace Service.ServiceAdapters;

public sealed class ItemService(IItemRepository itemRepository) : IItemService
{
    public async Task<Item> GetItemByIdAsync(int id)
    {
        return await itemRepository.GetItemByIdAsync(id) ?? throw new InvalidOperationException($"Item with id {id} not found.");
    }

    public async Task DeleteItemAsync(int id, DeletedReason reason)
    {
        _ = await GetItemByIdAsync(id);

        await itemRepository.SoftDeleteItemByIdAsync(id, reason);
    }
}
