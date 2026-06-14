using Application.DTOs;

namespace Application.ServicePorts;

public interface ICollectionService
{
    Task<CollectionResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResponse<CollectionResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);

    Task<CollectionResponse> CreateAsync(CreateCollectionRequest request, CancellationToken cancellationToken = default);

    Task<CollectionResponse> UpdateAsync(UpdateCollectionRequest request, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<CollectionResponse> AddItemAsync(int collectionId, AddCollectionItemRequest request, CancellationToken cancellationToken = default);

    Task<CollectionResponse> UpdateItemAsync(int collectionId, int itemId, UpdateCollectionItemRequest request, CancellationToken cancellationToken = default);

    Task RemoveItemAsync(int collectionId, int itemId, CancellationToken cancellationToken = default);
}
