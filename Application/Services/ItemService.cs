using Application.DTOs;
using Application.Logging;
using Application.Mapping;
using Application.ServicePorts;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Pagination;
using Domain.Querying;
using Domain.RepositoryPorts;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public sealed class ItemService(
    IItemRepository itemRepository,
    IItemEventRepository itemEventRepository,
    TimeProvider timeProvider,
    IValidator<CreateItemRequest> createValidator,
    IValidator<UpdateItemRequest> updateValidator,
    IValidator<SetItemTagsRequest> setTagsValidator,
    IValidator<ItemSearchQuery> searchQueryValidator,
    ILogger<ItemService> logger) : IItemService
{
    public async Task<ItemResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await itemRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Item", id);

        return item.ToResponse();
    }

    public async Task<PagedResponse<ItemResponse>> GetAllAsync(ItemSearchQuery query, CancellationToken cancellationToken = default)
    {
        await searchQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var filter = query.ToFilter();
        var page = await itemRepository.SearchAsync(filter, PageRequest.Create(query.Page, query.PageSize), cancellationToken);
        return page.ToResponse(i => i.ToResponse());
    }

    public async Task<ItemLocationPathResponse> GetLocationPathAsync(int itemId, CancellationToken cancellationToken = default)
    {
        var item = await itemRepository.GetWithLocationAsync(itemId, cancellationToken)
            ?? throw NotFoundException.For("Item", itemId);
        return item.ToLocationPathResponse();
    }

    public Task<PagedResponse<ItemResponse>> GetItemsByRoomAsync(int roomId, PaginationQuery pagination, CancellationToken cancellationToken = default)
        => ScopedSearch(new ItemFilter(RoomId: roomId), pagination, cancellationToken);

    public Task<PagedResponse<ItemResponse>> GetItemsByContainerAsync(int containerId, PaginationQuery pagination, CancellationToken cancellationToken = default)
        => ScopedSearch(new ItemFilter(ContainerId: containerId), pagination, cancellationToken);

    public Task<PagedResponse<ItemResponse>> GetItemsByFloorAsync(int floorId, PaginationQuery pagination, CancellationToken cancellationToken = default)
        => ScopedSearch(new ItemFilter(FloorId: floorId), pagination, cancellationToken);

    public Task<PagedResponse<ItemResponse>> GetItemsByLocationAsync(int locationId, PaginationQuery pagination, CancellationToken cancellationToken = default)
        => ScopedSearch(new ItemFilter(LocationId: locationId), pagination, cancellationToken);

    private async Task<PagedResponse<ItemResponse>> ScopedSearch(ItemFilter filter, PaginationQuery pagination, CancellationToken cancellationToken)
    {
        var page = await itemRepository.SearchAsync(filter, PageRequest.Create(pagination.Page, pagination.PageSize), cancellationToken);
        return page.ToResponse(i => i.ToResponse());
    }

    public async Task<ItemResponse> CreateAsync(CreateItemRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var item = request.ToEntity();
        await itemRepository.InsertAsync(item, cancellationToken);
        logger.EntityCreated("Item", item.Id);
        return item.ToResponse();
    }

    public async Task<ItemResponse> UpdateAsync(UpdateItemRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var item = await itemRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Item", request.Id);

        request.ApplyTo(item);
        await itemRepository.UpdateAsync(item, cancellationToken);
        logger.EntityUpdated("Item", item.Id);
        return item.ToResponse();
    }

    public async Task<int> DeleteAsync(int id, DeletedReason reason, CancellationToken cancellationToken = default)
    {
        var numberOfEffectedRows = await itemRepository.SoftDeleteItemByIdAsync(id, reason, cancellationToken);
        logger.ItemSoftDeleted(id, reason, numberOfEffectedRows);

        if (numberOfEffectedRows > 0)
        {
            await itemEventRepository.InsertAsync(new ItemEvent
            {
                ItemId = id,
                EventType = ItemEventType.SoftDeleted,
                OccurredAt = timeProvider.GetUtcNow().UtcDateTime,
                Notes = reason.ToString(),
            }, cancellationToken);
        }

        return numberOfEffectedRows;
    }

    public async Task<ItemTagsResponse> GetTagsAsync(int id, CancellationToken cancellationToken = default)
    {
        var tags = await itemRepository.GetTagsAsync(id, cancellationToken);
        return new ItemTagsResponse(id, tags.Select(t => t.ToResponse()).ToList());
    }

    public async Task<ItemTagsResponse> SetTagsAsync(int id, SetItemTagsRequest request, CancellationToken cancellationToken = default)
    {
        await setTagsValidator.ValidateAndThrowAsync(request, cancellationToken);

        var tags = await itemRepository.SetTagsAsync(id, request.TagIds, cancellationToken);
        logger.EntityUpdated("Item", id);
        return new ItemTagsResponse(id, tags.Select(t => t.ToResponse()).ToList());
    }
}
