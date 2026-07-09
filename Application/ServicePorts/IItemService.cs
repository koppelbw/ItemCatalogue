using Application.DTOs;
using Domain.Enums;

namespace Application.ServicePorts;

public interface IItemService
{
    Task<ItemResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResponse<ItemResponse>> GetAllAsync(ItemSearchQuery query, CancellationToken cancellationToken = default);

    Task<ItemLocationPathResponse> GetLocationPathAsync(int itemId, CancellationToken cancellationToken = default);

    Task<PagedResponse<ItemResponse>> GetItemsByRoomAsync(int roomId, PaginationQuery pagination, CancellationToken cancellationToken = default);

    Task<PagedResponse<ItemResponse>> GetItemsByContainerAsync(int containerId, PaginationQuery pagination, CancellationToken cancellationToken = default);

    Task<PagedResponse<ItemResponse>> GetItemsByFloorAsync(int floorId, PaginationQuery pagination, CancellationToken cancellationToken = default);

    Task<PagedResponse<ItemResponse>> GetItemsByLocationAsync(int locationId, PaginationQuery pagination, CancellationToken cancellationToken = default);

    Task<ItemResponse> CreateAsync(CreateItemRequest request, CancellationToken cancellationToken = default);

    // Partial-success bulk insert: valid rows are inserted in one transaction, invalid rows are
    // reported per-index (validation failures and dangling Room/Container/Owner references alike).
    // Never throws for row-level problems — only for systemic ones (e.g. the database being down).
    Task<BulkCreateResult> CreateManyAsync(IReadOnlyList<CreateItemRequest> requests, CancellationToken cancellationToken = default);

    Task<ItemResponse> UpdateAsync(UpdateItemRequest request, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, DeletedReason reason, CancellationToken cancellationToken = default);

    Task<ItemTagsResponse> GetTagsAsync(int id, CancellationToken cancellationToken = default);

    Task<ItemTagsResponse> SetTagsAsync(int id, SetItemTagsRequest request, CancellationToken cancellationToken = default);
}
