using Domain.Entities;
using Domain.Enums;

namespace Domain.RepositoryPorts;

public interface IItemRepository
{
    Task<Item?> GetItemByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Item?> GetItemForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Item>> GetAllItemsAsync(CancellationToken cancellationToken = default);

    Task<int> InsertItemAsync(Item item, CancellationToken cancellationToken = default);

    Task UpdateItemAsync (Item item, CancellationToken cancellationToken = default);

    Task<int> SoftDeleteItemByIdAsync (int id, DeletedReason reason, CancellationToken cancellationToken = default);
}
