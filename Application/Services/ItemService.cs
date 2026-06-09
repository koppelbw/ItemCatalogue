using Application.DTOs;
using Application.Mapping;
using Application.ServicePorts;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Pagination;
using Domain.RepositoryPorts;
using FluentValidation;

namespace Application.Services;

public sealed class ItemService(
    IItemRepository itemRepository, 
    IValidator<CreateItemRequest> createValidator, 
    IValidator<UpdateItemRequest> updateValidator) : IItemService
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
        return item.ToResponse();
    }

    public async Task<ItemResponse> UpdateAsync(UpdateItemRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var item = await itemRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Item", request.Id);

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
