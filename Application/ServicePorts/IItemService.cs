using Domain.Entities;
using Domain.Enums;

namespace Application.ServicePorts;

public interface IItemService
{
    public Task<Item> GetItemByIdAsync(int id);

    public Task DeleteItemAsync(int id, DeletedReason reason);
}
