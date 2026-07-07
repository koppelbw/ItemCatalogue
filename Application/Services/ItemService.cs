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
    IRoomRepository roomRepository,
    IContainerRepository containerRepository,
    IPersonRepository personRepository,
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

    public async Task<BulkCreateResult> CreateManyAsync(IReadOnlyList<CreateItemRequest> requests, CancellationToken cancellationToken = default)
    {
        var errorsByIndex = new Dictionary<int, List<string>>();

        // Per-row validation with ValidateAsync (not ValidateAndThrow): one bad row must not
        // reject its neighbors, so failures are accumulated instead of thrown.
        for (var i = 0; i < requests.Count; i++)
        {
            var result = await createValidator.ValidateAsync(requests[i], cancellationToken);
            if (!result.IsValid)
            {
                errorsByIndex[i] = result.Errors.Select(e => e.ErrorMessage).ToList();
            }
        }

        // FK existence pre-check, batched to one query per referenced table. A single-row insert
        // would surface a dangling reference as an unhandled SqlException 547; here it becomes a
        // per-row error and the rest of the batch still inserts.
        var candidates = Enumerable.Range(0, requests.Count).Where(i => !errorsByIndex.ContainsKey(i)).ToList();
        var missingRooms = await MissingIdsAsync(roomRepository, candidates.Where(i => requests[i].RoomId.HasValue).Select(i => requests[i].RoomId!.Value), cancellationToken);
        var missingContainers = await MissingIdsAsync(containerRepository, candidates.Where(i => requests[i].ContainerId.HasValue).Select(i => requests[i].ContainerId!.Value), cancellationToken);
        var missingOwners = await MissingIdsAsync(personRepository, candidates.Where(i => requests[i].OwnerId.HasValue).Select(i => requests[i].OwnerId!.Value), cancellationToken);

        foreach (var i in candidates)
        {
            var request = requests[i];
            var rowErrors = new List<string>();

            if (request.RoomId is int roomId && missingRooms.Contains(roomId))
                rowErrors.Add($"Room {roomId} does not exist.");
            if (request.ContainerId is int containerId && missingContainers.Contains(containerId))
                rowErrors.Add($"Container {containerId} does not exist.");
            if (request.OwnerId is int ownerId && missingOwners.Contains(ownerId))
                rowErrors.Add($"Person {ownerId} does not exist.");

            if (rowErrors.Count > 0)
            {
                errorsByIndex[i] = rowErrors;
            }
        }

        var entities = Enumerable.Range(0, requests.Count)
            .Where(i => !errorsByIndex.ContainsKey(i))
            .Select(i => requests[i].ToEntity())
            .ToList();

        if (entities.Count > 0)
        {
            await itemRepository.InsertRangeAsync(entities, cancellationToken);
        }
        logger.ItemsBulkCreated(entities.Count, errorsByIndex.Count);

        return new BulkCreateResult(
            entities.Select(e => e.Id).ToList(),
            errorsByIndex.OrderBy(kv => kv.Key).Select(kv => new BulkRowError(kv.Key, kv.Value)).ToList());
    }

    private static async Task<HashSet<int>> MissingIdsAsync<TEntity>(
        IGenericRepository<TEntity> repository, IEnumerable<int> referencedIds, CancellationToken cancellationToken)
        where TEntity : class, IEntity
    {
        var wanted = referencedIds.Distinct().ToList();
        if (wanted.Count == 0)
        {
            return [];
        }

        var existing = await repository.FilterExistingIdsAsync(wanted, cancellationToken);
        return wanted.Except(existing).ToHashSet();
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
