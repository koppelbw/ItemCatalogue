using Application.DTOs;
using Application.Logging;
using Application.Mapping;
using Application.ServicePorts;
using Domain.Exceptions;
using Domain.Pagination;
using Domain.RepositoryPorts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public sealed class CollectionService(
    ICollectionRepository collectionRepository,
    IItemRepository itemRepository,
    IValidator<CreateCollectionRequest> createValidator,
    IValidator<UpdateCollectionRequest> updateValidator,
    IValidator<AddCollectionItemRequest> addItemValidator,
    IValidator<UpdateCollectionItemRequest> updateItemValidator,
    ILogger<CollectionService> logger) : ICollectionService
{
    public async Task<CollectionResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var collection = await collectionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Collection", id);

        return collection.ToResponse();
    }

    public async Task<PagedResponse<CollectionResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var page = await collectionRepository.GetAllAsync(PageRequest.Create(pagination.Page, pagination.PageSize), cancellationToken);
        return page.ToResponse(c => c.ToResponse());
    }

    public async Task<CollectionResponse> CreateAsync(CreateCollectionRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var collection = request.ToEntity();
        await collectionRepository.InsertAsync(collection, cancellationToken);
        logger.EntityCreated("Collection", collection.Id);
        return collection.ToResponse();
    }

    public async Task<CollectionResponse> UpdateAsync(UpdateCollectionRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var collection = await collectionRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Collection", request.Id);

        request.ApplyTo(collection);
        await collectionRepository.UpdateAsync(collection, cancellationToken);
        logger.EntityUpdated("Collection", collection.Id);
        // Re-fetch through the read path so the response carries the collection's members.
        return await GetByIdAsync(collection.Id, cancellationToken);
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var rowsAffected = await collectionRepository.DeleteAsync(id, cancellationToken);
        logger.EntityDeleted("Collection", id, rowsAffected);
        return rowsAffected;
    }

    public async Task<CollectionResponse> AddItemAsync(int collectionId, AddCollectionItemRequest request, CancellationToken cancellationToken = default)
    {
        await addItemValidator.ValidateAndThrowAsync(request, cancellationToken);

        // The referenced item must exist, so the client gets a precise 404 rather than a raw FK error.
        _ = await itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
            ?? throw NotFoundException.For("Item", request.ItemId);

        var collection = await collectionRepository.GetForUpdateWithItemsAsync(collectionId, cancellationToken)
            ?? throw NotFoundException.For("Collection", collectionId);

        if (collection.Items.Any(ci => ci.ItemId == request.ItemId))
        {
            // Re-adding an existing member is a client error; surface it as a 400 through the same
            // validation channel as the field rules (use UpdateItem to change an existing membership).
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.ItemId), $"Item {request.ItemId} is already in collection {collectionId}."),
            ]);
        }

        collection.Items.Add(request.ToEntity(collectionId));
        await collectionRepository.SaveAsync(cancellationToken);
        logger.EntityUpdated("Collection", collectionId);

        return await GetByIdAsync(collectionId, cancellationToken);
    }

    public async Task<CollectionResponse> UpdateItemAsync(int collectionId, int itemId, UpdateCollectionItemRequest request, CancellationToken cancellationToken = default)
    {
        await updateItemValidator.ValidateAndThrowAsync(request, cancellationToken);

        var collection = await collectionRepository.GetForUpdateWithItemsAsync(collectionId, cancellationToken)
            ?? throw NotFoundException.For("Collection", collectionId);

        var member = collection.Items.FirstOrDefault(ci => ci.ItemId == itemId)
            ?? throw NotFoundException.For("CollectionItem", itemId);

        member.Quantity = request.Quantity;
        // A null SortOrder leaves the current ordering untouched.
        member.SortOrder = request.SortOrder ?? member.SortOrder;
        member.Role = request.Role;
        await collectionRepository.SaveAsync(cancellationToken);
        logger.EntityUpdated("Collection", collectionId);

        return await GetByIdAsync(collectionId, cancellationToken);
    }

    public async Task RemoveItemAsync(int collectionId, int itemId, CancellationToken cancellationToken = default)
    {
        var collection = await collectionRepository.GetForUpdateWithItemsAsync(collectionId, cancellationToken)
            ?? throw NotFoundException.For("Collection", collectionId);

        var member = collection.Items.FirstOrDefault(ci => ci.ItemId == itemId)
            ?? throw NotFoundException.For("CollectionItem", itemId);

        collection.Items.Remove(member);
        await collectionRepository.SaveAsync(cancellationToken);
        logger.EntityUpdated("Collection", collectionId);
    }
}
