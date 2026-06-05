using Application.DTOs;
using Application.Mapping;
using Application.ServicePorts;
using Domain.Enums;
using Domain.Pagination;
using Domain.RepositoryPorts;

namespace Application.Services;

public sealed class ItemService(IItemRepository itemRepository) : IItemService
{
    public async Task<ItemResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await itemRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Item with id {id} not found.");

        return item.ToResponse();
    }

    public async Task<PagedResponse<ItemResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var page = await itemRepository.GetAllAsync(PageRequest.Create(pagination.Page, pagination.PageSize), cancellationToken);
        return page.ToResponse(i => i.ToResponse());
    }

    public async Task<ItemResponse> CreateAsync(CreateItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = request.ToEntity();
        await itemRepository.InsertAsync(item, cancellationToken);
        return item.ToResponse();
    }

    public async Task<ItemResponse> UpdateAsync(UpdateItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await itemRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Item with id {request.Id} not found.");

        request.ApplyTo(item);
        await itemRepository.UpdateAsync(item, cancellationToken);
        return item.ToResponse();
    }

    public async Task<int> DeleteAsync(int id, DeletedReason reason, CancellationToken cancellationToken = default)
    {
        var numberOfEffectedRows = await itemRepository.SoftDeleteItemByIdAsync(id, reason, cancellationToken);
        return numberOfEffectedRows;
    }
}
