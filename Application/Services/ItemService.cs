using Application.DTOs;
using Application.Mapping;
using Application.ServicePorts;
using Domain.Enums;
using Domain.RepositoryPorts;

namespace Application.Services;

public sealed class ItemService(IItemRepository itemRepository) : IItemService
{
    public async Task<ItemResponse> GetByIdAsync(int id)
    {
        var item = await itemRepository.GetItemByIdAsync(id)
            ?? throw new InvalidOperationException($"Item with id {id} not found.");

        return item.ToResponse();
    }

    public async Task<IReadOnlyList<ItemResponse>> GetAllAsync()
    {
        var items = await itemRepository.GetAllItemsAsync();
        return items.Select(i => i.ToResponse()).ToList();
    }

    public async Task<ItemResponse> CreateAsync(CreateItemRequest request)
    {
        var item = request.ToEntity();
        await itemRepository.InsertItemAsync(item);
        return item.ToResponse();
    }

    public async Task<ItemResponse> UpdateAsync(UpdateItemRequest request)
    {
        var item = await itemRepository.GetItemForUpdateAsync(request.Id)
            ?? throw new InvalidOperationException($"Item with id {request.Id} not found.");

        request.ApplyTo(item);
        await itemRepository.UpdateItemAsync(item);
        return item.ToResponse();
    }

    public async Task<int> DeleteAsync(int id, DeletedReason reason)
    {
        var numberOfEffectedRows = await itemRepository.SoftDeleteItemByIdAsync(id, reason);
        return numberOfEffectedRows;
    }
}
