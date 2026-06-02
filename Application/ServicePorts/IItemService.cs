using Application.DTOs;
using Domain.Enums;

namespace Application.ServicePorts;

public interface IItemService
{
    Task<ItemResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ItemResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ItemResponse> CreateAsync(CreateItemRequest request, CancellationToken cancellationToken = default);

    Task<ItemResponse> UpdateAsync(UpdateItemRequest request, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, DeletedReason reason, CancellationToken cancellationToken = default);
}
