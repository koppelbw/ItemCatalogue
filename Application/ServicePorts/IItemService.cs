using Application.DTOs;
using Domain.Enums;

namespace Application.ServicePorts;

public interface IItemService
{
    Task<ItemResponse> GetByIdAsync(int id);

    Task<IReadOnlyList<ItemResponse>> GetAllAsync();

    Task<ItemResponse> CreateAsync(CreateItemRequest request);

    Task<ItemResponse> UpdateAsync(UpdateItemRequest request);

    Task<int> DeleteAsync(int id, DeletedReason reason);
}
