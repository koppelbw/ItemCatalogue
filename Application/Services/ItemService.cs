using Application.DTOs;
using Application.Logging;
using Application.Mapping;
using Application.ServicePorts;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Pagination;
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
    ILogger<ItemService> logger) : IItemService
{
    public async Task<ItemResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await itemRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Item", id);

        return item.ToResponse();
    }

    public async Task<PagedResponse<ItemResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var page = await itemRepository.GetAllAsync(PageRequest.Create(pagination.Page, pagination.PageSize), cancellationToken);
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
